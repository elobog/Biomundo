namespace Biomundo.Odoo;

/// <summary>Protocolo de la API externa de Odoo.</summary>
public enum OdooProtocol
{
    /// <summary>Detecta la versión del servidor: JSON-2 en Odoo 19+, JSON-RPC en versiones anteriores.</summary>
    Auto,

    /// <summary>API clásica <c>/jsonrpc</c> (obsoleta a partir de Odoo 20).</summary>
    JsonRpc,

    /// <summary>API <c>/json/2/{modelo}/{método}</c> con API key como bearer (Odoo 19+).</summary>
    Json2,
}

/// <summary>Configuración de conexión a Odoo. Sección <c>Odoo</c> (user-secrets / Key Vault).</summary>
public sealed class OdooOptions
{
    public const string SectionName = "Odoo";

    /// <summary>URL base, p. ej. <c>https://biomundo.odoo.com</c>.</summary>
    public string Url { get; set; } = "";

    /// <summary>Nombre de la base de datos. En Odoo Online se infiere del subdominio si se deja vacío.</summary>
    public string? Database { get; set; }

    /// <summary>Login del usuario de integración (normalmente su correo).</summary>
    public string Username { get; set; } = "";

    /// <summary>API key generada en Odoo (Preferencias → Seguridad de la cuenta). Nunca se registra en logs.</summary>
    public string ApiKey { get; set; } = "";

    public OdooProtocol Protocol { get; set; } = OdooProtocol.Auto;

    /// <summary>Registros por página en lecturas masivas.</summary>
    public int PageSize { get; set; } = 500;

    public int TimeoutSeconds { get; set; } = 120;

    public Uri BaseUri => new(Url.TrimEnd('/') + "/");

    /// <summary>
    /// Devuelve el nombre de base de datos configurado. No se adivina desde la URL: en Odoo Online multi-tenant
    /// clásico el subdominio suele coincidir con la base, pero en instancias alojadas por un partner
    /// (como la de Biomundo, <c>kpbchile-biomundo.odoo.com</c>) el nombre real es distinto
    /// (p. ej. <c>kpbchile-biomundo-main-24386933</c>) aunque la URL también termine en <c>.odoo.com</c>.
    /// Adivinar generó un diagnóstico erróneo en la prueba de conexión inicial; por eso ahora es obligatorio.
    /// Para obtenerlo: en el navegador, con la sesión de Odoo abierta, Ctrl+U → buscar <c>"db":"</c>.
    /// </summary>
    public string ResolveDatabase() =>
        !string.IsNullOrWhiteSpace(Database)
            ? Database
            : throw new InvalidOperationException(
                "Falta 'Odoo:Database'. No se infiere automáticamente porque el subdominio no siempre coincide " +
                "con el nombre real de la base (ver comentario en OdooOptions.ResolveDatabase).");

    public void Validate()
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(Url)) missing.Add("Odoo:Url");
        if (string.IsNullOrWhiteSpace(Database)) missing.Add("Odoo:Database");
        if (string.IsNullOrWhiteSpace(Username)) missing.Add("Odoo:Username");
        if (string.IsNullOrWhiteSpace(ApiKey)) missing.Add("Odoo:ApiKey");
        if (missing.Count > 0)
            throw new InvalidOperationException($"Falta configuración de Odoo: {string.Join(", ", missing)}.");

        if (!Uri.TryCreate(Url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("Odoo:Url debe ser una URL https absoluta.");
    }
}
