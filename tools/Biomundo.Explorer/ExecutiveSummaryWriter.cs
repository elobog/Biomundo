using System.Globalization;
using System.Net;
using System.Text;

namespace Biomundo.Explorer;

/// <summary>
/// Genera una página HTML de resumen ejecutivo a partir del diagnóstico, pensada para mostrar al cliente:
/// sin nombres técnicos de campos ni jerga de Odoo, solo lo que importa para decidir cómo seguir.
/// </summary>
public static class ExecutiveSummaryWriter
{
    // Modelos técnicos → etiqueta de negocio para la tabla de volumetría. Solo los de interés directo para
    // el cliente: se excluyen a propósito los de detalle técnico (líneas contables, tasas de cambio, plan
    // de cuentas) y cualquier hallazgo que necesite contexto para no alarmar sin explicación (ver la sección
    // "Resumen del mes" y docs/preguntas-pendientes.md para el caso de comercio exterior).
    private static readonly Dictionary<string, string> BusinessLabels = new()
    {
        ["res.partner"] = "Clientes y proveedores",
        ["sale.order"] = "Órdenes de venta",
        ["purchase.order"] = "Órdenes de compra",
        ["stock.picking"] = "Movimientos de inventario",
        ["account.move"] = "Documentos contables (facturas y pagos)",
    };

    private static readonly CultureInfo ClpCulture = GetClpCulture();

    public static async Task<string> WriteAsync(DiagnosticReport report, MonthlySummary monthly, string outputDir, CancellationToken ct = default)
    {
        Directory.CreateDirectory(outputDir);
        var stamp = report.GeneratedAtUtc.ToLocalTime().ToString("yyyyMMdd-HHmmss");
        var path = Path.Combine(outputDir, $"resumen-ejecutivo-{stamp}.html");
        await File.WriteAllTextAsync(path, Build(report, monthly), Encoding.UTF8, ct);
        return path;
    }

    private static CultureInfo GetClpCulture()
    {
        try { return CultureInfo.GetCultureInfo("es-CL"); }
        catch (CultureNotFoundException) { return CultureInfo.InvariantCulture; }
    }

    private static string Build(DiagnosticReport r, MonthlySummary monthly)
    {
        var company = r.Companies.FirstOrDefault();
        var usdRate = r.CurrencyRates.FirstOrDefault(c => c.Currency == "USD");
        var periodLabel = ClpCulture.TextInfo.ToTitleCase(monthly.PeriodStart.ToString("MMMM yyyy", ClpCulture));

        var volumeOrder = BusinessLabels.Keys.ToList();
        var volumeRows = r.Models
            .Where(m => m.Accessible && BusinessLabels.ContainsKey(m.Model))
            .OrderBy(m => volumeOrder.IndexOf(m.Model))
            .Select(m => (Label: BusinessLabels[m.Model], m.RecordCount, m.EarliestDate, m.LatestDate))
            .ToList();

        var sb = new StringBuilder();
        sb.Append($$"""
            <!doctype html>
            <html lang="es">
            <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <title>Biomundo — Diagnóstico de conexión con Odoo</title>
            <style>
              :root { color-scheme: light; }
              * { box-sizing: border-box; }
              body {
                margin: 0; padding: 40px 20px; background: #f4f6f8;
                font-family: -apple-system, "Segoe UI", Roboto, Arial, sans-serif; color: #1c2733;
              }
              .wrap { max-width: 880px; margin: 0 auto; }
              header { margin-bottom: 32px; }
              header h1 { font-size: 1.6rem; margin: 0 0 4px; }
              header p { margin: 0; color: #5b6b7a; }
              .badge {
                display: inline-block; background: #16a34a; color: white; font-size: 0.8rem;
                font-weight: 600; padding: 4px 12px; border-radius: 999px; margin-bottom: 16px;
              }
              .cards { display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap: 16px; margin-bottom: 32px; }
              .card { background: white; border-radius: 12px; padding: 18px; box-shadow: 0 1px 3px rgba(0,0,0,.08); }
              .card .label { font-size: 0.78rem; color: #5b6b7a; text-transform: uppercase; letter-spacing: .03em; margin-bottom: 6px; }
              .card .value { font-size: 1.3rem; font-weight: 700; }
              .card .sublabel { font-size: 0.78rem; color: #5b6b7a; margin-top: 6px; line-height: 1.4; }
              .card .muted { color: #8b98a5; font-style: italic; }
              p.note { font-size: 0.85rem; color: #5b6b7a; margin: 18px 0 0; }
              section { background: white; border-radius: 12px; padding: 24px; margin-bottom: 24px; box-shadow: 0 1px 3px rgba(0,0,0,.08); }
              section h2 { font-size: 1.1rem; margin: 0 0 16px; }
              table { width: 100%; border-collapse: collapse; }
              th, td { text-align: left; padding: 10px 8px; border-bottom: 1px solid #eceff1; font-size: 0.92rem; }
              th { color: #5b6b7a; font-weight: 600; font-size: 0.8rem; text-transform: uppercase; }
              td.num { text-align: right; font-variant-numeric: tabular-nums; }
              footer { color: #8b98a5; font-size: 0.85rem; text-align: center; margin-top: 32px; }
            </style>
            </head>
            <body>
            <div class="wrap">
              <header>
                <span class="badge">✓ Conexión verificada</span>
                <h1>Diagnóstico de conexión con Odoo — {{Enc(company?.Name ?? "Biomundo")}}</h1>
                <p>Generado el {{r.GeneratedAtUtc.ToLocalTime():dd 'de' MMMM 'de' yyyy, HH:mm}} · Preparado por Melirrepu</p>
              </header>

              <div class="cards">
                <div class="card"><div class="label">Empresa</div><div class="value">{{Enc(company?.Name ?? "—")}}</div></div>
                <div class="card"><div class="label">Moneda</div><div class="value">{{Enc(company?.Currency ?? "—")}}</div></div>
                <div class="card"><div class="label">Sistema</div><div class="value">Odoo {{Enc(r.ServerVersion)}}</div></div>
                <div class="card"><div class="label">Tipo de cambio USD/CLP</div><div class="value">{{FormatRate(usdRate?.Rate)}}</div></div>
              </div>

              <section>
                <h2>Qué se verificó</h2>
                <p>Conexión de <strong>solo lectura</strong> al Odoo de {{Enc(company?.Name ?? "Biomundo")}}: no se modificó
                ni se escribió ningún dato. Se confirmó acceso a la información necesaria para construir el flujo de
                caja, el control de importaciones y la proyección de ventas.</p>
              </section>

              <section>
                <h2>Información disponible</h2>
                <table>
                  <thead><tr><th>Información</th><th class="num">Registros</th><th>Período</th></tr></thead>
                  <tbody>
            {{BuildVolumeRows(volumeRows)}}
                  </tbody>
                </table>
              </section>

              <section>
                <h2>Resumen de {{Enc(periodLabel)}}</h2>
                <div class="cards">
                  <div class="card">
                    <div class="label">Ventas netas</div>
                    <div class="value">{{FormatClp(monthly.Sales.NetAmount)}}</div>
                    <div class="sublabel">{{monthly.Sales.InvoiceCount}} facturas · {{monthly.Sales.CreditNoteCount}} notas de crédito · {{monthly.Sales.DistinctPartners}} clientes</div>
                  </div>
                  <div class="card">
                    <div class="label">Compras netas</div>
                    <div class="value">{{FormatClp(monthly.Purchases.NetAmount)}}</div>
                    <div class="sublabel">{{monthly.Purchases.InvoiceCount}} facturas · {{monthly.Purchases.CreditNoteCount}} notas de crédito · {{monthly.Purchases.DistinctPartners}} proveedores</div>
                  </div>
                  <div class="card">
                    <div class="label">RRHH</div>
            {{BuildRrhhCardBody(monthly.Rrhh)}}
                  </div>
                  <div class="card">
                    <div class="label">Factoring</div>
            {{BuildFactoringCardBody(monthly.Factoring)}}
                  </div>
                </div>
                <p class="note">Ventas y compras corresponden a facturas publicadas en Odoo con fecha dentro del período
                (netas de notas de crédito). Órdenes de venta confirmadas en el período: {{monthly.Sales.ConfirmedOrderCount}}
                por {{FormatClp(monthly.Sales.ConfirmedOrderAmount)}}. Órdenes de compra confirmadas: {{monthly.Purchases.ConfirmedOrderCount}}
                por {{FormatClp(monthly.Purchases.ConfirmedOrderAmount)}}.</p>
              </section>

              <footer>Informe generado automáticamente a partir de una consulta de solo lectura a Odoo · Biomundo / Melirrepu</footer>
            </div>
            </body>
            </html>
            """);
        return sb.ToString();
    }

    private static string BuildRrhhCardBody(RrhhSummary rrhh)
    {
        if (!rrhh.ModuleAccessible)
            return """            <div class="value muted">Sin datos</div><div class="sublabel">Módulo de RRHH no disponible en Odoo</div>""";

        var wageLine = rrhh.NetWageTotal is { } wage
            ? $"{FormatClp(wage)} en liquidaciones"
            : $"{rrhh.PayslipsInPeriod?.ToString() ?? "—"} liquidaciones";

        return $"""
                        <div class="value">{rrhh.ActiveEmployees} empleados</div>
                        <div class="sublabel">{Enc(wageLine)} en el período</div>
            """.TrimEnd();
    }

    private static string BuildFactoringCardBody(FactoringSummary factoring)
    {
        if (!factoring.Determinable)
            return """            <div class="value muted">Por confirmar</div><div class="sublabel">Odoo no tiene hoy un campo que identifique con certeza qué facturas están factorizadas</div>""";

        var statusLine = string.Join(" · ", factoring.ByStatus.Select(kv => $"{kv.Value} {kv.Key}"));
        return $"""
                        <div class="value">{FormatClp(factoring.Amount)}</div>
                        <div class="sublabel">{factoring.InvoiceCount} facturas · {Enc(statusLine)}</div>
            """.TrimEnd();
    }

    private static string FormatClp(decimal amount) => $"${amount.ToString("N0", ClpCulture)}";

    private static string BuildVolumeRows(List<(string Label, int RecordCount, DateOnly? EarliestDate, DateOnly? LatestDate)> rows)
    {
        var sb = new StringBuilder();
        foreach (var row in rows)
        {
            var period = row.EarliestDate is null ? "—" : $"{row.EarliestDate:dd-MM-yyyy} a {row.LatestDate:dd-MM-yyyy}";
            sb.AppendLine($"""            <tr><td>{Enc(row.Label)}</td><td class="num">{row.RecordCount:N0}</td><td>{Enc(period)}</td></tr>""");
        }
        return sb.ToString().TrimEnd('\n', '\r');
    }

    private static string FormatRate(decimal? rate) => rate is null or 0 ? "—" : $"1 USD ≈ ${1m / rate.Value:N0} CLP";

    private static string Enc(string value) => WebUtility.HtmlEncode(value);
}
