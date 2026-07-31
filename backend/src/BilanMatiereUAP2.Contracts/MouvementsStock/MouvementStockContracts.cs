using BilanMatiereUAP2.Contracts.Imports;

namespace BilanMatiereUAP2.Contracts.MouvementsStock;

public sealed record MouvementStockResponse(
    Guid Id,
    DateOnly DateImputation,
    string? NumeroOF,
    string? CodeArticle,
    string? Designation,
    decimal QuantiteSignee,
    string? Unite,
    string? Transaction,
    string? TypePiece,
    string? PieceOrigine,
    string Nature,
    Guid ImportBatchId);

public sealed record MouvementsStockConsultationResponse(
    IReadOnlyList<MouvementStockResponse> Items,
    int Total,
    int Page,
    int PageSize,
    ImportTrackingResponse Imports);
