using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Common;
using BilanMatiereUAP2.Application.Features.Administration;
using BilanMatiereUAP2.Application.Features.Audit;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;
using FluentValidation;

namespace BilanMatiereUAP2.UnitTests.Application;

/// <summary>
/// Provisionnement automatique des profils Entra et administration des accès.
/// </summary>
public sealed class AdministrationServicesTests
{
    private static readonly DateTime PremierLoginUtc = new(2026, 9, 3, 8, 0, 0, DateTimeKind.Utc);
    private const string ObjectId = "ABC-0001";

    // --- Provisionnement (première connexion et connexions suivantes) --------

    [Fact]
    public async Task Provisionner_PremiereConnexion_CreeUnProfilActifSansSecret()
    {
        var repository = new FakeUtilisateurRepository();
        var audit = new FakeAuditService();
        var service = CreerProvisionnement(repository, audit: audit);

        var resultat = await service.ProvisionnerAsync(
            new EntraIdentite(ObjectId, "user@sopal.com", "Ahmed Ben Ali", ["ResponsableFonderie"]));

        Assert.Equal(ResultatProvisionnement.Cree, resultat);
        var profil = Assert.Single(repository.Utilisateurs);
        Assert.Equal(ObjectId, profil.EntraObjectId);
        Assert.Equal("user@sopal.com", profil.Email);
        Assert.Equal("Ahmed Ben Ali", profil.NomAffichage);
        Assert.Equal("ResponsableFonderie", profil.RoleObserve);
        Assert.True(profil.EstActif);
        Assert.Equal(PremierLoginUtc, profil.DerniereConnexionUtc);

        // Le provisionnement est tracé, sans recopier les revendications brutes du jeton.
        var trace = Assert.Single(audit.Entrees);
        Assert.Equal(ActionAudit.Creation, trace.Action);
        Assert.Equal(DomaineAudit.Administration, trace.Domaine);
        Assert.DoesNotContain(
            trace.NouvellesValeurs!.Keys,
            cle => cle.Contains("token", StringComparison.OrdinalIgnoreCase)
                || cle.Contains("password", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Provisionner_DeuxiemeConnexionMemeOid_NeDupliquePasEtActualiseLaDate()
    {
        var repository = new FakeUtilisateurRepository();
        var horloge = new FakeDateTimeProvider { UtcNow = PremierLoginUtc };
        var service = CreerProvisionnement(repository, horloge);
        var identite = new EntraIdentite(ObjectId, "user@sopal.com", "Ahmed Ben Ali", ["ResponsableFonderie"]);

        await service.ProvisionnerAsync(identite);
        horloge.UtcNow = PremierLoginUtc.AddHours(3);
        var resultat = await service.ProvisionnerAsync(identite);

        Assert.Equal(ResultatProvisionnement.Synchronise, resultat);
        var profil = Assert.Single(repository.Utilisateurs);
        Assert.Equal(PremierLoginUtc, profil.DateCreationUtc);
        Assert.Equal(PremierLoginUtc.AddHours(3), profil.DerniereConnexionUtc);
    }

    [Fact]
    public async Task Provisionner_EmailChangeChezEntra_MetAJourSansCreerUnSecondProfil()
    {
        // L'oid est la seule clé stable : un changement d'email ne doit pas dédoubler le profil.
        var repository = new FakeUtilisateurRepository();
        var horloge = new FakeDateTimeProvider { UtcNow = PremierLoginUtc };
        var service = CreerProvisionnement(repository, horloge);

        await service.ProvisionnerAsync(new EntraIdentite(ObjectId, "ancien@sopal.com", "Ahmed", ["Admin"]));
        horloge.UtcNow = PremierLoginUtc.AddDays(1);
        await service.ProvisionnerAsync(new EntraIdentite(ObjectId, "nouveau@sopal.com", "Ahmed", ["Admin"]));

        var profil = Assert.Single(repository.Utilisateurs);
        Assert.Equal("nouveau@sopal.com", profil.Email);
    }

    [Fact]
    public async Task Provisionner_ProfilDesactive_RefuseLAccesSansReactiver()
    {
        var repository = new FakeUtilisateurRepository();
        var service = CreerProvisionnement(repository);
        var identite = new EntraIdentite(ObjectId, "user@sopal.com", "Ahmed", ["Admin"]);
        await service.ProvisionnerAsync(identite);
        repository.Utilisateurs[0].Desactiver(PremierLoginUtc);

        var resultat = await service.ProvisionnerAsync(identite);

        Assert.Equal(ResultatProvisionnement.Desactive, resultat);
        // Un jeton Entra valide ne doit jamais réactiver un accès coupé localement.
        Assert.False(repository.Utilisateurs[0].EstActif);
    }

    [Theory]
    [InlineData()]
    [InlineData("Lecteur")]
    [InlineData("Invite", "RoleInconnu")]
    public async Task Provisionner_AucunRoleReconnu_NAttribueAucunRoleParDefaut(params string[] roles)
    {
        var repository = new FakeUtilisateurRepository();
        var service = CreerProvisionnement(repository);

        var resultat = await service.ProvisionnerAsync(
            new EntraIdentite(ObjectId, "user@sopal.com", "Ahmed", roles));

        Assert.Equal(ResultatProvisionnement.AucunRoleReconnu, resultat);
        Assert.Empty(repository.Utilisateurs);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("ResponsableFonderie")]
    [InlineData("ResponsableUsinage2")]
    public async Task Provisionner_RoleReconnu_EstEnregistreCommeRoleObserve(string role)
    {
        var repository = new FakeUtilisateurRepository();
        var service = CreerProvisionnement(repository);

        await service.ProvisionnerAsync(new EntraIdentite(ObjectId, "user@sopal.com", "Ahmed", [role]));

        Assert.Equal(role, Assert.Single(repository.Utilisateurs).RoleObserve);
    }

    [Fact]
    public async Task Provisionner_RoleInconnuMeleAUnRoleReconnu_RetientLeRoleReconnu()
    {
        var repository = new FakeUtilisateurRepository();
        var service = CreerProvisionnement(repository);

        await service.ProvisionnerAsync(
            new EntraIdentite(ObjectId, "user@sopal.com", "Ahmed", ["RoleInconnu", "ResponsableUsinage2"]));

        Assert.Equal("ResponsableUsinage2", Assert.Single(repository.Utilisateurs).RoleObserve);
    }

    // --- Administration ------------------------------------------------------

    [Fact]
    public async Task ChangerActivation_Desactivation_TraceLeChangementAvecAvantEtApres()
    {
        var repository = new FakeUtilisateurRepository();
        var audit = new FakeAuditService();
        repository.Utilisateurs.Add(Profil());
        var service = CreerAdministration(repository, audit);

        var reponse = await service.ChangerActivationAsync(0, estActif: false);

        Assert.False(reponse.EstActif);
        var trace = Assert.Single(audit.Entrees);
        Assert.Equal(ActionAudit.Modification, trace.Action);
        Assert.Equal(DomaineAudit.Administration, trace.Domaine);
        Assert.Equal(true, trace.AnciennesValeurs!["estActif"]);
        Assert.Equal(false, trace.NouvellesValeurs!["estActif"]);
    }

    [Fact]
    public async Task ChangerActivation_SansChangementReel_NEcritNiNeTrace()
    {
        var repository = new FakeUtilisateurRepository();
        var audit = new FakeAuditService();
        repository.Utilisateurs.Add(Profil());
        var unitOfWork = new FakeUnitOfWork();
        var service = CreerAdministration(repository, audit, unitOfWork);

        await service.ChangerActivationAsync(0, estActif: true);

        Assert.Empty(audit.Entrees);
        Assert.Equal(0, unitOfWork.NombreSauvegardes);
    }

    [Fact]
    public async Task Search_ReponsePaginee_NExposeAucunSecret()
    {
        var repository = new FakeUtilisateurRepository();
        repository.Utilisateurs.Add(Profil());
        var service = CreerAdministration(repository);

        var reponse = await service.SearchAsync(new GetUtilisateursQuery(null, null, 1, 25));

        Assert.Equal(1, reponse.Total);
        Assert.Equal(1, reponse.TotalPages);
        var proprietes = reponse.Items[0].GetType().GetProperties().Select(p => p.Name).ToArray();
        Assert.DoesNotContain(proprietes, nom =>
            nom.Contains("password", StringComparison.OrdinalIgnoreCase)
            || nom.Contains("token", StringComparison.OrdinalIgnoreCase)
            || nom.Contains("secret", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(0, 25)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task Search_PaginationInvalide_EstRefusee(int page, int pageSize)
    {
        var service = CreerAdministration(new FakeUtilisateurRepository());

        await Assert.ThrowsAsync<ValidationException>(
            () => service.SearchAsync(new GetUtilisateursQuery(null, null, page, pageSize)));
    }

    [Fact]
    public async Task ChangerActivation_ProfilInexistant_RetourneNotFound()
    {
        var service = CreerAdministration(new FakeUtilisateurRepository());

        await Assert.ThrowsAsync<NotFoundException>(() => service.ChangerActivationAsync(404, true));
    }

    /// <summary>
    /// Garde-fou structurel : aucune API publique du service d'administration ne doit
    /// permettre de créer un profil ou de changer un rôle — Entra en est la source.
    /// </summary>
    [Fact]
    public void IAdministrationService_NExposeNiCreationNiChangementDeRole()
    {
        var methodes = typeof(IAdministrationService).GetMethods().Select(m => m.Name).ToArray();

        Assert.DoesNotContain(methodes, nom => nom.Contains("Create", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(methodes, nom => nom.Contains("Role", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(methodes, nom => nom.Contains("Password", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void UtilisateurApplication_NExposeAucunSetterPublicNiChampSecret()
    {
        var type = typeof(UtilisateurApplication);

        Assert.DoesNotContain(type.GetProperties(), p => p.SetMethod?.IsPublic == true);
        Assert.DoesNotContain(type.GetProperties(), p =>
            p.Name.Contains("password", StringComparison.OrdinalIgnoreCase)
            || p.Name.Contains("token", StringComparison.OrdinalIgnoreCase)
            || p.Name.Contains("secret", StringComparison.OrdinalIgnoreCase));
    }

    // --- Helpers -------------------------------------------------------------

    private static UtilisateurApplication Profil()
        => UtilisateurApplication.ProvisionnerDepuisEntra(ObjectId, "user@sopal.com", "Ahmed", "Admin", PremierLoginUtc);

    private static EntraUserProvisioningService CreerProvisionnement(
        FakeUtilisateurRepository repository,
        FakeDateTimeProvider? horloge = null,
        FakeAuditService? audit = null)
        => new(
            repository,
            new FakeUnitOfWork(),
            horloge ?? new FakeDateTimeProvider { UtcNow = PremierLoginUtc },
            audit ?? new FakeAuditService());

    private static AdministrationService CreerAdministration(
        FakeUtilisateurRepository repository,
        FakeAuditService? audit = null,
        FakeUnitOfWork? unitOfWork = null)
        => new(
            repository,
            unitOfWork ?? new FakeUnitOfWork(),
            new FakeDateTimeProvider { UtcNow = PremierLoginUtc },
            audit ?? new FakeAuditService());

    private sealed class FakeUtilisateurRepository : IUtilisateurApplicationRepository
    {
        public List<UtilisateurApplication> Utilisateurs { get; } = [];

        public Task<(IReadOnlyList<UtilisateurApplication> Items, int Total)> SearchAsync(
            UtilisateurApplicationFiltre filtre,
            CancellationToken cancellationToken = default)
        {
            var query = Utilisateurs.AsEnumerable();
            if (filtre.EstActif is not null) query = query.Where(u => u.EstActif == filtre.EstActif.Value);
            var items = query.ToArray();
            return Task.FromResult<(IReadOnlyList<UtilisateurApplication>, int)>((
                items.Skip((filtre.Page - 1) * filtre.PageSize).Take(filtre.PageSize).ToArray(),
                items.Length));
        }

        // Les fakes indexent par position : suffisant et stable pour ces scénarios.
        public Task<UtilisateurApplication?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(id >= 0 && id < Utilisateurs.Count ? Utilisateurs[id] : null);

        public Task<UtilisateurApplication?> GetByEntraObjectIdAsync(string entraObjectId, CancellationToken cancellationToken = default)
            => Task.FromResult(Utilisateurs.FirstOrDefault(u =>
                string.Equals(u.EntraObjectId, entraObjectId, StringComparison.Ordinal)));

        public Task AddAsync(UtilisateurApplication utilisateur, CancellationToken cancellationToken = default)
        {
            Utilisateurs.Add(utilisateur);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int NombreSauvegardes { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            NombreSauvegardes++;
            return Task.FromResult(0);
        }
    }

    private sealed class FakeAuditService : IAuditService
    {
        public List<AuditEntry> Entrees { get; } = [];

        public Task EnregistrerAsync(AuditEntry entry, CancellationToken cancellationToken = default)
        {
            Entrees.Add(entry);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow { get; set; } = PremierLoginUtc;
    }
}
