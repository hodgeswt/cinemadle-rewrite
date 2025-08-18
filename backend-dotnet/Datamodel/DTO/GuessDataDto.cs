namespace Cinemadle.Datamodel.DTO;

public class GuessDataDto
{
    public required int GuessMediaId { get; set; }
    public MovieDto? Movie { get; set; }
    public required long GuessCount { get; set; }
}