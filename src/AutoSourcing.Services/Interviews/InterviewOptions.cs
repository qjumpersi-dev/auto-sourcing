namespace AutoSourcing.Services.Interviews;

public class InterviewOptions
{
    public const string SectionName = "Interview";

    // Time zone the organiser's working hours are expressed in.
    public string TimeZone { get; set; } = "Pacific/Auckland";

    public int DurationMinutes { get; set; } = 30;

    // How many options we offer the candidate.
    public int SlotCount { get; set; } = 3;

    // Minimum notice before an interview can be booked. Lower this for testing (e.g. 0.5).
    public double EarliestBookingHours { get; set; } = 48;

    // How far ahead candidates can book.
    public int LatestBookingDays { get; set; } = 7;

    public int WorkingHourStart { get; set; } = 9;
    public int WorkingHourEnd { get; set; } = 17;
}
