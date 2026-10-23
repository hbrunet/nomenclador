namespace Nomenclador.Api.Services;

public sealed class AsistenteOptions
{
    public const string SectionName = "Asistente";

    // Cualquier API compatible con OpenAI. Por defecto Ollama local; para GitHub Models usar
    // "https://models.github.ai/inference" con un PAT en ApiKey.
    public string Endpoint { get; init; } = "http://localhost:11434/v1";

    public string Model { get; init; } = "qwen2.5:3b";

    // Opcional: Ollama no la requiere. Si se usa un proveedor externo, configurarla vía user-secrets o
    // variable de entorno Asistente__ApiKey; nunca commitearla en appsettings.
    public string? ApiKey { get; init; }

    public int MaxMensajesHistorial { get; init; } = 20;

    // Un modelo local corriendo en CPU puede tardar minutos en responder.
    public int TimeoutSegundos { get; init; } = 300;

    public bool Habilitado => !string.IsNullOrWhiteSpace(Endpoint) && !string.IsNullOrWhiteSpace(Model);
}
