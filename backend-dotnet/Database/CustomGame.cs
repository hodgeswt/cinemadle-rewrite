using Microsoft.EntityFrameworkCore;

namespace Cinemadle.Database;

[Index(nameof(Id), IsUnique = true)]
public class CustomGame
{
    public required string Id { get; set; }
    public int TargetMovieId { get; set; }
    public required string CreatorUserId { get; set; }
    public DateTime Inserted { get; set; }
}
