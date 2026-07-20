using Microsoft.EntityFrameworkCore;

namespace BilanMatiereUAP2.Infrastructure.Persistence;

public sealed class BilanMatiereUAP2DbContext(
    DbContextOptions<BilanMatiereUAP2DbContext> options) : DbContext(options)
{
}
