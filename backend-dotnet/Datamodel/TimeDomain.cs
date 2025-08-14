namespace Cinemadle.Datamodel;

public class TimeDomain
{
    public required DateTime DateTime { get; set; }
    public required DayOfWeek DayOfWeek { get; set; }
    public required TimeOnly TimeOnly { get; set; }
    public required int Count { get; set; }
}