using System.ClientModel;
using System.ClientModel.Primitives;
using System.Net;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI;

namespace Nomenclador.Api.Services;

// Singleton: mantiene un único cliente HTTP hacia el proveedor del modelo. Es null si el asistente no está configurado.
public sealed class AsistenteChatClientProvider(IOptions<AsistenteOptions> options) : IDisposable
{
    private readonly Lazy<(IChatClient ChatClient, HttpClient HttpClient)?> client = new(() =>
    {
        var config = options.Value;
        if (!config.Habilitado) return null;

        var timeout = TimeSpan.FromSeconds(Math.Max(30, config.TimeoutSegundos));
        var endpoint = new Uri(config.Endpoint);
        var handler = new HttpClientHandler
        {
            UseProxy = true,
            DefaultProxyCredentials = CredentialCache.DefaultCredentials,
        };

        var proxy = HttpClient.DefaultProxy;
        if (!proxy.IsBypassed(endpoint) && proxy.GetProxy(endpoint) is { } proxyAddress)
        {
            // Ignora credenciales embebidas en la URL del proxy: usa la identidad del proceso.
            var address = new UriBuilder(proxyAddress) { UserName = "", Password = "" }.Uri;
            handler.Proxy = new WebProxy(address)
            {
                Credentials = CredentialCache.DefaultCredentials,
            };
        }

        var httpClient = new HttpClient(handler) { Timeout = timeout };
        try
        {
            var openAiClient = new OpenAIClient(
                // Ollama ignora la clave, pero el SDK exige una no vacía.
                new ApiKeyCredential(string.IsNullOrWhiteSpace(config.ApiKey) ? "sin-clave" : config.ApiKey),
                new OpenAIClientOptions
                {
                    Endpoint = endpoint,
                    Transport = new HttpClientPipelineTransport(httpClient),
                    NetworkTimeout = timeout,
                    // Reintentar una generación local lenta sólo multiplica la espera.
                    RetryPolicy = new ClientRetryPolicy(maxRetries: 0),
                });

            return (openAiClient.GetChatClient(config.Model).AsIChatClient(), httpClient);
        }
        catch
        {
            httpClient.Dispose();
            throw;
        }
    });

    public IChatClient? Client => client.Value?.ChatClient;

    public void Dispose()
    {
        if (client.IsValueCreated && client.Value is { } created)
        {
            created.ChatClient.Dispose();
            created.HttpClient.Dispose();
        }
    }
}
