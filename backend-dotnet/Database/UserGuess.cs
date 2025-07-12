using Microsoft.EntityFrameworkCore;

using System.ComponentModel.DataAnnotations.Schema;

namespace Cinemadle.Database;

[Index(nameof(UserId), nameof(GameId))]
public class UserGuess
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public string Id { get; set; }

    public required string UserId { get; set; }

    public required string GameId { get; set; }

    public int SequenceId { get; set; }

    public required int GuessMediaId { get; set; }

    public DateTime Inserted { get; set; }
}
