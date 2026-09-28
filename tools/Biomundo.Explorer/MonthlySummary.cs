namespace Biomundo.Explorer;

/// <summary>Resumen del último mes calendario completo, para mostrar al cliente.</summary>
public sealed record MonthlySummary
{
    public required DateOnly PeriodStart { get; init; }
    public required DateOnly PeriodEnd { get; init; }
    public required InvoiceAggregate Sales { get; init; }
    public required InvoiceAggregate Purchases { get; init; }
    public required RrhhSummary Rrhh { get; init; }
    public required FactoringSummary Factoring { get; init; }
    public List<string> Warnings { get; init; } = [];
}

/// <summary>
/// Facturación neta de un período (facturas menos notas de crédito) y órdenes asociadas
/// (órdenes de venta o de compra), ambas leídas de Odoo con estado confirmado/publicado.
/// </summary>
public sealed record InvoiceAggregate
{
    public int InvoiceCount { get; init; }
    public int CreditNoteCount { get; init; }
    public decimal NetAmount { get; init; }
    public int DistinctPartners { get; init; }
    public int ConfirmedOrderCount { get; init; }
    public decimal ConfirmedOrderAmount { get; init; }
}

public sealed record RrhhSummary
{
    public bool ModuleAccessible { get; init; }
    public int ActiveEmployees { get; init; }
    public int? PayslipsInPeriod { get; init; }
    public decimal? NetWageTotal { get; init; }
}

/// <summary>
/// Resumen de facturas factorizadas. En la instancia de Biomundo, ningún campo de Odoo distingue de forma
/// confiable qué facturas están realmente cedidas a una empresa de factoring: los campos "x_studio_factoring*"
/// están vacíos en todos los registros, y "x_studio_estado_de_la_cesion" tiene el mismo valor fijo
/// ("En proceso de pago") en prácticamente el 100% de las facturas, tanto de venta como de compra — no aporta
/// información. Por eso <see cref="Determinable"/> es <c>false</c> hoy: hay que confirmar con Biomundo qué
/// campo (o qué otro sistema) identifica una factura factorizada antes de poder calcular este número.
/// </summary>
public sealed record FactoringSummary
{
    public bool Determinable { get; init; }
    public int InvoiceCount { get; init; }
    public decimal Amount { get; init; }
    public IReadOnlyDictionary<string, int> ByStatus { get; init; } = new Dictionary<string, int>();
}
