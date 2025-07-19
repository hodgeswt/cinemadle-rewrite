using Microsoft.EntityFrameworkCore;

using System.ComponentModel.DataAnnotations.Schema;

namespace Cinemadle.Database;

[Index(nameof(GameId))]
public class TargetMovie
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public string Id { get; set; }

    public required string GameId { get; set; }

    public required int TargetMovieId { get; set; }

    public DateTime Inserted { get; set; }
}
