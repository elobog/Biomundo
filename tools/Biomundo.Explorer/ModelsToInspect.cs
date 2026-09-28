namespace Biomundo.Explorer;

/// <summary>
/// Modelos de Odoo relevantes para los tres módulos objetivo (flujo de caja, comercio exterior, ventas).
/// La exploración lee volumen, rango de fechas y campos personalizados de cada uno; no lee datos de negocio.
/// </summary>
internal static class ModelsToInspect
{
    public static readonly IReadOnlyList<(string Model, string Purpose)> Items =
    [
        ("res.partner", "Clientes y proveedores"),
        ("account.account", "Plan de cuentas"),
        ("account.journal", "Diarios contables (bancos, caja, ventas, compras)"),
        ("account.move", "Asientos contables (facturas, pagos, ajustes)"),
        ("account.move.line", "Líneas de asiento (base del flujo de caja: vencimientos y saldos)"),
        ("account.payment", "Pagos registrados"),
        ("sale.order", "Órdenes de venta (base de la proyección de ventas)"),
        ("sale.order.line", "Líneas de venta por producto"),
        ("purchase.order", "Órdenes de compra (posible origen de importaciones)"),
        ("purchase.order.line", "Líneas de compra"),
        ("stock.picking", "Recepciones y despachos de inventario"),
        ("stock.landed.cost", "Costos adicionales de importación (flete, seguro, aduana)"),
        ("res.currency.rate", "Tasas de cambio registradas en Odoo"),
    ];

    /// <summary>Módulos cuya presencia orienta el diseño de comex y localización chilena.</summary>
    public static readonly IReadOnlyList<string> RelevantModuleTechnicalNames =
    [
        "account",
        "account_accountant",
        "l10n_cl",
        "l10n_cl_edi",
        "purchase",
        "sale",
        "stock",
        "stock_landed_costs",
        "purchase_stock",
    ];
}
