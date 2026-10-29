namespace Cinemadle.Datamodel.DTO;

public class GuessDto
{
    public required Dictionary<string, FieldDto> Fields { get; set; }
    public required string Title { get; set; }
}
