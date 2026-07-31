using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.Application.Abstractions;

public sealed record MouvementStockConsultationQuery(
    DateOnly? DateDebut,
    DateOnly? DateFin,
    string? NumeroOF,
    string? CodeArticle,
    string? Designation,
    string? Transaction,
    string? TypePiece,
    string? PieceOrigine,
    string? Nature,
    Guid? ImportBatchId,
    int Page = 1,
    int PageSize = 50,
    string? SortBy = null,
    string? SortDirection = null);

public sealed record MouvementStockConsultationFilter(
    DateOnly? DateDebut,
    DateOnly? DateFin,
    string? NumeroOF,
    string? CodeArticle,
    string? Designation,
    string? Transaction,
    string? TypePiece,
    string? PieceOrigine,
    NatureMouvementStock? Nature,
    Guid? ImportBatchId,
    int Page,
    int PageSize,
    string SortBy,
    bool Descending);

public sealed record MouvementStockConsultationResult(
    IReadOnlyList<X3MouvementStock> Items,
    int Total,
    ImportTrackingSnapshot Imports);
