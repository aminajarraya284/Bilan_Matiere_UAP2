namespace BilanMatiereUAP2.Application.Abstractions;

public interface ICurrentUserContext
{
    bool IsAuthenticated { get; }
    string UserId { get; }
    string DisplayName { get; }
    string? Email { get; }
    IReadOnlyCollection<string> Roles { get; }
}

public interface ICorrelationIdAccessor
{
    string CorrelationId { get; }
}
