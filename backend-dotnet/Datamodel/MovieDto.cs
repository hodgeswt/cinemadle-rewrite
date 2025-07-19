namespace Cinemadle.Datamodel;

public class MovieDto
{
    public required int Id { get; set; }
    public required string Title { get; set; }
    public required IEnumerable<string> Genres { get; set; }
    public required IEnumerable<PersonDto> Cast { get; set; }
    public required IEnumerable<PersonDto> Creatives { get; set; }
    public required long BoxOffice { get; set; }
    public required string Year { get; set; }
    public required Rating Rating { get; set; }
}
