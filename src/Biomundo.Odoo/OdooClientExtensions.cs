using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;

namespace Biomundo.Odoo;

/// <summary>Operaciones de lectura comunes, independientes del protocolo.</summary>
public static class OdooClientExtensions
{
    public static async Task<int> SearchCountAsync(this IOdooClient client, string model, OdooDomain? domain = null, CancellationToken ct = default)
    {
        var result = await client.CallAsync(model, "search_count",
            new JsonObject { ["domain"] = (domain ?? OdooDomain.All).ToJson() }, ct: ct);
        return result?.GetValue<int>() ?? 0;
    }

    public static async Task<IReadOnlyList<JsonObject>> SearchReadAsync(
        this IOdooClient client,
        string model,
        OdooDomain? domain = null,
        IEnumerable<string>? fields = null,
        int? limit = null,
        int? offset = null,
        string? order = null,
        CancellationToken ct = default)
    {
        var kwargs = new JsonObject { ["domain"] = (domain ?? OdooDomain.All).ToJson() };
        if (fields is not null) kwargs["fields"] = ToJsonArray(fields);
        if (limit is not null) kwargs["limit"] = limit;
        if (offset is not null) kwargs["offset"] = offset;
        if (order is not null) kwargs["order"] = order;

        var result = await client.CallAsync(model, "search_read", kwargs, ct: ct);
        return result is JsonArray rows ? rows.OfType<JsonObject>().ToList() : [];
    }

    /// <summary>
    /// Lee todos los registros que cumplen el dominio, paginando por id ascendente (cursor <c>id &gt; último</c>).
    /// Es estable aunque se creen registros durante la lectura, a diferencia de paginar por offset.
    /// </summary>
    public static async IAsyncEnumerable<JsonObject> SearchReadAllAsync(
        this IOdooClient client,
        string model,
        OdooDomain? domain = null,
        IEnumerable<string>? fields = null,
        int pageSize = 500,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        domain ??= OdooDomain.All;
        var fieldList = fields?.ToList();
        var lastId = 0;

        while (true)
        {
            var page = await client.SearchReadAsync(model, domain.With("id", ">", lastId), fieldList, pageSize, order: "id asc", ct: ct);
            foreach (var row in page)
                yield return row;

            if (page.Count < pageSize)
                yield break;
            lastId = page[^1].GetInt("id") ?? throw new OdooException($"Registro de {model} sin id.");
        }
    }

    public static async Task<IReadOnlyList<JsonObject>> ReadAsync(this IOdooClient client, string model, IReadOnlyList<int> ids, IEnumerable<string>? fields = null, CancellationToken ct = default)
    {
        var kwargs = new JsonObject();
        if (fields is not null) kwargs["fields"] = ToJsonArray(fields);
        var result = await client.CallAsync(model, "read", kwargs, ids, ct);
        return result is JsonArray rows ? rows.OfType<JsonObject>().ToList() : [];
    }

    /// <summary>Metadatos de los campos del modelo (<c>nombre → {string, type, relation, ...}</c>).</summary>
    public static async Task<JsonObject> FieldsGetAsync(this IOdooClient client, string model, IEnumerable<string>? attributes = null, CancellationToken ct = default)
    {
        var kwargs = new JsonObject();
        if (attributes is not null) kwargs["attributes"] = ToJsonArray(attributes);
        return await client.CallAsync(model, "fields_get", kwargs, ct: ct) as JsonObject ?? [];
    }

    private static JsonArray ToJsonArray(IEnumerable<string> values) => new(values.Select(v => (JsonNode)v).ToArray());
}
