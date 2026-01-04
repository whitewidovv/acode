namespace Acode.Infrastructure.Tests.Ollama.Health;

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// HTTP handler that delays before responding.
/// </summary>
internal sealed class DelayingHttpMessageHandler : HttpMessageHandler
{
    private readonly TimeSpan _delay;

    public DelayingHttpMessageHandler(TimeSpan delay)
    {
        this._delay = delay;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await Task.Delay(this._delay, cancellationToken).ConfigureAwait(false);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"models\": []}"),
        };
    }
}
