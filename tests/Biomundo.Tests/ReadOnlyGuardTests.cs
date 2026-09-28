using Biomundo.Odoo;

namespace Biomundo.Tests;

public class ReadOnlyGuardTests
{
    [Theory]
    [InlineData("search_read")]
    [InlineData("search_count")]
    [InlineData("read")]
    [InlineData("fields_get")]
    public void MetodosDeLectura_EstanPermitidos(string method) =>
        ReadOnlyGuard.EnsureAllowed("account.move", method); // no lanza

    [Theory]
    [InlineData("write")]
    [InlineData("create")]
    [InlineData("unlink")]
    [InlineData("action_post")]
    [InlineData("button_confirm")]
    public void MetodosDeEscritura_SeRechazan(string method)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => ReadOnlyGuard.EnsureAllowed("account.move", method));
        Assert.Contains("solo lectura", ex.Message);
    }
}
