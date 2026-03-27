namespace HRPayroll.Core.Entities;

public class PublicHoliday
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Year { get; set; }
    public string CountryCode { get; set; } = "SG";
    public string Source { get; set; } = "data.gov.sg";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
