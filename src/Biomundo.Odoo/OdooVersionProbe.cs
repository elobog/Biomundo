using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace Biomundo.Odoo;

/// <summary>Consulta la versión del servidor sin autenticación.</summary>
internal static class OdooVersionProbe
{
    public static async Task<OdooServerVersion> GetAsync(HttpClient http, CancellationToken ct)
    {
        try
        {
            var result = await OdooJsonRpcClient.PostRpcAsync(http, "common", "version", [], 0, ct);
            return OdooServerVersion.Parse(result);
        }
        catch (Exception ex) when (ex is OdooException or HttpRequestException)
        {
            // /jsonrpc puede no existir en versiones futuras (Odoo 20+): se intenta el endpoint web.
            using var response = await http.GetAsync("web/version", ct);
            if (!response.IsSuccessStatusCode)
                throw new OdooException("No se pudo obtener la versión de Odoo por /jsonrpc ni por /web/version.", statusCode: response.StatusCode, inner: ex);
            return OdooServerVersion.Parse(await response.Content.ReadFromJsonAsync<JsonNode>(ct));
        }
    }
}
