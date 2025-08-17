namespace Cinemadle.Datamodel.DTO;

public class UserDataDto
{
    public required string Email { get; set; }
    public required long GamesPlayed { get; set; }
    public required long TotalGuesses { get; set; }
    public required IEnumerable<int> DistinctGuesses { get; set; }
}