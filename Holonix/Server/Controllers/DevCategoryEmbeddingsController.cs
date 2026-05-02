using System.Data;
using Holonix.Server.Infrastructure.Configuration;
using Holonix.Server.Infrastructure.Data;
using Holonix.Server.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Holonix.Server.Controllers;

[ApiController]
[Route("api/dev/category-embeddings")]
public sealed class DevCategoryEmbeddingsController : ControllerBase
{
    private const string DevAdminHeaderName = "X-Dev-Admin-Key";

    private readonly ApplicationDbContext _dbContext;
    private readonly OpenAiEmbeddingService _embeddingService;
    private readonly DevAdminOptions _devAdminOptions;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<DevCategoryEmbeddingsController> _logger;

    public DevCategoryEmbeddingsController(
        ApplicationDbContext dbContext,
        OpenAiEmbeddingService embeddingService,
        IOptions<DevAdminOptions> devAdminOptions,
        IHostEnvironment environment,
        ILogger<DevCategoryEmbeddingsController> logger)
    {
        _dbContext = dbContext;
        _embeddingService = embeddingService;
        _devAdminOptions = devAdminOptions.Value;
        _environment = environment;
        _logger = logger;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> Generate(CancellationToken cancellationToken)
    {
        if (!IsDevAdminRequest())
        {
            return Forbid();
        }

        var candidates = await LoadCandidatesAsync(cancellationToken);
        var totalFound = candidates.Count;
        var generatedCount = 0;
        var skippedCount = 0;
        var failedCount = 0;

        _logger.LogInformation("Category embedding generation requested. Found {TotalFound} candidate categories.", totalFound);

        const int batchSize = 50;
        for (var index = 0; index < candidates.Count; index += batchSize)
        {
            var batch = candidates
                .Skip(index)
                .Take(batchSize)
                .ToList();

            var valid = batch
                .Where(item => !string.IsNullOrWhiteSpace(item.SearchText))
                .ToList();

            skippedCount += batch.Count - valid.Count;

            if (valid.Count == 0)
            {
                continue;
            }

            IReadOnlyList<float[]> embeddings;
            try
            {
                embeddings = await _embeddingService.CreateEmbeddingsAsync(valid.Select(item => item.SearchText!).ToList(), cancellationToken);
            }
            catch (OpenAiEmbeddingException ex) when (ex.StatusCode is 401 or 429)
            {
                failedCount += valid.Count;
                var remaining = candidates.Count - (index + batch.Count);
                if (remaining > 0)
                {
                    failedCount += remaining;
                }

                _logger.LogError(ex, "Failed to create embeddings (HTTP {StatusCode}). Aborting early; {Remaining} categories remain without embeddings.", ex.StatusCode, remaining);
                return Ok(new
                {
                    totalFound,
                    generatedCount,
                    skippedCount,
                    failedCount,
                });
            }
            catch (Exception ex)
            {
                failedCount += valid.Count;
                _logger.LogError(ex, "Failed to create embeddings for a batch of {BatchCount} categories.", valid.Count);
                continue;
            }

            for (var i = 0; i < valid.Count; i++)
            {
                var categoryId = valid[i].CategoryId;
                var embedding = embeddings[i];

                if (embedding.Length == 0)
                {
                    failedCount++;
                    _logger.LogWarning("OpenAI returned an empty embedding for CategoryId {CategoryId}.", categoryId);
                    continue;
                }

                var vectorLiteral = OpenAiEmbeddingService.ToVectorLiteral(embedding);

                try
                {
                    var updated = await _dbContext.Database.ExecuteSqlRawAsync(
                        "UPDATE service.\"Category\" SET \"Embedding\" = CAST(@p0 AS vector(1536)) WHERE \"CategoryId\" = @p1 AND \"Embedding\" IS NULL;",
                        new object[] { vectorLiteral, categoryId },
                        cancellationToken);

                    if (updated > 0)
                    {
                        generatedCount++;
                    }
                    else
                    {
                        skippedCount++;
                    }
                }
                catch (Exception ex)
                {
                    failedCount++;
                    _logger.LogError(ex, "Failed to save embedding for CategoryId {CategoryId}.", categoryId);
                }
            }
        }

        return Ok(new
        {
            totalFound,
            generatedCount,
            skippedCount,
            failedCount,
        });
    }

    private bool IsDevAdminRequest()
    {
        var configuredKey = _devAdminOptions.ApiKey?.Trim();
        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            return _environment.IsDevelopment();
        }

        if (!Request.Headers.TryGetValue(DevAdminHeaderName, out var providedValues))
        {
            return false;
        }

        var providedKey = providedValues.ToString().Trim();
        return !string.IsNullOrWhiteSpace(providedKey)
               && string.Equals(providedKey, configuredKey, StringComparison.Ordinal);
    }

    private async Task<List<CategoryEmbeddingCandidate>> LoadCandidatesAsync(CancellationToken cancellationToken)
    {
        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT "CategoryId", "SearchText"
            FROM service."Category"
            WHERE "InactiveDate" IS NULL
              AND "SearchText" IS NOT NULL
              AND "Embedding" IS NULL
            ORDER BY "CategoryId";
            """;

        var results = new List<CategoryEmbeddingCandidate>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new CategoryEmbeddingCandidate(
                reader.GetInt32(0),
                reader.IsDBNull(1) ? null : reader.GetString(1)));
        }

        return results;
    }

    private sealed record CategoryEmbeddingCandidate(int CategoryId, string? SearchText);
}
