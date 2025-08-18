namespace Cinemadle.Datamodel.DTO;

public class FieldDto
{
    public required string Color { get; set; }
    public required int Direction { get; set; }
    public required IEnumerable<string> Values { get; set; }
    public required Dictionary<string, List<string>> Modifiers { get; set; }
}
