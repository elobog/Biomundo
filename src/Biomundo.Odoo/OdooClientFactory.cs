using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Biomundo.Odoo;

/// <summary>Crea clientes Odoo según el protocolo configurado o detectado.</summary>
public sealed class OdooClientFactory(IHttpClientFactory httpClientFactory, IOptions<OdooOptions> options)
{
    public const string HttpClientName = "Odoo";

    public OdooOptions Options => options.Value;

    public IOdooClient Create(OdooProtocol protocol)
    {
        var http = httpClientFactory.CreateClient(HttpClientName);
        return protocol switch
        {
            OdooProtocol.JsonRpc => new OdooJsonRpcClient(http, options.Value),
            OdooProtocol.Json2 => new OdooJson2Client(http, options.Value),
            _ => throw new ArgumentOutOfRangeException(nameof(protocol), protocol, "Use CreateAsync para detección automática."),
        };
    }

    /// <summary>Protocolo configurado o, en modo Auto, JSON-2 si el servidor es Odoo 19+ (Biomundo hoy es Odoo 18 → JSON-RPC).</summary>
    public async Task<IOdooClient> CreateAsync(CancellationToken ct = default)
    {
        if (options.Value.Protocol != OdooProtocol.Auto)
            return Create(options.Value.Protocol);

        var version = await Create(OdooProtocol.JsonRpc).GetVersionAsync(ct);
        return Create(version.SupportsJson2 ? OdooProtocol.Json2 : OdooProtocol.JsonRpc);
    }
}

public static class OdooServiceCollectionExtensions
{
    public static IServiceCollection AddOdooClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<OdooOptions>()
            .Bind(configuration.GetSection(OdooOptions.SectionName))
            .Validate(o => { o.Validate(); return true; })
            .ValidateOnStart();

        services.AddHttpClient(OdooClientFactory.HttpClientName, (sp, http) =>
        {
            var o = sp.GetRequiredService<IOptions<OdooOptions>>().Value;
            http.BaseAddress = o.BaseUri;
            http.Timeout = TimeSpan.FromSeconds(o.TimeoutSeconds);
            http.DefaultRequestHeaders.UserAgent.ParseAdd("Biomundo-Integracion/1.0");
        });

        services.AddSingleton<OdooClientFactory>();
        return services;
    }
}
