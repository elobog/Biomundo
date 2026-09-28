using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace Biomundo.Odoo;

/// <summary>
/// Cliente sobre <c>POST /jsonrpc</c> (servicios <c>common</c> y <c>object.execute_kw</c>).
/// Compatible con Odoo 8 a 19; Odoo lo marca obsoleto para Odoo 20.
/// </summary>
public sealed class OdooJsonRpcClient(HttpClient http, OdooOptions options) : IOdooClient
{
    private readonly string _database = options.ResolveDatabase();
    private int? _uid;
    private int _requestId;

    public OdooProtocol Protocol => OdooProtocol.JsonRpc;

    public Task<OdooServerVersion> GetVersionAsync(CancellationToken ct = default) => OdooVersionProbe.GetAsync(http, ct);

    public async Task<int> AuthenticateAsync(CancellationToken ct = default)
    {
        var result = await PostRpcAsync(http, "common", "authenticate",
            [_database, options.Username, options.ApiKey, new JsonObject()], NextId(), ct);

        if (result is not JsonValue value || !value.TryGetValue<int>(out var uid))
            throw new OdooAuthenticationException(
                $"Odoo rechazó las credenciales del usuario '{options.Username}' en la base '{_database}'.");

        _uid = uid;
        return uid;
    }

    public async Task<JsonNode?> CallAsync(string model, string method, JsonObject? kwargs = null, IReadOnlyList<int>? ids = null, CancellationToken ct = default)
    {
        ReadOnlyGuard.EnsureAllowed(model, method);
        var uid = _uid ?? await AuthenticateAsync(ct);

        var args = new JsonArray();
        if (ids is not null)
            args.Add(new JsonArray(ids.Select(id => (JsonNode)id).ToArray()));

        return await PostRpcAsync(http, "object", "execute_kw",
            [_database, uid, options.ApiKey, model, method, args, kwargs ?? new JsonObject()], NextId(), ct);
    }

    private int NextId() => Interlocked.Increment(ref _requestId);

    internal static async Task<JsonNode?> PostRpcAsync(HttpClient http, string service, string method, JsonArray args, int id, CancellationToken ct)
    {
        var payload = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["method"] = "call",
            ["params"] = new JsonObject { ["service"] = service, ["method"] = method, ["args"] = args },
            ["id"] = id,
        };

        using var response = await http.PostAsJsonAsync("jsonrpc", payload, ct);
        if (!response.IsSuccessStatusCode)
            throw new OdooException($"Odoo respondió HTTP {(int)response.StatusCode} en /jsonrpc ({service}.{method}).", statusCode: response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonObject>(ct)
                   ?? throw new OdooException("Respuesta vacía de Odoo.");

        if (body["error"] is JsonObject error)
        {
            var data = error["data"] as JsonObject;
            var message = data?["message"]?.GetValue<string>() ?? error["message"]?.GetValue<string>() ?? "Error de Odoo";
            throw new OdooException(message, data?["name"]?.GetValue<string>());
        }

        return body["result"];
    }
}
