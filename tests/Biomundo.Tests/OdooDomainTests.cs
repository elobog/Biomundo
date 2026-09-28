using Biomundo.Odoo;

namespace Biomundo.Tests;

public class OdooDomainTests
{
    [Fact]
    public void Where_GeneraUnaCondicion()
    {
        var domain = OdooDomain.Where("state", "=", "posted");
        Assert.Equal("""[["state","=","posted"]]""", domain.ToString());
    }

    [Fact]
    public void And_AcumulaCondiciones()
    {
        var domain = OdooDomain.Where("state", "=", "posted").And("move_type", "=", "out_invoice");
        Assert.Equal("""[["state","=","posted"],["move_type","=","out_invoice"]]""", domain.ToString());
    }

    [Fact]
    public void With_NoModificaElDominioOriginal()
    {
        var original = OdooDomain.Where("state", "=", "posted");
        var extended = original.With("id", ">", 100);

        Assert.Equal("""[["state","=","posted"]]""", original.ToString());
        Assert.Equal("""[["state","=","posted"],["id",">",100]]""", extended.ToString());
    }

    [Fact]
    public void Where_SerializaListasComoArregloJson()
    {
        var domain = OdooDomain.Where("id", "in", new[] { 1, 2, 3 });
        Assert.Equal("""[["id","in",[1,2,3]]]""", domain.ToString());
    }
}
