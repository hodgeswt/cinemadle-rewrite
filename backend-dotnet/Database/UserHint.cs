using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cinemadle.Database;

[Index(nameof(UserId), nameof(GameId))]
public class UserHint
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public string Id { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    public required string UserId { get; set; }

    public required string GameId { get; set; }

    /// <summary>
    /// JSON-serialized hints data
    /// </summary>
    public required string HintsJson { get; set; }

    public DateTime LastUpdated { get; set; }
}
