namespace Acode.Infrastructure.Tests.Ollama.Http;

/// <summary>
/// Test HTTP message handler for capturing requests.
/// </summary>
internal sealed class TestHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage _response;

    public TestHttpMessageHandler(HttpResponseMessage response)
    {
        this._response = response;
    }

    public Uri? LastRequestUri { get; private set; }

    public string? LastRequestContent { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        this.LastRequestUri = request.RequestUri;
        if (request.Content is not null)
        {
            this.LastRequestContent = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }

        return this._response;
    }
}
