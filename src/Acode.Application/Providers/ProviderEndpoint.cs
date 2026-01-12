namespace Acode.Application.Providers;

using System;
using System.Collections.Generic;

/// <summary>
/// Connection details for a provider endpoint.
/// </summary>
/// <remarks>
/// FR-031 to FR-038 from task-004c spec.
/// Gap #3 from task-004c completion checklist.
/// </remarks>
public sealed record ProviderEndpoint
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderEndpoint"/> class.
    /// </summary>
    /// <param name="baseUrl">The base URL for the provider endpoint.</param>
    /// <param name="connectTimeout">Connection timeout (defaults to 5 seconds).</param>
    /// <param name="requestTimeout">Request timeout (defaults to 300 seconds).</param>
    /// <param name="maxRetries">Maximum number of retries (defaults to 3).</param>
    /// <param name="headers">Optional HTTP headers.</param>
    public ProviderEndpoint(
        Uri baseUrl,
        TimeSpan? connectTimeout = null,
        TimeSpan? requestTimeout = null,
        int? maxRetries = null,
        Dictionary<string, string>? headers = null)
    {
        ArgumentNullException.ThrowIfNull(baseUrl, nameof(baseUrl));

        if (baseUrl.Scheme != Uri.UriSchemeHttp && baseUrl.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException(
                "BaseUrl must be a valid HTTP or HTTPS URL",
                nameof(baseUrl));
        }

        var actualConnectTimeout = connectTimeout ?? TimeSpan.FromSeconds(5);
        var actualRequestTimeout = requestTimeout ?? TimeSpan.FromSeconds(300);
        var actualMaxRetries = maxRetries ?? 3;

        if (actualConnectTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentException(
                "ConnectTimeout must be positive",
                nameof(connectTimeout));
        }

        if (actualRequestTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentException(
                "RequestTimeout must be positive",
                nameof(requestTimeout));
        }

        if (actualMaxRetries < 0)
        {
            throw new ArgumentException(
                "MaxRetries must be >= 0",
                nameof(maxRetries));
        }

        BaseUrl = baseUrl;
        ConnectTimeout = actualConnectTimeout;
        RequestTimeout = actualRequestTimeout;
        MaxRetries = actualMaxRetries;
        Headers = headers;
    }

    /// <summary>
    /// Gets the base URL for the provider endpoint.
    /// </summary>
    public Uri BaseUrl { get; init; }

    /// <summary>
    /// Gets the connection timeout.
    /// </summary>
    public TimeSpan ConnectTimeout { get; init; }

    /// <summary>
    /// Gets the request timeout.
    /// </summary>
    public TimeSpan RequestTimeout { get; init; }

    /// <summary>
    /// Gets the maximum number of retries.
    /// </summary>
    public int MaxRetries { get; init; }

    /// <summary>
    /// Gets optional HTTP headers.
    /// </summary>
    public Dictionary<string, string>? Headers { get; init; }
}
