namespace BusinessCloud.Application.Common.Dtos;

/// <summary>
/// Strongly-typed DTO for audit log entries from MongoDB.
/// Replaces dynamic typing for type safety per C# 14 standards.
/// </summary>
public sealed record AuditLogEntry
{
    public string? Id { get; init; }
    public string? Event { get; init; }
    public int? SaleId { get; init; }
    public string? TenantId { get; init; }
    public DateTime Timestamp { get; init; }
    public string? Details { get; init; }
    public decimal? Amount { get; init; }
    public string? UserId { get; init; }
    public string? Reference { get; init; }
    
    /// <summary>
    /// Additional metadata stored as key-value pairs.
    /// </summary>
    public Dictionary<string, object?>? Metadata { get; init; }
}
