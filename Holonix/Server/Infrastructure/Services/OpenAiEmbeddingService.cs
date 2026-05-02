using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Holonix.Server.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Holonix.Server.Infrastructure.Services;

public sealed class OpenAiEmbeddingService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly OpenAiOptions _options;
    private readonly ILogger<OpenAiEmbeddingService> _logger;

    public OpenAiEmbeddingService(HttpClient httpClient, IOptions<OpenAiOptions> options, ILogger<OpenAiEmbeddingService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<float[]>> CreateEmbeddingsAsync(IReadOnlyList<string> inputs, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("OpenAI API key is missing. Set OpenAI:ApiKey via user secrets or environment variables.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/embeddings");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        var payload = new CreateEmbeddingsRequest(
            model: "text-embedding-3-small",
            input: inputs,
            encoding_format: "float");

        request.Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("OpenAI embeddings call failed ({StatusCode}). Body: {Body}", (int)response.StatusCode, body);
            throw new OpenAiEmbeddingException((int)response.StatusCode, body);
        }

        var parsed = JsonSerializer.Deserialize<CreateEmbeddingsResponse>(body, JsonOptions);
        if (parsed?.Data is null || parsed.Data.Count == 0)
        {
            throw new InvalidOperationException("OpenAI embeddings response was empty.");
        }

        var vectors = parsed.Data
            .OrderBy(item => item.Index)
            .Select(item => item.Embedding?.ToArray() ?? Array.Empty<float>())
            .ToList();

        if (vectors.Count != inputs.Count)
        {
            throw new InvalidOperationException($"OpenAI embeddings response count mismatch. Expected {inputs.Count}, got {vectors.Count}.");
        }

        return vectors;
    }

    public static string ToVectorLiteral(float[] embedding)
    {
        var parts = embedding.Select(value => value.ToString("R", CultureInfo.InvariantCulture));
        return $"[{string.Join(",", parts)}]";
    }

    private sealed record CreateEmbeddingsRequest(string model, IReadOnlyList<string> input, string encoding_format);

    private sealed record CreateEmbeddingsResponse(List<EmbeddingItem> Data);

    private sealed record EmbeddingItem(int Index, List<float>? Embedding);
}

public sealed class OpenAiEmbeddingException : Exception
{
    public int StatusCode { get; }
    public string ResponseBody { get; }

    public OpenAiEmbeddingException(int statusCode, string responseBody)
        : base($"OpenAI embeddings call failed with status {statusCode}.")
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}
