namespace Biomundo.Explorer;

/// <summary>Informe de diagnóstico de una instancia Odoo, pensado para revisar antes de diseñar el modelo SQL.</summary>
public sealed record DiagnosticReport
{
    public required DateTimeOffset GeneratedAtUtc { get; init; }
    public required string OdooUrl { get; init; }
    public required string Database { get; init; }
    public required string Protocol { get; init; }
    public required string ServerVersion { get; init; }
    public required IReadOnlyList<CompanyInfo> Companies { get; init; }
    public required IReadOnlyList<string> InstalledRelevantModules { get; init; }
    public required IReadOnlyList<string> MissingRelevantModules { get; init; }
    public required IReadOnlyList<ModelSummary> Models { get; init; }
    public required IReadOnlyList<CurrencyRateInfo> CurrencyRates { get; init; }
    public List<string> Warnings { get; init; } = [];
}

public sealed record CompanyInfo(int Id, string Name, string Currency);

public sealed record CurrencyRateInfo(string Currency, decimal Rate, DateOnly? RateDate);

/// <summary>Resumen de un modelo de Odoo: volumen, rango de fechas y campos personalizados detectados.</summary>
public sealed record ModelSummary
{
    public required string Model { get; init; }
    public required string Purpose { get; init; }
    public bool Accessible { get; init; }
    public string? AccessError { get; init; }
    public int RecordCount { get; init; }
    public DateOnly? EarliestDate { get; init; }
    public DateOnly? LatestDate { get; init; }
    public IReadOnlyList<CustomField> CustomFields { get; init; } = [];
}

public sealed record CustomField(string Name, string Label, string Type);
