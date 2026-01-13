namespace Acode.Infrastructure.Ollama.Http;

/// <summary>
/// Factory for creating configured OllamaHttpClient instances.
/// </summary>
/// <remarks>
/// Gap #3: Factory pattern for OllamaHttpClient creation.
/// Uses IHttpClientFactory internally for proper connection pooling.
/// </remarks>
public sealed class OllamaHttpClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OllamaConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaHttpClientFactory"/> class.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="configuration">The Ollama configuration.</param>
    public OllamaHttpClientFactory(
        IHttpClientFactory httpClientFactory,
        OllamaConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(configuration);

        this._httpClientFactory = httpClientFactory;
        this._configuration = configuration;
    }

    /// <summary>
    /// Creates a new configured OllamaHttpClient instance.
    /// </summary>
    /// <returns>A configured OllamaHttpClient.</returns>
    /// <remarks>
    /// Each call creates a new OllamaHttpClient instance with a unique correlation ID.
    /// The underlying HttpClient is created by IHttpClientFactory for proper connection pooling.
    /// </remarks>
    public OllamaHttpClient CreateClient()
    {
        return new OllamaHttpClient(this._httpClientFactory, this._configuration);
    }
}
