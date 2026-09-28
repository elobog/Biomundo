using Biomundo.Odoo;

namespace Biomundo.Explorer;

/// <summary>
/// Calcula el resumen de ventas, compras, RRHH y factoring del último mes calendario completo,
/// con operaciones de solo lectura sobre Odoo.
/// </summary>
public sealed class OdooMonthlySummaryService(OdooClientFactory factory)
{
    public async Task<MonthlySummary> RunAsync(CancellationToken ct = default)
    {
        var (start, end) = LastCompleteMonth();
        var warnings = new List<string>();
        var client = await factory.CreateAsync(ct);
        await client.AuthenticateAsync(ct);

        var sales = await GetInvoiceAggregateAsync(client, "out_invoice", "out_refund", "sale.order", "date_order", start, end, warnings, ct);
        var purchases = await GetInvoiceAggregateAsync(client, "in_invoice", "in_refund", "purchase.order", "date_order", start, end, warnings, ct);
        var rrhh = await GetRrhhSummaryAsync(client, start, end, warnings, ct);
        var factoring = await GetFactoringSummaryAsync(client, start, end, warnings, ct);

        return new MonthlySummary
        {
            PeriodStart = start,
            PeriodEnd = end,
            Sales = sales,
            Purchases = purchases,
            Rrhh = rrhh,
            Factoring = factoring,
            Warnings = warnings,
        };
    }

    /// <summary>Último mes calendario ya cerrado, calculado respecto a hoy (hora local).</summary>
    internal static (DateOnly Start, DateOnly End) LastCompleteMonth()
    {
        var firstOfThisMonth = new DateOnly(DateTime.Now.Year, DateTime.Now.Month, 1);
        var start = firstOfThisMonth.AddMonths(-1);
        var end = firstOfThisMonth.AddDays(-1);
        return (start, end);
    }

    private static async Task<InvoiceAggregate> GetInvoiceAggregateAsync(
        IOdooClient client, string invoiceType, string refundType, string orderModel, string orderDateField,
        DateOnly start, DateOnly end, List<string> warnings, CancellationToken ct)
    {
        try
        {
            var invoiceDomain = OdooDomain.Where("state", "=", "posted")
                .And("invoice_date", ">=", start.ToString("yyyy-MM-dd"))
                .And("invoice_date", "<=", end.ToString("yyyy-MM-dd"));

            var invoices = await client.SearchReadAsync("account.move", invoiceDomain.With("move_type", "=", invoiceType),
                fields: ["amount_total", "partner_id"], ct: ct);
            var refunds = await client.SearchReadAsync("account.move", invoiceDomain.With("move_type", "=", refundType),
                fields: ["amount_total", "partner_id"], ct: ct);

            var invoiceTotal = invoices.Sum(r => r.GetDecimal("amount_total") ?? 0m);
            var refundTotal = refunds.Sum(r => r.GetDecimal("amount_total") ?? 0m);
            var distinctPartners = invoices.Concat(refunds)
                .Select(r => r.GetMany2One("partner_id")?.Id)
                .Where(id => id is not null)
                .Distinct()
                .Count();

            var (orderCount, orderAmount) = await GetConfirmedOrdersAsync(client, orderModel, orderDateField, start, end, warnings, ct);

            return new InvoiceAggregate
            {
                InvoiceCount = invoices.Count,
                CreditNoteCount = refunds.Count,
                NetAmount = invoiceTotal - refundTotal,
                DistinctPartners = distinctPartners,
                ConfirmedOrderCount = orderCount,
                ConfirmedOrderAmount = orderAmount,
            };
        }
        catch (OdooException ex)
        {
            warnings.Add($"No se pudo calcular el agregado de {invoiceType}/{refundType}: {ex.Message}");
            return new InvoiceAggregate();
        }
    }

    private static async Task<(int Count, decimal Amount)> GetConfirmedOrdersAsync(
        IOdooClient client, string model, string dateField, DateOnly start, DateOnly end, List<string> warnings, CancellationToken ct)
    {
        try
        {
            var states = model == "sale.order" ? new[] { "sale", "done" } : new[] { "purchase", "done" };
            var domain = OdooDomain.Where("state", "in", states)
                .And(dateField, ">=", start.ToString("yyyy-MM-dd"))
                .And(dateField, "<=", end.ToString("yyyy-MM-dd") + " 23:59:59");
            var rows = await client.SearchReadAsync(model, domain, fields: ["amount_total"], ct: ct);
            return (rows.Count, rows.Sum(r => r.GetDecimal("amount_total") ?? 0m));
        }
        catch (OdooException ex)
        {
            warnings.Add($"No se pudo leer órdenes confirmadas de {model}: {ex.Message}");
            return (0, 0m);
        }
    }

    private static async Task<RrhhSummary> GetRrhhSummaryAsync(IOdooClient client, DateOnly start, DateOnly end, List<string> warnings, CancellationToken ct)
    {
        int activeEmployees;
        try
        {
            activeEmployees = await client.SearchCountAsync("hr.employee", OdooDomain.Where("active", "=", true), ct);
        }
        catch (OdooException ex)
        {
            warnings.Add($"Módulo de RRHH (hr.employee) no accesible: {ex.Message}");
            return new RrhhSummary { ModuleAccessible = false };
        }

        int? payslipCount = null;
        decimal? netWageTotal = null;
        try
        {
            var payslipFields = await client.FieldsGetAsync("hr.payslip", ["string"], ct);
            var hasNetWage = payslipFields.ContainsKey("net_wage");
            var domain = OdooDomain.Where("date_to", ">=", start.ToString("yyyy-MM-dd")).And("date_to", "<=", end.ToString("yyyy-MM-dd"));
            var fields = hasNetWage ? new[] { "net_wage" } : ["id"];
            var payslips = await client.SearchReadAsync("hr.payslip", domain, fields, ct: ct);

            payslipCount = payslips.Count;
            if (hasNetWage)
                netWageTotal = payslips.Sum(p => p.GetDecimal("net_wage") ?? 0m);
        }
        catch (OdooException ex)
        {
            warnings.Add($"No se pudo leer liquidaciones de sueldo (hr.payslip): {ex.Message}");
        }

        return new RrhhSummary
        {
            ModuleAccessible = true,
            ActiveEmployees = activeEmployees,
            PayslipsInPeriod = payslipCount,
            NetWageTotal = netWageTotal,
        };
    }

    /// <summary>
    /// Candidatos a "esta factura está factorizada" ya descartados por evidencia real contra Odoo
    /// (ver comentario en <see cref="FactoringSummary"/>): x_studio_factoring, x_studio_factoring_1 y
    /// x_studio_factoring_2 están vacíos en el 100% de los registros; x_studio_empresa_factoring está
    /// seteado en el 100% (no discrimina); x_studio_estado_de_la_cesion tiene un único valor fijo en ~100%
    /// de las facturas, incluidas las de compra, donde no debería aplicar. Mientras no se confirme con
    /// Biomundo qué campo (o qué otro sistema) identifica realmente una cesión a factoring, no se calcula
    /// un monto: mostrarlo sería un número inventado, no un dato.
    /// </summary>
    private static Task<FactoringSummary> GetFactoringSummaryAsync(IOdooClient client, DateOnly start, DateOnly end, List<string> warnings, CancellationToken ct)
    {
        warnings.Add(
            "Factoring: ningún campo de Odoo detectado distingue de forma confiable qué facturas están " +
            "factorizadas (ver docs/preguntas-pendientes.md). No se calculó un monto para evitar reportar una cifra incorrecta.");
        return Task.FromResult(new FactoringSummary { Determinable = false });
    }
}
