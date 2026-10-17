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
// Blazor agrega sus propias cabeceras anti-clickjacking (X-Frame-Options: SAMEORIGIN y
// Content-Security-Policy: frame-ancestors 'self') más abajo en el pipeline, en la respuesta final.
// Si fijamos nuestra CSP aquí arriba con un middleware normal, Blazor agrega la suya DESPUÉS y el
// navegador aplica la intersección de ambas (la más restrictiva gana), bloqueando a Teams igual.
// Por eso se hace en Response.OnStarting: ese callback corre justo antes de enviar la respuesta,
// después de que todo el pipeline (incluido Blazor) ya puso sus cabeceras, así las sobrescribimos.
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        context.Response.Headers.Remove("X-Frame-Options");
        context.Response.Headers["Content-Security-Policy"] =
            "frame-ancestors https://teams.microsoft.com https://*.teams.microsoft.com " +
            "https://*.cloud.microsoft https://teams.live.com https://*.skype.com " +
            "https://outlook.office.com https://outlook.office365.com;";
        return Task.CompletedTask;
    });
    await next();
});

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
