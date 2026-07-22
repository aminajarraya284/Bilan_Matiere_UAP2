using BilanMatiereUAP2.Application.Abstractions;

namespace BilanMatiereUAP2.Infrastructure.Services;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
