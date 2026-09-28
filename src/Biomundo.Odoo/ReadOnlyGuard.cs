namespace Biomundo.Odoo;

/// <summary>
/// La aplicación solo lee de Odoo. Cualquier método fuera de esta lista se rechaza antes de salir a la red,
/// como protección adicional a los permisos del usuario de integración.
/// </summary>
public static class ReadOnlyGuard
{
    private static readonly HashSet<string> AllowedMethods = new(StringComparer.Ordinal)
    {
        "search_read",
        "search_count",
        "search",
        "read",
        "fields_get",
        "name_search",
        "context_get",
        "has_group",
        "check_access_rights",
    };

    public static bool IsAllowed(string method) => AllowedMethods.Contains(method);

    public static void EnsureAllowed(string model, string method)
    {
        if (!IsAllowed(method))
            throw new InvalidOperationException(
                $"Método '{model}.{method}' bloqueado: el cliente Odoo de Biomundo es de solo lectura.");
    }
}
