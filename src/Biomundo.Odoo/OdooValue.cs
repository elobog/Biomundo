using System.Globalization;
using System.Text.Json.Nodes;

namespace Biomundo.Odoo;

/// <summary>
/// Lectura de valores de registros Odoo. Odoo devuelve <c>false</c> para campos vacíos
/// y <c>[id, "nombre"]</c> para campos many2one.
/// </summary>
public static class OdooValue
{
    public static string? GetString(this JsonObject record, string field) =>
        record[field] is JsonValue v && v.TryGetValue<string>(out var s) ? s : null;

    public static int? GetInt(this JsonObject record, string field) =>
        record[field] is JsonValue v && v.TryGetValue<int>(out var n) ? n : null;

    public static decimal? GetDecimal(this JsonObject record, string field) =>
        record[field] is JsonValue v && v.TryGetValue<decimal>(out var d) ? d : null;

    public static bool GetBool(this JsonObject record, string field) =>
        record[field] is JsonValue v && v.TryGetValue<bool>(out var b) && b;

    public static (int Id, string Name)? GetMany2One(this JsonObject record, string field) =>
        record[field] is JsonArray { Count: >= 2 } a && a[0] is JsonValue id && id.TryGetValue<int>(out var n)
            ? (n, a[1]?.GetValue<string>() ?? "")
            : null;

    public static string? GetMany2OneName(this JsonObject record, string field) => record.GetMany2One(field)?.Name;

    /// <summary>Fecha de un campo date (<c>yyyy-MM-dd</c>) o datetime (<c>yyyy-MM-dd HH:mm:ss</c>, UTC).</summary>
    public static DateOnly? GetDate(this JsonObject record, string field)
    {
        var s = record.GetString(field);
        return s is { Length: >= 10 } && DateOnly.TryParseExact(s[..10], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d
            : null;
    }
}
