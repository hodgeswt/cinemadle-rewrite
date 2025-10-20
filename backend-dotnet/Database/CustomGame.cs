using System.ComponentModel.DataAnnotations.Schema;

namespace Cinemadle.Database;

public class CustomGame
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public string Id { get; set; }
    public int TargetMovieId { get; set; }
    public required string CreatorUserId { get; set; }
    public DateTime Inserted { get; set; }
}
