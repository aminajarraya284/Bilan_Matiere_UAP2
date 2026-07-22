using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BilanMatiereUAP2.Infrastructure.Persistence;

public sealed class BilanMatiereUAP2DbContext(
    DbContextOptions<BilanMatiereUAP2DbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<TypeMatiere> TypesMatiere => Set<TypeMatiere>();
    public DbSet<DechetFonderie> DechetsFonderie => Set<DechetFonderie>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BilanMatiereUAP2DbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
