using Biomundo.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

// ExposedViaTunnel: true cuando se corre detrás de devtunnel (spike del módulo de control de vendedores,
// ver docs/plan-desarrollo.md). El túnel ya termina TLS; redirigir a HTTPS localmente rompe la URL pública
// del túnel. Se activa solo con la variable de entorno, nunca por defecto.
var exposedViaTunnel = builder.Configuration.GetValue<bool>("ExposedViaTunnel");
if (!exposedViaTunnel)
{
    app.UseHttpsRedirection();
}

// Permite que Microsoft Teams incruste la app en un iframe (requerido por las Tabs de Teams).
// Sin este header, el navegador/webview bloquea el iframe por X-Frame-Options/CSP por defecto.
app.Use(async (context, next) =>
{
    context.Response.Headers["Content-Security-Policy"] =
        "frame-ancestors https://teams.microsoft.com https://*.teams.microsoft.com " +
        "https://*.cloud.microsoft https://teams.live.com https://*.skype.com " +
        "https://outlook.office.com https://outlook.office365.com;";
    await next();
});

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
