using System.Text;
using System.Text.Json;

namespace Biomundo.Explorer;

/// <summary>Escribe el informe de diagnóstico en Markdown (para leer) y JSON (para procesar después).</summary>
public static class ReportWriter
{
    public static async Task<(string MarkdownPath, string JsonPath)> WriteAsync(DiagnosticReport report, string outputDir, CancellationToken ct = default)
    {
        Directory.CreateDirectory(outputDir);
        var stamp = report.GeneratedAtUtc.ToLocalTime().ToString("yyyyMMdd-HHmmss");
        var mdPath = Path.Combine(outputDir, $"diagnostico-odoo-{stamp}.md");
        var jsonPath = Path.Combine(outputDir, $"diagnostico-odoo-{stamp}.json");

        await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }), ct);
        await File.WriteAllTextAsync(mdPath, ToMarkdown(report), Encoding.UTF8, ct);
        return (mdPath, jsonPath);
    }

    private static string ToMarkdown(DiagnosticReport r)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Diagnóstico de Odoo — Biomundo");
        sb.AppendLine();
        sb.AppendLine($"- Generado: {r.GeneratedAtUtc.ToLocalTime():yyyy-MM-dd HH:mm} (hora local)");
        sb.AppendLine($"- URL: {r.OdooUrl}");
        sb.AppendLine($"- Base de datos: `{r.Database}`");
        sb.AppendLine($"- Versión de Odoo: {r.ServerVersion}");
        sb.AppendLine($"- Protocolo usado: {r.Protocol}");
        sb.AppendLine();

        if (r.Warnings.Count > 0)
        {
            sb.AppendLine("## ⚠️ Advertencias");
            foreach (var w in r.Warnings)
                sb.AppendLine($"- {w}");
            sb.AppendLine();
        }

        sb.AppendLine("## Compañías");
        sb.AppendLine("| Id | Nombre | Moneda |");
        sb.AppendLine("|---|---|---|");
        foreach (var c in r.Companies)
            sb.AppendLine($"| {c.Id} | {c.Name} | {c.Currency} |");
        sb.AppendLine();

        sb.AppendLine("## Módulos relevantes");
        sb.AppendLine($"- Instalados: {(r.InstalledRelevantModules.Count > 0 ? string.Join(", ", r.InstalledRelevantModules.Select(m => $"`{m}`")) : "(ninguno detectado)")}");
        sb.AppendLine($"- No instalados: {(r.MissingRelevantModules.Count > 0 ? string.Join(", ", r.MissingRelevantModules.Select(m => $"`{m}`")) : "(ninguno)")}");
        sb.AppendLine();

        sb.AppendLine("## Tasas de cambio registradas (últimas 10)");
        if (r.CurrencyRates.Count == 0)
        {
            sb.AppendLine("_No se pudo leer `res.currency.rate`._");
        }
        else
        {
            sb.AppendLine("| Moneda | Fecha | Tasa (tal como la guarda Odoo) |");
            sb.AppendLine("|---|---|---|");
            foreach (var c in r.CurrencyRates)
                sb.AppendLine($"| {c.Currency} | {c.RateDate?.ToString("yyyy-MM-dd") ?? "?"} | {c.Rate} |");
        }
        sb.AppendLine();

        sb.AppendLine("## Modelos");
        sb.AppendLine("| Modelo | Uso | Registros | Desde | Hasta | Campos personalizados |");
        sb.AppendLine("|---|---|---|---|---|---|");
        foreach (var m in r.Models)
        {
            if (!m.Accessible)
            {
                sb.AppendLine($"| `{m.Model}` | {m.Purpose} | ⛔ sin acceso | — | — | {Escape(m.AccessError)} |");
                continue;
            }
            var customFields = m.CustomFields.Count == 0 ? "—" : string.Join(", ", m.CustomFields.Select(f => $"`{f.Name}` ({f.Label}, {f.Type})"));
            sb.AppendLine($"| `{m.Model}` | {m.Purpose} | {m.RecordCount:N0} | {m.EarliestDate?.ToString("yyyy-MM-dd") ?? "—"} | {m.LatestDate?.ToString("yyyy-MM-dd") ?? "—"} | {customFields} |");
        }
        sb.AppendLine();

        sb.AppendLine("## Siguiente paso");
        sb.AppendLine("Revisar este informe junto al usuario antes de diseñar el esquema `stg`/`dw` en SQL Server (fase 2),");
        sb.AppendLine("en particular los campos personalizados detectados en `purchase.order` y `stock.picking`,");
        sb.AppendLine("que suelen indicar cómo registra Biomundo hoy sus importaciones (DIN, BL, fecha de embarque, agente de aduana).");
        return sb.ToString();
    }

    private static string Escape(string? text) => (text ?? "").Replace("|", "\\|").Replace("\n", " ");
}
