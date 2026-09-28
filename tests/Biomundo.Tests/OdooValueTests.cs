using System.Text.Json.Nodes;
using Biomundo.Odoo;

namespace Biomundo.Tests;

public class OdooValueTests
{
    [Fact]
    public void GetString_DevuelveNull_CuandoOdooDevuelveFalse()
    {
        var record = JsonNode.Parse("""{"name": false}""")!.AsObject();
        Assert.Null(record.GetString("name"));
    }

    [Fact]
    public void GetString_DevuelveElValor_CuandoExiste()
    {
        var record = JsonNode.Parse("""{"name": "Biomundo SPA"}""")!.AsObject();
        Assert.Equal("Biomundo SPA", record.GetString("name"));
    }

    [Fact]
    public void GetMany2One_InterpretaParIdNombre()
    {
        var record = JsonNode.Parse("""{"currency_id": [45, "CLP"]}""")!.AsObject();
        var value = record.GetMany2One("currency_id");
        Assert.Equal((45, "CLP"), value);
        Assert.Equal("CLP", record.GetMany2OneName("currency_id"));
    }

    [Fact]
    public void GetMany2One_DevuelveNull_CuandoCampoVacio()
    {
        var record = JsonNode.Parse("""{"partner_id": false}""")!.AsObject();
        Assert.Null(record.GetMany2One("partner_id"));
    }

    [Fact]
    public void GetDate_InterpretaFechaYFechaHora()
    {
        var record = JsonNode.Parse("""{"date_maturity": "2026-10-15", "create_date": "2026-09-28 14:30:00"}""")!.AsObject();
        Assert.Equal(new DateOnly(2026, 10, 15), record.GetDate("date_maturity"));
        Assert.Equal(new DateOnly(2026, 9, 28), record.GetDate("create_date"));
    }

    [Fact]
    public void GetBool_TrataAusenteYFalseComoFalso()
    {
        var record = JsonNode.Parse("""{"active": false}""")!.AsObject();
        Assert.False(record.GetBool("active"));
        Assert.False(record.GetBool("no_existe"));
    }
}
