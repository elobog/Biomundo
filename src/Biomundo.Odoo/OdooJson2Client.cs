using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Biomundo.Odoo;

/// <summary>
/// Cliente sobre <c>POST /json/2/{modelo}/{método}</c> (Odoo 19+). Autentica cada llamada con la API key
/// como bearer; los argumentos van por nombre y los ids de registro en <c>ids</c>.
/// No usado con Biomundo hoy (Odoo 18), pero queda listo para cuando actualicen de versión.
/// </summary>
public sealed class OdooJson2Client(HttpClient http, OdooOptions options) : IOdooClient
{
    private readonly string _database = options.ResolveDatabase();

    public OdooProtocol Protocol => OdooProtocol.Json2;

    public Task<OdooServerVersion> GetVersionAsync(CancellationToken ct = default) => OdooVersionProbe.GetAsync(http, ct);

    public async Task<int> AuthenticateAsync(CancellationToken ct = default)
    {
        var context = await CallAsync("res.users", "context_get", ct: ct);
        return context?["uid"]?.GetValue<int>()
               ?? throw new OdooAuthenticationException("Odoo no devolvió el uid del usuario de la API key.");
    }

    public async Task<JsonNode?> CallAsync(string model, string method, JsonObject? kwargs = null, IReadOnlyList<int>? ids = null, CancellationToken ct = default)
    {
        ReadOnlyGuard.EnsureAllowed(model, method);

        var body = (JsonObject?)kwargs?.DeepClone() ?? new JsonObject();
        if (ids is not null)
            body["ids"] = new JsonArray(ids.Select(id => (JsonNode)id).ToArray());

        using var request = new HttpRequestMessage(HttpMethod.Post, $"json/2/{model}/{method}")
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("bearer", options.ApiKey);
        request.Headers.Add("X-Odoo-Database", _database);

        using var response = await http.SendAsync(request, ct);
        var text = await response.Content.ReadAsStringAsync(ct);

        if (response.IsSuccessStatusCode)
            return string.IsNullOrEmpty(text) ? null : JsonNode.Parse(text);

        var (name, message) = ParseError(text);
        message ??= $"Odoo respondió HTTP {(int)response.StatusCode} en /json/2/{model}/{method}.";

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new OdooAuthenticationException(message, response.StatusCode);
        throw new OdooException(message, name, response.StatusCode);
    }

    private static (string? Name, string? Message) ParseError(string text)
    {
        try
        {
            return JsonNode.Parse(text) is JsonObject error
                ? (error["name"]?.GetValue<string>(), error["message"]?.GetValue<string>())
                : (null, null);
        }
        catch (JsonException)
        {
            return (null, null);
        }
    }
}
