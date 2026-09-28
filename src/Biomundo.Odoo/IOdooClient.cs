using System.Text.Json.Nodes;

namespace Biomundo.Odoo;

/// <summary>Cliente de solo lectura hacia la API externa de Odoo.</summary>
public interface IOdooClient
{
    OdooProtocol Protocol { get; }

    Task<OdooServerVersion> GetVersionAsync(CancellationToken ct = default);

    /// <summary>Valida las credenciales y devuelve el id (uid) del usuario de integración.</summary>
    Task<int> AuthenticateAsync(CancellationToken ct = default);

    /// <summary>
    /// Invoca un método de modelo con argumentos por nombre. <paramref name="ids"/> identifica los registros
    /// para métodos de registro (<c>read</c>, <c>has_group</c>); se omite en métodos de modelo (<c>search_read</c>).
    /// </summary>
    Task<JsonNode?> CallAsync(string model, string method, JsonObject? kwargs = null, IReadOnlyList<int>? ids = null, CancellationToken ct = default);
}
