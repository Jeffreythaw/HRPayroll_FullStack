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
using Microsoft.VisualBasic.FileIO;

namespace HRPayroll.Infrastructure.Services;

public class PublicHolidayService : IPublicHolidayService
{
    private const string ConsolidatedDatasetId = "d_8ef23381f9417e4d4254ee8b4dcdb176";
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
        var records = await DownloadConsolidatedHolidayRecordsAsync(client);
        if (records.Count == 0)
            throw new InvalidOperationException("No public holiday records were returned by data.gov.sg.");

        var now = DateTime.UtcNow;
        foreach (var item in records)
        {
            if (!DateOnly.TryParseExact(item.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                continue;
            if (date.Year != year)
                continue;

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

    private async Task<List<DataGovHolidayRecord>> DownloadConsolidatedHolidayRecordsAsync(HttpClient client)
    {
        try
        {
            var initiate = await client.GetFromJsonAsync<DownloadInitiateResponse>(
                $"https://api-open.data.gov.sg/v1/public/api/datasets/{ConsolidatedDatasetId}/initiate-download",
                JsonOptions());

            var downloadUrl = initiate?.Data?.Url;
            if (string.IsNullOrWhiteSpace(downloadUrl))
            {
                throw new InvalidOperationException("Unable to initiate public holiday download.");
            }

            var csv = await client.GetStringAsync(downloadUrl);
            return ParseHolidayCsv(csv);
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException("Singapore public holiday sync is temporarily unavailable. Please try again in a minute.", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new InvalidOperationException("Singapore public holiday sync timed out. Please try again in a minute.", ex);
        }
    }

    private static JsonSerializerOptions JsonOptions() => new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static List<DataGovHolidayRecord> ParseHolidayCsv(string csv)
    {
        var records = new List<DataGovHolidayRecord>();
        using var stringReader = new StringReader(csv);
        using var parser = new TextFieldParser(stringReader)
        {
            TextFieldType = FieldType.Delimited,
            HasFieldsEnclosedInQuotes = true,
            TrimWhiteSpace = true
        };
        parser.SetDelimiters(",");

        var headers = parser.ReadFields();
        if (headers == null || headers.Length == 0)
        {
            return records;
        }

        var dateIndex = Array.FindIndex(headers, h => h.Equals("date", StringComparison.OrdinalIgnoreCase));
        var dayIndex = Array.FindIndex(headers, h => h.Equals("day", StringComparison.OrdinalIgnoreCase));
        var holidayIndex = Array.FindIndex(headers, h => h.Equals("holiday", StringComparison.OrdinalIgnoreCase));

        while (!parser.EndOfData)
        {
            var fields = parser.ReadFields();
            if (fields == null)
            {
                continue;
            }

            records.Add(new DataGovHolidayRecord
            {
                Date = dateIndex >= 0 && dateIndex < fields.Length ? fields[dateIndex] : string.Empty,
                Day = dayIndex >= 0 && dayIndex < fields.Length ? fields[dayIndex] : string.Empty,
                Holiday = holidayIndex >= 0 && holidayIndex < fields.Length ? fields[holidayIndex] : string.Empty,
            });
        }

        return records;
    }

    private sealed class DownloadInitiateResponse
    {
        [JsonPropertyName("data")]
        public DownloadInitiateEnvelope? Data { get; set; }
    }

    private sealed class DownloadInitiateEnvelope
    {
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
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
