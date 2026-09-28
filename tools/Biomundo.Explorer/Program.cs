using Biomundo.Explorer;
using Biomundo.Odoo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddUserSecrets<Program>();
builder.Services.AddOdooClient(builder.Configuration);
builder.Services.AddSingleton<OdooDiagnostics>();
builder.Services.AddSingleton<OdooMonthlySummaryService>();

using var host = builder.Build();

Console.WriteLine("Biomundo — Diagnóstico de Odoo (solo lectura)");
Console.WriteLine("==============================================");

try
{
    var diagnostics = host.Services.GetRequiredService<OdooDiagnostics>();
    Console.WriteLine("Conectando y recorriendo modelos...");
    var report = await diagnostics.RunAsync();

    Console.WriteLine("Calculando resumen del último mes (ventas, compras, RRHH, factoring)...");
    var monthlySummaryService = host.Services.GetRequiredService<OdooMonthlySummaryService>();
    var monthly = await monthlySummaryService.RunAsync();
    if (monthly.Warnings.Count > 0)
        foreach (var w in monthly.Warnings)
            Console.WriteLine($"  Aviso: {w}");

    var outputDir = Path.Combine(FindRepoRoot(), "docs", "diagnostico-odoo");
    var (mdPath, jsonPath) = await ReportWriter.WriteAsync(report, outputDir);
    var summaryPath = await ExecutiveSummaryWriter.WriteAsync(report, monthly, outputDir);

    Console.WriteLine();
    Console.WriteLine($"Versión de Odoo: {report.ServerVersion} (protocolo {report.Protocol})");
    Console.WriteLine($"Compañías: {report.Companies.Count}");
    Console.WriteLine($"Modelos inspeccionados: {report.Models.Count} ({report.Models.Count(m => m.Accessible)} accesibles)");
    if (report.Warnings.Count > 0)
        Console.WriteLine($"Advertencias: {report.Warnings.Count} (ver informe)");
    Console.WriteLine();
    Console.WriteLine($"Informe Markdown:     {mdPath}");
    Console.WriteLine($"Informe JSON:         {jsonPath}");
    Console.WriteLine($"Resumen ejecutivo:    {summaryPath}");
    return 0;
}
catch (OdooAuthenticationException ex)
{
    Console.Error.WriteLine($"Error de autenticación con Odoo: {ex.Message}");
    Console.Error.WriteLine("Revise Odoo:Url, Odoo:Username y Odoo:ApiKey en 'dotnet user-secrets' (ver README).");
    return 1;
}
catch (OdooException ex)
{
    Console.Error.WriteLine($"Error de Odoo: {ex.Message}");
    return 1;
}
catch (InvalidOperationException ex)
{
    Console.Error.WriteLine($"Configuración inválida: {ex.Message}");
    return 1;
}

/// <summary>
/// Ubica la raíz del repositorio (carpeta con Biomundo.slnx) a partir del directorio de ejecución.
/// Necesario porque Directory.Build.props envía bin/obj fuera del repo (a %LOCALAPPDATA%), así que
/// contar niveles fijos desde AppContext.BaseDirectory ya no apunta al lugar correcto.
/// </summary>
static string FindRepoRoot()
{
    // AppContext.BaseDirectory no sirve: Directory.Build.props envía bin/obj a %LOCALAPPDATA%, fuera del repo.
    // Directory.GetCurrentDirectory() sí sirve con 'dotnet run' (queda en la carpeta del proyecto) y con 'dotnet exec'
    // ejecutado desde la raíz del repo.
    foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
    {
        var dir = new DirectoryInfo(start);
        while (dir is not null && !dir.EnumerateFiles("Biomundo.slnx").Any())
            dir = dir.Parent;
        if (dir is not null)
            return dir.FullName;
    }

    throw new InvalidOperationException(
        "No se encontró Biomundo.slnx subiendo desde el directorio actual ni desde el de ejecución.");
}
