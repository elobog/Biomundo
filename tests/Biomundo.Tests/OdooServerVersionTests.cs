using System.Text.Json.Nodes;
using Biomundo.Odoo;

namespace Biomundo.Tests;

public class OdooServerVersionTests
{
    [Fact]
    public void Parse_Odoo18Enterprise_NoSoportaJson2()
    {
        // Respuesta real observada en la instancia de Biomundo (common.version).
        var node = JsonNode.Parse("""
            {"server_version": "18.0+e", "server_version_info": [18, 0, 0, "final", 0, "e"], "server_serie": "18.0", "protocol_version": 1}
            """);

        var version = OdooServerVersion.Parse(node);

        Assert.Equal(18, version.Major);
        Assert.Equal(0, version.Minor);
        Assert.False(version.SupportsJson2);
    }

    [Fact]
    public void Parse_Odoo19_SoportaJson2()
    {
        var node = JsonNode.Parse("""
            {"server_version": "19.0", "server_version_info": [19, 0, 0, "final", 0, ""]}
            """);

        var version = OdooServerVersion.Parse(node);

        Assert.Equal(19, version.Major);
        Assert.True(version.SupportsJson2);
    }

    [Fact]
    public void Parse_SerieSaas_InterpretaVersionTextual()
    {
        var node = JsonNode.Parse("""
            {"server_version": "saas~18.3+e", "server_version_info": ["saas~18", 3, 0, "final", 0, "e"]}
            """);

        var version = OdooServerVersion.Parse(node);

        Assert.Equal(18, version.Major);
        Assert.Equal(3, version.Minor);
        Assert.True(version.IsSaas);
    }

    [Fact]
    public void Parse_SinVersionInfo_CaeAlPatronDeTexto()
    {
        var node = JsonNode.Parse("""{"version": "18.4"}""");

        var version = OdooServerVersion.Parse(node);

        Assert.Equal(18, version.Major);
        Assert.Equal(4, version.Minor);
    }
}
