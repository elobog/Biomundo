using Biomundo.Odoo;

namespace Biomundo.Explorer;

/// <summary>
/// Recorre Odoo con operaciones de solo lectura (conteos, metadatos de campos y una muestra mínima de fechas)
/// para producir un informe que oriente el diseño del modelo SQL. No lee datos de negocio en volumen.
/// </summary>
public sealed class OdooDiagnostics(OdooClientFactory factory)
{
    public async Task<DiagnosticReport> RunAsync(CancellationToken ct = default)
    {
        var warnings = new List<string>();
        var client = await factory.CreateAsync(ct);
        var version = await client.GetVersionAsync(ct);
        await client.AuthenticateAsync(ct);

        var companies = await GetCompaniesAsync(client, ct);
        var (installed, missing) = await GetModuleStatusAsync(client, warnings, ct);
        var models = new List<ModelSummary>();
        foreach (var (model, purpose) in ModelsToInspect.Items)
            models.Add(await InspectModelAsync(client, model, purpose, ct));

        var rates = await GetCurrencyRatesAsync(client, warnings, ct);

        return new DiagnosticReport
        {
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            OdooUrl = factory.Options.BaseUri.ToString(),
            Database = factory.Options.ResolveDatabase(),
            Protocol = client.Protocol.ToString(),
            ServerVersion = version.Version,
            Companies = companies,
            InstalledRelevantModules = installed,
            MissingRelevantModules = missing,
            Models = models,
            CurrencyRates = rates,
            Warnings = warnings,
        };
    }

    private static async Task<IReadOnlyList<CompanyInfo>> GetCompaniesAsync(IOdooClient client, CancellationToken ct)
    {
        var rows = await client.SearchReadAsync("res.company", fields: ["name", "currency_id"], ct: ct);
        return rows.Select(r => new CompanyInfo(
            r.GetInt("id") ?? 0,
            r.GetString("name") ?? "",
            r.GetMany2OneName("currency_id") ?? "")).ToList();
    }

    private static async Task<(IReadOnlyList<string> Installed, IReadOnlyList<string> Missing)> GetModuleStatusAsync(
        IOdooClient client, List<string> warnings, CancellationToken ct)
    {
        try
        {
            var domain = OdooDomain.Where("name", "in", ModelsToInspect.RelevantModuleTechnicalNames);
            var rows = await client.SearchReadAsync("ir.module.module", domain, ["name", "state"], ct: ct);
            var installed = rows.Where(r => r.GetString("state") == "installed")
                .Select(r => r.GetString("name") ?? "").ToList();
            var missing = ModelsToInspect.RelevantModuleTechnicalNames.Except(installed).ToList();
            return (installed, missing);
        }
        catch (OdooException ex)
        {
            warnings.Add($"No se pudo leer ir.module.module: {ex.Message}");
            return ([], ModelsToInspect.RelevantModuleTechnicalNames);
        }
    }

    private static async Task<ModelSummary> InspectModelAsync(IOdooClient client, string model, string purpose, CancellationToken ct)
    {
        try
        {
            var count = await client.SearchCountAsync(model, ct: ct);

            DateOnly? earliest = null, latest = null;
            if (count > 0)
            {
                var first = await client.SearchReadAsync(model, fields: ["create_date"], limit: 1, order: "create_date asc", ct: ct);
                var last = await client.SearchReadAsync(model, fields: ["create_date"], limit: 1, order: "create_date desc", ct: ct);
                earliest = first.FirstOrDefault()?.GetDate("create_date");
                latest = last.FirstOrDefault()?.GetDate("create_date");
            }

            var customFields = await GetCustomFieldsAsync(client, model, ct);

            return new ModelSummary
            {
                Model = model,
                Purpose = purpose,
                Accessible = true,
                RecordCount = count,
                EarliestDate = earliest,
                LatestDate = latest,
                CustomFields = customFields,
            };
        }
        catch (OdooException ex)
        {
            return new ModelSummary
            {
                Model = model,
                Purpose = purpose,
                Accessible = false,
                AccessError = ex.Message,
            };
        }
    }

    private static async Task<IReadOnlyList<CustomField>> GetCustomFieldsAsync(IOdooClient client, string model, CancellationToken ct)
    {
        var fields = await client.FieldsGetAsync(model, ["string", "type"], ct);
        return fields
            .Where(kv => kv.Key.StartsWith("x_", StringComparison.Ordinal))
            .Select(kv => new CustomField(
                kv.Key,
                kv.Value?["string"]?.GetValue<string>() ?? kv.Key,
                kv.Value?["type"]?.GetValue<string>() ?? "?"))
            .OrderBy(f => f.Name, StringComparer.Ordinal)
            .ToList();
    }

    private static async Task<IReadOnlyList<CurrencyRateInfo>> GetCurrencyRatesAsync(IOdooClient client, List<string> warnings, CancellationToken ct)
    {
        try
        {
            var rows = await client.SearchReadAsync("res.currency.rate",
                fields: ["currency_id", "name", "rate"], limit: 10, order: "name desc", ct: ct);
            return rows.Select(r => new CurrencyRateInfo(
                r.GetMany2OneName("currency_id") ?? "?",
                r.GetDecimal("rate") ?? 0,
                r.GetDate("name"))).ToList();
        }
        catch (OdooException ex)
        {
            warnings.Add($"No se pudo leer res.currency.rate: {ex.Message}");
            return [];
        }
    }
}
