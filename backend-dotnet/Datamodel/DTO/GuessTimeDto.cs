namespace Cinemadle.Datamodel.DTO;

public class GuessTimeDto
{
    public required DayOfWeek MeanDay { get; set; }
    public required DayOfWeek MedianDay { get; set; }
    public required DayOfWeek ModeDay { get; set; }

    public required TimeOnly MeanTime { get; set; }
    public required TimeOnly MedianTime { get; set; }
    public required TimeOnly ModeTime { get; set; }
    public required TimeOnly MinTime { get; set; }
    public required TimeOnly MaxTime { get; set; }
}