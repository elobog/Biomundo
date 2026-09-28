using Biomundo.Odoo;

namespace Biomundo.Tests;

public class OdooOptionsTests
{
    [Fact]
    public void ResolveDatabase_UsaElValorExplicito_SiEstaConfigurado()
    {
        var options = new OdooOptions { Url = "https://kpbchile-biomundo.odoo.com", Database = "kpbchile-biomundo-main-24386933" };
        Assert.Equal("kpbchile-biomundo-main-24386933", options.ResolveDatabase());
    }

    [Fact]
    public void ResolveDatabase_Falla_SiNoSeConfiguraExplicitamente()
    {
        // Caso real de Biomundo: aunque la URL termina en .odoo.com (kpbchile-biomundo.odoo.com),
        // el nombre real de la base (kpbchile-biomundo-main-24386933) NO coincide con el subdominio.
        // Por eso no se adivina: se exige siempre 'Odoo:Database' explícito.
        var options = new OdooOptions { Url = "https://kpbchile-biomundo.odoo.com" };
        Assert.Throws<InvalidOperationException>(() => options.ResolveDatabase());
    }

    [Fact]
    public void ResolveDatabase_DevuelveElValorConfigurado_AunqueLaUrlSeaOdooOnlineEstandar()
    {
        var options = new OdooOptions { Url = "https://miempresa.odoo.com", Database = "miempresa" };
        Assert.Equal("miempresa", options.ResolveDatabase());
    }

    [Fact]
    public void Validate_ExigeUrlUsuarioYApiKey()
    {
        var options = new OdooOptions();
        var ex = Assert.Throws<InvalidOperationException>(options.Validate);
        Assert.Contains("Odoo:Url", ex.Message);
        Assert.Contains("Odoo:Database", ex.Message);
        Assert.Contains("Odoo:Username", ex.Message);
        Assert.Contains("Odoo:ApiKey", ex.Message);
    }

    [Fact]
    public void Validate_RechazaUrlNoHttps()
    {
        var options = new OdooOptions { Url = "http://biomundo.odoo.com", Username = "u", ApiKey = "k" };
        Assert.Throws<InvalidOperationException>(options.Validate);
    }
}
