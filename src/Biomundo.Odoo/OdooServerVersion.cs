using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Biomundo.Odoo;

/// <summary>Versión del servidor Odoo, p. ej. <c>saas~18.3+e</c> o <c>19.0+e</c>.</summary>
public sealed partial record OdooServerVersion(string Version, int Major, int Minor, bool IsSaas)
{
    /// <summary>La API JSON-2 existe desde Odoo 19.</summary>
    public bool SupportsJson2 => Major >= 19;

    /// <summary>
    /// Interpreta la respuesta de <c>common.version</c> (<c>server_version</c>, <c>server_version_info</c>)
    /// o de <c>/web/version</c> (<c>version</c>, <c>version_info</c>).
    /// En series SaaS el primer elemento de version_info puede venir como texto (<c>"saas~18"</c>).
    /// </summary>
    public static OdooServerVersion Parse(JsonNode? node)
    {
        if (node is not JsonObject obj)
            throw new OdooException("Respuesta de versión de Odoo inválida.");

        var version = (obj["server_version"] ?? obj["version"])?.GetValue<string>() ?? "";
        var info = (obj["server_version_info"] ?? obj["version_info"]) as JsonArray;

        var major = info is { Count: > 0 } ? ParseNumber(info[0]) : null;
        var minor = info is { Count: > 1 } ? ParseNumber(info[1]) : null;

        if (major is null)
        {
            var match = VersionPattern().Match(version);
            if (!match.Success)
                throw new OdooException($"No se pudo interpretar la versión de Odoo '{version}'.");
            major = int.Parse(match.Groups[1].Value);
            minor = int.Parse(match.Groups[2].Value);
        }

        return new OdooServerVersion(version, major.Value, minor ?? 0, version.Contains("saas", StringComparison.OrdinalIgnoreCase));
    }

    private static int? ParseNumber(JsonNode? node)
    {
        if (node is not JsonValue value) return null;
        if (value.TryGetValue<int>(out var n)) return n;
        if (value.TryGetValue<string>(out var s))
        {
            var match = LeadingNumber().Match(s);
            if (match.Success) return int.Parse(match.Value);
        }
        return null;
    }

    [GeneratedRegex(@"(\d+)\.(\d+)")]
    private static partial Regex VersionPattern();

    [GeneratedRegex(@"\d+")]
    private static partial Regex LeadingNumber();
}
