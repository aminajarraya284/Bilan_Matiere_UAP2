namespace BilanMatiereUAP2.Application.Abstractions;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
