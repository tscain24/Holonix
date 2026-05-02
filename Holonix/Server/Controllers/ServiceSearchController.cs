using System.Data;
using Holonix.Server.Infrastructure.Data;
using Holonix.Server.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Holonix.Server.Controllers;

[ApiController]
[Route("api/search")]
public sealed class ServiceSearchController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly OpenAiEmbeddingService _embeddingService;
    private readonly ILogger<ServiceSearchController> _logger;

    public ServiceSearchController(
        ApplicationDbContext dbContext,
        OpenAiEmbeddingService embeddingService,
        ILogger<ServiceSearchController> logger)
    {
        _dbContext = dbContext;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    [HttpGet("services")]
    public async Task<IActionResult> SearchServices(
        [FromQuery] string? query,
        [FromQuery] decimal? lat,
        [FromQuery] decimal? lng,
        [FromQuery] decimal? radiusMiles,
        CancellationToken cancellationToken)
    {
        return await SearchServicesCoreAsync(query, lat, lng, radiusMiles, includeServices: true, includeTopCategories: false, cancellationToken);
    }

    [HttpGet("businesses")]
    public async Task<IActionResult> SearchBusinesses(
        [FromQuery] string? query,
        [FromQuery] decimal? lat,
        [FromQuery] decimal? lng,
        [FromQuery] decimal? radiusMiles,
        [FromQuery] bool includeTopCategories,
        CancellationToken cancellationToken)
    {
        return await SearchServicesCoreAsync(query, lat, lng, radiusMiles, includeServices: false, includeTopCategories, cancellationToken);
    }

    private async Task<IActionResult> SearchServicesCoreAsync(
        string? query,
        decimal? lat,
        decimal? lng,
        decimal? radiusMiles,
        bool includeServices,
        bool includeTopCategories,
        CancellationToken cancellationToken)
    {
        var errors = Validate(query, lat, lng, ref radiusMiles);
        if (errors.Count > 0)
        {
            return BadRequest(new { errors });
        }

        var normalizedQuery = query!.Trim();
        var latitude = lat!.Value;
        var longitude = lng!.Value;
        var radius = radiusMiles!.Value;
        var geographyType = await ResolveGeographyTypeNameAsync(cancellationToken);
        var supportsGeography = geographyType is not null;

        try
        {
            var embeddings = await _embeddingService.CreateEmbeddingsAsync([normalizedQuery], cancellationToken);
            var embedding = embeddings[0];
            if (embedding.Length != 1536)
            {
                _logger.LogWarning("Unexpected embedding dimensions {Dimensions}. Falling back to keyword search.", embedding.Length);
                return await TryKeywordFallbackAsync(normalizedQuery, latitude, longitude, radius, supportsGeography, geographyType, includeServices, cancellationToken);
            }

            var embeddingLiteral = OpenAiEmbeddingService.ToVectorLiteral(embedding);
            if (includeServices)
            {
                var results = await VectorSearchAsync(embeddingLiteral, latitude, longitude, radius, supportsGeography, geographyType, cancellationToken);
                return Ok(results);
            }

            var businessResults = await VectorSearchBusinessesAsync(embeddingLiteral, latitude, longitude, radius, supportsGeography, geographyType, cancellationToken);
            if (!includeTopCategories)
            {
                return Ok(businessResults);
            }

            var topCategories = await VectorTopCategoriesAsync(embeddingLiteral, cancellationToken);
            return Ok(new
            {
                topCategories,
                results = businessResults,
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Semantic search failed. Falling back to keyword search.");
            return await TryKeywordFallbackAsync(normalizedQuery, latitude, longitude, radius, supportsGeography, geographyType, includeServices, cancellationToken);
        }
    }

    private static List<string> Validate(string? query, decimal? lat, decimal? lng, ref decimal? radiusMiles)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(query))
        {
            errors.Add("Query is required.");
        }
        else if (query.Length > 500)
        {
            errors.Add("Query is too long.");
        }

        if (!lat.HasValue || !lng.HasValue)
        {
            errors.Add("Latitude and longitude are required.");
        }
        else
        {
            if (lat.Value is < -90 or > 90)
            {
                errors.Add("Latitude must be between -90 and 90.");
            }

            if (lng.Value is < -180 or > 180)
            {
                errors.Add("Longitude must be between -180 and 180.");
            }
        }

        radiusMiles ??= 25m;
        if (radiusMiles.Value <= 0)
        {
            errors.Add("radiusMiles must be greater than 0.");
        }
        else if (radiusMiles.Value > 100m)
        {
            errors.Add("radiusMiles must be at most 100.");
        }

        return errors;
    }

    private async Task<List<ServiceSearchResult>> VectorSearchAsync(
        string embeddingLiteral,
        decimal latitude,
        decimal longitude,
        decimal radiusMiles,
        bool useGeography,
        string? geographyTypeName,
        CancellationToken cancellationToken)
    {
        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = useGeography
            ? $"""
            WITH top_categories AS (
                SELECT
                    c."CategoryId",
                    c."Name" AS "CategoryName",
                    (c."Embedding" <=> CAST(@p0 AS vector(1536))) AS "CosineDistance",
                    (1 - (c."Embedding" <=> CAST(@p0 AS vector(1536)))) AS "SemanticScore"
                FROM service."Category" c
                WHERE c."InactiveDate" IS NULL AND c."Embedding" IS NOT NULL
                ORDER BY c."Embedding" <=> CAST(@p0 AS vector(1536))
                LIMIT 5
            )
            SELECT
                b."BusinessId",
                b."Name" AS "BusinessName",
                bs."BusinessServiceId" AS "ServiceId",
                bs."Name" AS "ServiceName",
                bs."Description" AS "ServiceDescription",
                tc."CategoryId",
                tc."CategoryName",
                tc."SemanticScore",
                (ST_Distance(
                    ST_SetSRID(ST_MakePoint(ba."Longitude"::double precision, ba."Latitude"::double precision), 4326)::{geographyTypeName},
                    ST_SetSRID(ST_MakePoint(@p1::double precision, @p2::double precision), 4326)::{geographyTypeName}
                ) / 1609.344) AS "DistanceMiles",
                bs."Price"
            FROM top_categories tc
            JOIN business."BusinessService" bs
                ON bs."CategoryId" = tc."CategoryId"
               AND bs."InactiveDate" IS NULL
            JOIN business."Business" b
                ON b."BusinessId" = bs."BusinessId"
               AND b."InactiveDate" IS NULL
            JOIN business."BusinessAddress" ba
                ON ba."BusinessId" = b."BusinessId"
               AND ba."IsPrimary" = TRUE
               AND ba."InactiveDate" IS NULL
               AND ba."Latitude" IS NOT NULL
               AND ba."Longitude" IS NOT NULL
            WHERE ST_DWithin(
                ST_SetSRID(ST_MakePoint(ba."Longitude"::double precision, ba."Latitude"::double precision), 4326)::{geographyTypeName},
                ST_SetSRID(ST_MakePoint(@p1::double precision, @p2::double precision), 4326)::{geographyTypeName},
                @p3
            )
            ORDER BY tc."SemanticScore" DESC, "DistanceMiles" ASC
            LIMIT 100;
            """
            : """
            WITH top_categories AS (
                SELECT
                    c."CategoryId",
                    c."Name" AS "CategoryName",
                    (c."Embedding" <=> CAST(@p0 AS vector(1536))) AS "CosineDistance",
                    (1 - (c."Embedding" <=> CAST(@p0 AS vector(1536)))) AS "SemanticScore"
                FROM service."Category" c
                WHERE c."InactiveDate" IS NULL AND c."Embedding" IS NOT NULL
                ORDER BY c."Embedding" <=> CAST(@p0 AS vector(1536))
                LIMIT 5
            ),
            candidates AS (
                SELECT
                    b."BusinessId",
                    b."Name" AS "BusinessName",
                    bs."BusinessServiceId" AS "ServiceId",
                    bs."Name" AS "ServiceName",
                    bs."Description" AS "ServiceDescription",
                    tc."CategoryId",
                    tc."CategoryName",
                    tc."SemanticScore",
                    (
                        3958.7613 * 2 * ASIN(SQRT(
                            POWER(SIN((RADIANS(@p2) - RADIANS(ba."Latitude")) / 2), 2) +
                            COS(RADIANS(ba."Latitude")) * COS(RADIANS(@p2)) *
                            POWER(SIN((RADIANS(@p1) - RADIANS(ba."Longitude")) / 2), 2)
                        ))
                    ) AS "DistanceMiles",
                    bs."Price"
                FROM top_categories tc
                JOIN business."BusinessService" bs
                    ON bs."CategoryId" = tc."CategoryId"
                   AND bs."InactiveDate" IS NULL
                JOIN business."Business" b
                    ON b."BusinessId" = bs."BusinessId"
                   AND b."InactiveDate" IS NULL
                JOIN business."BusinessAddress" ba
                    ON ba."BusinessId" = b."BusinessId"
                   AND ba."IsPrimary" = TRUE
                   AND ba."InactiveDate" IS NULL
                   AND ba."Latitude" IS NOT NULL
                   AND ba."Longitude" IS NOT NULL
            )
            SELECT *
            FROM candidates
            WHERE "DistanceMiles" <= @p3
            ORDER BY "SemanticScore" DESC, "DistanceMiles" ASC
            LIMIT 100;
            """;

        AddParameter(command, "@p0", embeddingLiteral);
        AddParameter(command, "@p1", longitude);
        AddParameter(command, "@p2", latitude);
        if (useGeography)
        {
            const decimal metersPerMile = 1609.344m;
            AddParameter(command, "@p3", radiusMiles * metersPerMile);
        }
        else
        {
            AddParameter(command, "@p3", radiusMiles);
        }

        var results = new List<ServiceSearchResult>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new ServiceSearchResult(
                businessId: reader.GetInt32(0),
                businessName: reader.GetString(1),
                serviceId: reader.GetInt64(2),
                serviceName: reader.GetString(3),
                serviceDescription: reader.IsDBNull(4) ? null : reader.GetString(4),
                categoryId: reader.GetInt32(5),
                categoryName: reader.GetString(6),
                semanticScore: reader.IsDBNull(7) ? null : reader.GetDouble(7),
                distanceMiles: reader.IsDBNull(8) ? null : reader.GetDouble(8),
                price: reader.IsDBNull(9) ? null : reader.GetDecimal(9)));
        }

        return results;
    }

    private async Task<List<BusinessSearchResult>> VectorSearchBusinessesAsync(
        string embeddingLiteral,
        decimal latitude,
        decimal longitude,
        decimal radiusMiles,
        bool useGeography,
        string? geographyTypeName,
        CancellationToken cancellationToken)
    {
        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = useGeography
            ? $"""
            WITH top_categories AS (
                SELECT
                    c."CategoryId",
                    c."Name" AS "CategoryName",
                    (1 - (c."Embedding" <=> CAST(@p0 AS vector(1536)))) AS "SemanticScore"
                FROM service."Category" c
                WHERE c."InactiveDate" IS NULL AND c."Embedding" IS NOT NULL
                ORDER BY c."Embedding" <=> CAST(@p0 AS vector(1536))
                LIMIT 5
            ),
            candidates AS (
                SELECT
                    b."BusinessId",
                    b."Name" AS "BusinessName",
                    bd."BusinessIconBase64" AS "BusinessIconBase64",
                    ba."Latitude"::double precision AS "Latitude",
                    ba."Longitude"::double precision AS "Longitude",
                    tc."CategoryId",
                    tc."CategoryName",
                    tc."SemanticScore",
                    (ST_Distance(
                        ST_SetSRID(ST_MakePoint(ba."Longitude"::double precision, ba."Latitude"::double precision), 4326)::{geographyTypeName},
                        ST_SetSRID(ST_MakePoint(@p1::double precision, @p2::double precision), 4326)::{geographyTypeName}
                    ) / 1609.344) AS "DistanceMiles"
                FROM top_categories tc
                JOIN business."BusinessService" bs
                    ON bs."CategoryId" = tc."CategoryId"
                   AND bs."InactiveDate" IS NULL
                JOIN business."Business" b
                    ON b."BusinessId" = bs."BusinessId"
                   AND b."InactiveDate" IS NULL
                LEFT JOIN business."BusinessDetails" bd
                    ON bd."BusinessId" = b."BusinessId"
                JOIN business."BusinessAddress" ba
                    ON ba."BusinessId" = b."BusinessId"
                   AND ba."IsPrimary" = TRUE
                   AND ba."InactiveDate" IS NULL
                   AND ba."Latitude" IS NOT NULL
                   AND ba."Longitude" IS NOT NULL
                WHERE ST_DWithin(
                    ST_SetSRID(ST_MakePoint(ba."Longitude"::double precision, ba."Latitude"::double precision), 4326)::{geographyTypeName},
                    ST_SetSRID(ST_MakePoint(@p1::double precision, @p2::double precision), 4326)::{geographyTypeName},
                    @p3
                )
            ),
            ranked AS (
                SELECT
                    c.*,
                    ROW_NUMBER() OVER (PARTITION BY c."BusinessId" ORDER BY c."SemanticScore" DESC, c."DistanceMiles" ASC) AS rn
                FROM candidates c
            )
            SELECT
                r."BusinessId",
                r."BusinessName",
                r."BusinessIconBase64",
                r."Latitude",
                r."Longitude",
                r."CategoryId",
                r."CategoryName",
                r."SemanticScore",
                r."DistanceMiles"
            FROM ranked r
            WHERE r.rn = 1
            ORDER BY r."SemanticScore" DESC, r."DistanceMiles" ASC
            LIMIT 100;
            """
            : """
            WITH top_categories AS (
                SELECT
                    c."CategoryId",
                    c."Name" AS "CategoryName",
                    (1 - (c."Embedding" <=> CAST(@p0 AS vector(1536)))) AS "SemanticScore"
                FROM service."Category" c
                WHERE c."InactiveDate" IS NULL AND c."Embedding" IS NOT NULL
                ORDER BY c."Embedding" <=> CAST(@p0 AS vector(1536))
                LIMIT 5
            ),
            candidates AS (
                SELECT
                    b."BusinessId",
                    b."Name" AS "BusinessName",
                    bd."BusinessIconBase64" AS "BusinessIconBase64",
                    ba."Latitude"::double precision AS "Latitude",
                    ba."Longitude"::double precision AS "Longitude",
                    tc."CategoryId",
                    tc."CategoryName",
                    tc."SemanticScore",
                    (
                        3958.7613 * 2 * ASIN(SQRT(
                            POWER(SIN((RADIANS(@p2) - RADIANS(ba."Latitude")) / 2), 2) +
                            COS(RADIANS(ba."Latitude")) * COS(RADIANS(@p2)) *
                            POWER(SIN((RADIANS(@p1) - RADIANS(ba."Longitude")) / 2), 2)
                        ))
                    ) AS "DistanceMiles"
                FROM top_categories tc
                JOIN business."BusinessService" bs
                    ON bs."CategoryId" = tc."CategoryId"
                   AND bs."InactiveDate" IS NULL
                JOIN business."Business" b
                    ON b."BusinessId" = bs."BusinessId"
                   AND b."InactiveDate" IS NULL
                LEFT JOIN business."BusinessDetails" bd
                    ON bd."BusinessId" = b."BusinessId"
                JOIN business."BusinessAddress" ba
                    ON ba."BusinessId" = b."BusinessId"
                   AND ba."IsPrimary" = TRUE
                   AND ba."InactiveDate" IS NULL
                   AND ba."Latitude" IS NOT NULL
                   AND ba."Longitude" IS NOT NULL
            ),
            filtered AS (
                SELECT * FROM candidates WHERE "DistanceMiles" <= @p3
            ),
            ranked AS (
                SELECT
                    f.*,
                    ROW_NUMBER() OVER (PARTITION BY f."BusinessId" ORDER BY f."SemanticScore" DESC, f."DistanceMiles" ASC) AS rn
                FROM filtered f
            )
            SELECT
                r."BusinessId",
                r."BusinessName",
                r."BusinessIconBase64",
                r."Latitude",
                r."Longitude",
                r."CategoryId",
                r."CategoryName",
                r."SemanticScore",
                r."DistanceMiles"
            FROM ranked r
            WHERE r.rn = 1
            ORDER BY r."SemanticScore" DESC, r."DistanceMiles" ASC
            LIMIT 100;
            """;

        AddParameter(command, "@p0", embeddingLiteral);
        AddParameter(command, "@p1", longitude);
        AddParameter(command, "@p2", latitude);
        if (useGeography)
        {
            const decimal metersPerMile = 1609.344m;
            AddParameter(command, "@p3", radiusMiles * metersPerMile);
        }
        else
        {
            AddParameter(command, "@p3", radiusMiles);
        }

        var results = new List<BusinessSearchResult>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new BusinessSearchResult(
                businessId: reader.GetInt32(0),
                businessName: reader.GetString(1),
                businessIconBase64: reader.IsDBNull(2) ? null : reader.GetString(2),
                latitude: reader.IsDBNull(3) ? null : reader.GetDouble(3),
                longitude: reader.IsDBNull(4) ? null : reader.GetDouble(4),
                categoryId: reader.GetInt32(5),
                categoryName: reader.GetString(6),
                semanticScore: reader.IsDBNull(7) ? null : reader.GetDouble(7),
                distanceMiles: reader.IsDBNull(8) ? null : reader.GetDouble(8)));
        }

        return results;
    }

    private async Task<List<TopCategoryResult>> VectorTopCategoriesAsync(
        string embeddingLiteral,
        CancellationToken cancellationToken)
    {
        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                c."CategoryId",
                c."Name" AS "CategoryName",
                (1 - (c."Embedding" <=> CAST(@p0 AS vector(1536)))) AS "SemanticScore"
            FROM service."Category" c
            WHERE c."InactiveDate" IS NULL AND c."Embedding" IS NOT NULL
            ORDER BY c."Embedding" <=> CAST(@p0 AS vector(1536))
            LIMIT 5;
            """;

        AddParameter(command, "@p0", embeddingLiteral);

        var results = new List<TopCategoryResult>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new TopCategoryResult(
                categoryId: reader.GetInt32(0),
                categoryName: reader.GetString(1),
                semanticScore: reader.IsDBNull(2) ? null : reader.GetDouble(2)));
        }

        return results;
    }

    private async Task<List<ServiceSearchResult>> KeywordFallbackAsync(
        string query,
        decimal latitude,
        decimal longitude,
        decimal radiusMiles,
        bool useGeography,
        string? geographyTypeName,
        CancellationToken cancellationToken)
    {
        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = useGeography
            ? $"""
            SELECT
                b."BusinessId",
                b."Name" AS "BusinessName",
                bs."BusinessServiceId" AS "ServiceId",
                bs."Name" AS "ServiceName",
                bs."Description" AS "ServiceDescription",
                c."CategoryId",
                c."Name" AS "CategoryName",
                0.0 AS "SemanticScore",
                (ST_Distance(
                    ST_SetSRID(ST_MakePoint(ba."Longitude"::double precision, ba."Latitude"::double precision), 4326)::{geographyTypeName},
                    ST_SetSRID(ST_MakePoint(@p1::double precision, @p2::double precision), 4326)::{geographyTypeName}
                ) / 1609.344) AS "DistanceMiles",
                bs."Price"
            FROM business."BusinessService" bs
            JOIN service."Category" c
                ON c."CategoryId" = bs."CategoryId"
               AND c."InactiveDate" IS NULL
            JOIN business."Business" b
                ON b."BusinessId" = bs."BusinessId"
               AND b."InactiveDate" IS NULL
            JOIN business."BusinessAddress" ba
                ON ba."BusinessId" = b."BusinessId"
               AND ba."IsPrimary" = TRUE
               AND ba."InactiveDate" IS NULL
               AND ba."Latitude" IS NOT NULL
               AND ba."Longitude" IS NOT NULL
            WHERE bs."InactiveDate" IS NULL
              AND ST_DWithin(
                    ST_SetSRID(ST_MakePoint(ba."Longitude"::double precision, ba."Latitude"::double precision), 4326)::{geographyTypeName},
                    ST_SetSRID(ST_MakePoint(@p1::double precision, @p2::double precision), 4326)::{geographyTypeName},
                    @p3
                )
              AND (
                    c."Name" ILIKE @p0 OR
                    c."SearchText" ILIKE @p0 OR
                    bs."Name" ILIKE @p0 OR
                    bs."Description" ILIKE @p0
                )
            ORDER BY "DistanceMiles" ASC
            LIMIT 100;
            """
            : """
            WITH candidates AS (
                SELECT
                    b."BusinessId",
                    b."Name" AS "BusinessName",
                    bs."BusinessServiceId" AS "ServiceId",
                    bs."Name" AS "ServiceName",
                    bs."Description" AS "ServiceDescription",
                    c."CategoryId",
                    c."Name" AS "CategoryName",
                    0.0 AS "SemanticScore",
                    (
                        3958.7613 * 2 * ASIN(SQRT(
                            POWER(SIN((RADIANS(@p2) - RADIANS(ba."Latitude")) / 2), 2) +
                            COS(RADIANS(ba."Latitude")) * COS(RADIANS(@p2)) *
                            POWER(SIN((RADIANS(@p1) - RADIANS(ba."Longitude")) / 2), 2)
                        ))
                    ) AS "DistanceMiles",
                    bs."Price"
                FROM business."BusinessService" bs
                JOIN service."Category" c
                    ON c."CategoryId" = bs."CategoryId"
                   AND c."InactiveDate" IS NULL
                JOIN business."Business" b
                    ON b."BusinessId" = bs."BusinessId"
                   AND b."InactiveDate" IS NULL
                JOIN business."BusinessAddress" ba
                    ON ba."BusinessId" = b."BusinessId"
                   AND ba."IsPrimary" = TRUE
                   AND ba."InactiveDate" IS NULL
                   AND ba."Latitude" IS NOT NULL
                   AND ba."Longitude" IS NOT NULL
                WHERE bs."InactiveDate" IS NULL
                  AND (
                        c."Name" ILIKE @p0 OR
                        c."SearchText" ILIKE @p0 OR
                        bs."Name" ILIKE @p0 OR
                        bs."Description" ILIKE @p0
                    )
            )
            SELECT *
            FROM candidates
            WHERE "DistanceMiles" <= @p3
            ORDER BY "DistanceMiles" ASC
            LIMIT 100;
            """;

        AddParameter(command, "@p0", $"%{query}%");
        AddParameter(command, "@p1", longitude);
        AddParameter(command, "@p2", latitude);
        if (useGeography)
        {
            const decimal metersPerMile = 1609.344m;
            AddParameter(command, "@p3", radiusMiles * metersPerMile);
        }
        else
        {
            AddParameter(command, "@p3", radiusMiles);
        }

        var results = new List<ServiceSearchResult>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new ServiceSearchResult(
                businessId: reader.GetInt32(0),
                businessName: reader.GetString(1),
                serviceId: reader.GetInt64(2),
                serviceName: reader.GetString(3),
                serviceDescription: reader.IsDBNull(4) ? null : reader.GetString(4),
                categoryId: reader.GetInt32(5),
                categoryName: reader.GetString(6),
                semanticScore: reader.IsDBNull(7) ? null : reader.GetDouble(7),
                distanceMiles: reader.IsDBNull(8) ? null : reader.GetDouble(8),
                price: reader.IsDBNull(9) ? null : reader.GetDecimal(9)));
        }

        return results;
    }

    private async Task<IActionResult> TryKeywordFallbackAsync(
        string query,
        decimal latitude,
        decimal longitude,
        decimal radiusMiles,
        bool useGeography,
        string? geographyTypeName,
        bool includeServices,
        CancellationToken cancellationToken)
    {
        try
        {
            if (includeServices)
            {
                var fallback = await KeywordFallbackAsync(query, latitude, longitude, radiusMiles, useGeography, geographyTypeName, cancellationToken);
                return Ok(fallback);
            }

            var businessFallback = await KeywordFallbackBusinessesAsync(query, latitude, longitude, radiusMiles, useGeography, geographyTypeName, cancellationToken);
            return Ok(businessFallback);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Keyword fallback search failed.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { errors = new[] { "Search is temporarily unavailable." } });
        }
    }

    private async Task<List<BusinessSearchResult>> KeywordFallbackBusinessesAsync(
        string query,
        decimal latitude,
        decimal longitude,
        decimal radiusMiles,
        bool useGeography,
        string? geographyTypeName,
        CancellationToken cancellationToken)
    {
        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = useGeography
            ? $"""
            WITH candidates AS (
                SELECT
                    b."BusinessId",
                    b."Name" AS "BusinessName",
                    bd."BusinessIconBase64" AS "BusinessIconBase64",
                    ba."Latitude"::double precision AS "Latitude",
                    ba."Longitude"::double precision AS "Longitude",
                    c."CategoryId",
                    c."Name" AS "CategoryName",
                    0.0 AS "SemanticScore",
                    (ST_Distance(
                        ST_SetSRID(ST_MakePoint(ba."Longitude"::double precision, ba."Latitude"::double precision), 4326)::{geographyTypeName},
                        ST_SetSRID(ST_MakePoint(@p1::double precision, @p2::double precision), 4326)::{geographyTypeName}
                    ) / 1609.344) AS "DistanceMiles"
                FROM business."BusinessService" bs
                JOIN service."Category" c
                    ON c."CategoryId" = bs."CategoryId"
                   AND c."InactiveDate" IS NULL
                JOIN business."Business" b
                    ON b."BusinessId" = bs."BusinessId"
                   AND b."InactiveDate" IS NULL
                LEFT JOIN business."BusinessDetails" bd
                    ON bd."BusinessId" = b."BusinessId"
                JOIN business."BusinessAddress" ba
                    ON ba."BusinessId" = b."BusinessId"
                   AND ba."IsPrimary" = TRUE
                   AND ba."InactiveDate" IS NULL
                   AND ba."Latitude" IS NOT NULL
                   AND ba."Longitude" IS NOT NULL
                WHERE bs."InactiveDate" IS NULL
                  AND ST_DWithin(
                        ST_SetSRID(ST_MakePoint(ba."Longitude"::double precision, ba."Latitude"::double precision), 4326)::{geographyTypeName},
                        ST_SetSRID(ST_MakePoint(@p1::double precision, @p2::double precision), 4326)::{geographyTypeName},
                        @p3
                    )
                  AND (
                        c."Name" ILIKE @p0 OR
                        c."SearchText" ILIKE @p0 OR
                        bs."Name" ILIKE @p0 OR
                        bs."Description" ILIKE @p0
                    )
            ),
            ranked AS (
                SELECT
                    c.*,
                    ROW_NUMBER() OVER (PARTITION BY c."BusinessId" ORDER BY c."DistanceMiles" ASC) AS rn
                FROM candidates c
            )
            SELECT
                r."BusinessId",
                r."BusinessName",
                r."BusinessIconBase64",
                r."Latitude",
                r."Longitude",
                r."CategoryId",
                r."CategoryName",
                r."SemanticScore",
                r."DistanceMiles"
            FROM ranked r
            WHERE r.rn = 1
            ORDER BY r."DistanceMiles" ASC
            LIMIT 100;
            """
            : """
            WITH candidates AS (
                SELECT
                    b."BusinessId",
                    b."Name" AS "BusinessName",
                    bd."BusinessIconBase64" AS "BusinessIconBase64",
                    ba."Latitude"::double precision AS "Latitude",
                    ba."Longitude"::double precision AS "Longitude",
                    c."CategoryId",
                    c."Name" AS "CategoryName",
                    0.0 AS "SemanticScore",
                    (
                        3958.7613 * 2 * ASIN(SQRT(
                            POWER(SIN((RADIANS(@p2) - RADIANS(ba."Latitude")) / 2), 2) +
                            COS(RADIANS(ba."Latitude")) * COS(RADIANS(@p2)) *
                            POWER(SIN((RADIANS(@p1) - RADIANS(ba."Longitude")) / 2), 2)
                        ))
                    ) AS "DistanceMiles"
                FROM business."BusinessService" bs
                JOIN service."Category" c
                    ON c."CategoryId" = bs."CategoryId"
                   AND c."InactiveDate" IS NULL
                JOIN business."Business" b
                    ON b."BusinessId" = bs."BusinessId"
                   AND b."InactiveDate" IS NULL
                LEFT JOIN business."BusinessDetails" bd
                    ON bd."BusinessId" = b."BusinessId"
                JOIN business."BusinessAddress" ba
                    ON ba."BusinessId" = b."BusinessId"
                   AND ba."IsPrimary" = TRUE
                   AND ba."InactiveDate" IS NULL
                   AND ba."Latitude" IS NOT NULL
                   AND ba."Longitude" IS NOT NULL
                WHERE bs."InactiveDate" IS NULL
                  AND (
                        c."Name" ILIKE @p0 OR
                        c."SearchText" ILIKE @p0 OR
                        bs."Name" ILIKE @p0 OR
                        bs."Description" ILIKE @p0
                    )
            ),
            filtered AS (
                SELECT * FROM candidates WHERE "DistanceMiles" <= @p3
            ),
            ranked AS (
                SELECT
                    f.*,
                    ROW_NUMBER() OVER (PARTITION BY f."BusinessId" ORDER BY f."DistanceMiles" ASC) AS rn
                FROM filtered f
            )
            SELECT
                r."BusinessId",
                r."BusinessName",
                r."BusinessIconBase64",
                r."Latitude",
                r."Longitude",
                r."CategoryId",
                r."CategoryName",
                r."SemanticScore",
                r."DistanceMiles"
            FROM ranked r
            WHERE r.rn = 1
            ORDER BY r."DistanceMiles" ASC
            LIMIT 100;
            """;

        AddParameter(command, "@p0", $"%{query}%");
        AddParameter(command, "@p1", longitude);
        AddParameter(command, "@p2", latitude);
        if (useGeography)
        {
            const decimal metersPerMile = 1609.344m;
            AddParameter(command, "@p3", radiusMiles * metersPerMile);
        }
        else
        {
            AddParameter(command, "@p3", radiusMiles);
        }

        var results = new List<BusinessSearchResult>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new BusinessSearchResult(
                businessId: reader.GetInt32(0),
                businessName: reader.GetString(1),
                businessIconBase64: reader.IsDBNull(2) ? null : reader.GetString(2),
                latitude: reader.IsDBNull(3) ? null : reader.GetDouble(3),
                longitude: reader.IsDBNull(4) ? null : reader.GetDouble(4),
                categoryId: reader.GetInt32(5),
                categoryName: reader.GetString(6),
                semanticScore: reader.IsDBNull(7) ? null : reader.GetDouble(7),
                distanceMiles: reader.IsDBNull(8) ? null : reader.GetDouble(8)));
        }

        return results;
    }

    private static void AddParameter(IDbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private async Task<string?> ResolveGeographyTypeNameAsync(CancellationToken cancellationToken)
    {
        try
        {
            var connection = _dbContext.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT n.nspname
                FROM pg_type t
                JOIN pg_namespace n ON n.oid = t.typnamespace
                WHERE t.typname = 'geography'
                ORDER BY n.nspname
                LIMIT 1;
                """;

            var schema = (await command.ExecuteScalarAsync(cancellationToken))?.ToString();
            if (string.IsNullOrWhiteSpace(schema))
            {
                return null;
            }

            schema = schema.Trim();
            for (var i = 0; i < schema.Length; i++)
            {
                var c = schema[i];
                var isValid = char.IsLetterOrDigit(c) || c == '_';
                if (!isValid)
                {
                    return null;
                }
            }

            var geographyTypeName = $"{schema}.geography";

            await using var smokeTest = connection.CreateCommand();
            smokeTest.CommandText = $"SELECT ST_SetSRID(ST_MakePoint(0::double precision, 0::double precision), 4326)::{geographyTypeName};";
            _ = await smokeTest.ExecuteScalarAsync(cancellationToken);

            return geographyTypeName;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Unable to resolve PostGIS geography type. Using non-PostGIS distance fallback.");
            return null;
        }
    }

    private sealed record ServiceSearchResult(
        int businessId,
        string businessName,
        long serviceId,
        string serviceName,
        string? serviceDescription,
        int categoryId,
        string categoryName,
        double? semanticScore,
        double? distanceMiles,
        decimal? price);

    private sealed record BusinessSearchResult(
        int businessId,
        string businessName,
        string? businessIconBase64,
        double? latitude,
        double? longitude,
        int categoryId,
        string categoryName,
        double? semanticScore,
        double? distanceMiles);

    private sealed record TopCategoryResult(
        int categoryId,
        string categoryName,
        double? semanticScore);
}
