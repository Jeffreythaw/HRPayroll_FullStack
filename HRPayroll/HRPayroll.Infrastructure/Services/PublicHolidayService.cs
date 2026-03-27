using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HRPayroll.Core.DTOs;
using HRPayroll.Core.Entities;
using HRPayroll.Core.Interfaces;
using HRPayroll.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http;

namespace HRPayroll.Infrastructure.Services;

public class PublicHolidayService : IPublicHolidayService
{
    private const string CollectionMetadataUrl = "https://api-production.data.gov.sg/v2/public/api/collections/691/metadata";
    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;

    public PublicHolidayService(AppDbContext db, IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<List<PublicHolidayDto>> GetByYearAsync(int year)
    {
        await EnsureYearAsync(year);
        return await _db.PublicHolidays
            .Where(x => x.Year == year)
            .OrderBy(x => x.Date)
            .Select(x => new PublicHolidayDto
            {
                Id = x.Id,
                Date = x.Date,
                Name = x.Name,
                Year = x.Year,
                CountryCode = x.CountryCode,
                Source = x.Source
            })
            .ToListAsync();
    }

    public async Task<List<PublicHolidayDto>> SyncYearAsync(int year)
    {
        var client = _httpClientFactory.CreateClient();
        var datasetId = await ResolveDatasetIdAsync(client, year);
        if (string.IsNullOrWhiteSpace(datasetId))
        {
            throw new InvalidOperationException($"Unable to find Singapore public holiday dataset for {year}.");
        }

        var url = $"https://data.gov.sg/api/action/datastore_search?resource_id={datasetId}&limit=1000";
        var payload = await client.GetFromJsonAsync<DataGovDatastoreResponse>(url, JsonOptions());
        var records = payload?.Result?.Records ?? new List<DataGovHolidayRecord>();
        if (records.Count == 0)
        {
            throw new InvalidOperationException($"No public holiday records were returned for {year}.");
        }

        var now = DateTime.UtcNow;
        foreach (var item in records)
        {
            var date = DateOnly.Parse(item.Date, CultureInfo.InvariantCulture);
            var existing = await _db.PublicHolidays.FirstOrDefaultAsync(x => x.Date == date);
            if (existing == null)
            {
                existing = new PublicHoliday
                {
                    Date = date,
                    Name = item.Holiday.Trim(),
                    Year = date.Year,
                    CountryCode = "SG",
                    Source = "data.gov.sg",
                    CreatedAt = now,
                    UpdatedAt = now
                };
                _db.PublicHolidays.Add(existing);
            }
            else
            {
                existing.Name = item.Holiday.Trim();
                existing.Year = date.Year;
                existing.CountryCode = "SG";
                existing.Source = "data.gov.sg";
                existing.UpdatedAt = now;
            }
        }

        await _db.SaveChangesAsync();

        return await GetByYearAsync(year);
    }

    public async Task EnsureYearAsync(int year)
    {
        var hasData = await _db.PublicHolidays.AnyAsync(x => x.Year == year);
        if (!hasData)
        {
            await SyncYearAsync(year);
        }
    }

    public async Task<HashSet<DateOnly>> GetHolidayDatesAsync(int year)
    {
        await EnsureYearAsync(year);
        var dates = await _db.PublicHolidays
            .Where(x => x.Year == year)
            .Select(x => x.Date)
            .ToListAsync();
        return dates.ToHashSet();
    }

    public async Task<bool> IsHolidayAsync(DateOnly date)
    {
        await EnsureYearAsync(date.Year);
        return await _db.PublicHolidays.AnyAsync(x => x.Date == date);
    }

    private async Task<string?> ResolveDatasetIdAsync(HttpClient client, int year)
    {
        var collection = await client.GetFromJsonAsync<CollectionMetadataResponse>(CollectionMetadataUrl, JsonOptions());
        var datasetIds = collection?.Data?.CollectionMetadata?.ChildDatasets ?? new List<string>();
        if (datasetIds.Count == 0)
        {
            return null;
        }

        var metadataTasks = datasetIds.Select(async datasetId =>
        {
            var meta = await client.GetFromJsonAsync<DatasetMetadataResponse>(
                $"https://api-production.data.gov.sg/v2/public/api/datasets/{datasetId}/metadata",
                JsonOptions());
            return meta?.Data;
        });

        var allMetadata = await Task.WhenAll(metadataTasks);
        var match = allMetadata
            .Where(x => x != null)
            .Select(x => x!)
            .FirstOrDefault(x => x.CoverageStart?.Year == year || x.Name.Contains(year.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase));

        return match?.DatasetId;
    }

    private static JsonSerializerOptions JsonOptions() => new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed class CollectionMetadataResponse
    {
        [JsonPropertyName("data")]
        public CollectionMetadataEnvelope? Data { get; set; }
    }

    private sealed class CollectionMetadataEnvelope
    {
        [JsonPropertyName("collectionMetadata")]
        public CollectionMetadata? CollectionMetadata { get; set; }
    }

    private sealed class CollectionMetadata
    {
        [JsonPropertyName("childDatasets")]
        public List<string> ChildDatasets { get; set; } = new();
    }

    private sealed class DatasetMetadataResponse
    {
        [JsonPropertyName("data")]
        public DatasetMetadata? Data { get; set; }
    }

    private sealed class DatasetMetadata
    {
        [JsonPropertyName("datasetId")]
        public string DatasetId { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("coverageStart")]
        public DateTime? CoverageStart { get; set; }
    }

    private sealed class DataGovDatastoreResponse
    {
        [JsonPropertyName("result")]
        public DataGovDatastoreResult? Result { get; set; }
    }

    private sealed class DataGovDatastoreResult
    {
        [JsonPropertyName("records")]
        public List<DataGovHolidayRecord> Records { get; set; } = new();
    }

    private sealed class DataGovHolidayRecord
    {
        [JsonPropertyName("date")]
        public string Date { get; set; } = string.Empty;

        [JsonPropertyName("day")]
        public string Day { get; set; } = string.Empty;

        [JsonPropertyName("holiday")]
        public string Holiday { get; set; } = string.Empty;
    }
}
