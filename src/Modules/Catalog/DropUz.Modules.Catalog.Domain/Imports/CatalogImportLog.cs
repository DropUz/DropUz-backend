using DropUz.Common.Domain;

namespace DropUz.Modules.Catalog.Domain.Imports;

public sealed class CatalogImportLog : Entity
{
    private CatalogImportLog()
    {
    }

    private CatalogImportLog(
        Guid id,
        string sourcePlatform,
        string sourceProductId,
        string providerName,
        CatalogImportStatus status,
        Guid? catalogProductId,
        CatalogImportOperation? operation,
        string? errorCode,
        string? errorMessage,
        Guid? requestedByUserId,
        DateTime completedAtUtc)
        : base(id)
    {
        SourcePlatform = sourcePlatform;
        SourceProductId = sourceProductId;
        ProviderName = providerName;
        Status = status;
        CatalogProductId = catalogProductId;
        Operation = operation;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        RequestedByUserId = requestedByUserId;
        CompletedAtUtc = completedAtUtc;
    }

    public string SourcePlatform { get; private set; } = string.Empty;

    public string SourceProductId { get; private set; } = string.Empty;

    public string ProviderName { get; private set; } = string.Empty;

    public CatalogImportStatus Status { get; private set; }

    public Guid? CatalogProductId { get; private set; }

    public CatalogImportOperation? Operation { get; private set; }

    public string? ErrorCode { get; private set; }

    public string? ErrorMessage { get; private set; }

    public Guid? RequestedByUserId { get; private set; }

    public DateTime CompletedAtUtc { get; private set; }

    public static CatalogImportLog Succeeded(
        string sourcePlatform,
        string sourceProductId,
        string providerName,
        Guid catalogProductId,
        CatalogImportOperation operation,
        Guid? requestedByUserId,
        DateTime completedAtUtc)
    {
        return new CatalogImportLog(
            Guid.NewGuid(),
            NormalizeRequired(sourcePlatform),
            NormalizeRequired(sourceProductId),
            NormalizeRequired(providerName),
            CatalogImportStatus.Succeeded,
            catalogProductId,
            operation,
            null,
            null,
            requestedByUserId,
            completedAtUtc);
    }

    public static CatalogImportLog Failed(
        string sourcePlatform,
        string sourceProductId,
        string providerName,
        string errorCode,
        string errorMessage,
        Guid? requestedByUserId,
        DateTime completedAtUtc)
    {
        return new CatalogImportLog(
            Guid.NewGuid(),
            NormalizeRequired(sourcePlatform),
            NormalizeRequired(sourceProductId),
            NormalizeRequired(providerName),
            CatalogImportStatus.Failed,
            null,
            null,
            NormalizeRequired(errorCode),
            NormalizeRequired(errorMessage),
            requestedByUserId,
            completedAtUtc);
    }

    private static string NormalizeRequired(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value.Trim();
    }
}
