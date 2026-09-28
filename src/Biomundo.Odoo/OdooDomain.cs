using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Biomundo.Odoo;

/// <summary>
/// Dominio de búsqueda de Odoo (lista de condiciones unidas por AND implícito),
/// p. ej. <c>OdooDomain.Where("state", "=", "posted").And("move_type", "in", new[] { "out_invoice" })</c>.
/// Para valores vacíos Odoo usa <c>false</c>, no <c>null</c>.
/// </summary>
public sealed class OdooDomain
{
    private readonly JsonArray _terms = [];

    public static OdooDomain All => new();

    public static OdooDomain Where(string field, string op, object? value) => new OdooDomain().And(field, op, value);

    public OdooDomain And(string field, string op, object? value)
    {
        _terms.Add(new JsonArray(JsonValue.Create(field), JsonValue.Create(op), JsonSerializer.SerializeToNode(value)));
        return this;
    }

    /// <summary>Copia del dominio con una condición adicional, sin modificar el original.</summary>
    public OdooDomain With(string field, string op, object? value)
    {
        var copy = new OdooDomain();
        foreach (var term in _terms)
            copy._terms.Add(term?.DeepClone());
        return copy.And(field, op, value);
    }

    private static readonly JsonSerializerOptions DebugOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public JsonArray ToJson() => (JsonArray)_terms.DeepClone();

    /// <summary>Representación legible para logs/depuración (operadores como <c>&gt;</c> sin escapar a <c>></c>).</summary>
    public override string ToString() => _terms.ToJsonString(DebugOptions);
}
