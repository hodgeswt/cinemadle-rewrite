using Microsoft.EntityFrameworkCore;

using System.ComponentModel.DataAnnotations.Schema;

namespace Cinemadle.Database;

[Index(nameof(UserId), nameof(GameId))]
public class UserGuess
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public string Id { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    public required string UserId { get; set; }

    public required string GameId { get; set; }

    public int SequenceId { get; set; }

    public required int GuessMediaId { get; set; }

    public DateTime Inserted { get; set; }
}
