using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Repositories;

public sealed class ImportSourceFileRepository(BilanMatiereUAP2DbContext context) : IImportSourceFileRepository
{
    public async Task<IReadOnlyList<ImportSourceFile>> GetByBatchIdAsync(Guid importBatchId, CancellationToken cancellationToken = default)
        => await context.ImportSourceFiles.AsNoTracking()
            .Where(file => file.ImportBatchId == importBatchId)
            .OrderBy(file => file.TypeImport)
            .ToListAsync(cancellationToken);

    public Task<ImportSourceFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => context.ImportSourceFiles.AsNoTracking().FirstOrDefaultAsync(file => file.Id == id, cancellationToken);

    public Task<ImportSourceFile?> GetLatestByProfileAsync(string profilImport, CancellationToken cancellationToken = default)
        => context.ImportSourceFiles.AsNoTracking()
            .Where(file => file.ProfilImport == profilImport)
            .OrderByDescending(file => file.DateUploadUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(ImportSourceFile file, CancellationToken cancellationToken = default)
        => await context.ImportSourceFiles.AddAsync(file, cancellationToken);
}
