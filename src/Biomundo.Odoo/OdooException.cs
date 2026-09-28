using System.Net;

namespace Biomundo.Odoo;

/// <summary>Error devuelto por Odoo o por el transporte hacia Odoo.</summary>
public class OdooException(string message, string? odooErrorName = null, HttpStatusCode? statusCode = null, Exception? inner = null)
    : Exception(message, inner)
{
    /// <summary>Nombre de la excepción Python en Odoo, p. ej. <c>odoo.exceptions.AccessError</c>.</summary>
    public string? OdooErrorName { get; } = odooErrorName;

    public HttpStatusCode? StatusCode { get; } = statusCode;

    public bool IsAccessError =>
        OdooErrorName?.EndsWith("AccessError", StringComparison.Ordinal) == true || StatusCode == HttpStatusCode.Forbidden;
}

public sealed class OdooAuthenticationException(string message, HttpStatusCode? statusCode = null)
    : OdooException(message, "AuthenticationError", statusCode);
