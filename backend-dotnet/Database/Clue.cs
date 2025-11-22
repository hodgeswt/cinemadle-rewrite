using Microsoft.EntityFrameworkCore;

using System.ComponentModel.DataAnnotations.Schema;

namespace Cinemadle.Database;

[Index(nameof(UserId), nameof(GameId))]
public class Clue
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public string Id { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    public required string UserId { get; set; }

    public required string GameId { get; set; }

    [Column(TypeName = "nvarchar(24)")]
    public required ClueType ClueType { get; set; }

    public DateTime Inserted { get; set; }
}

public enum ClueType
{
    Visual
}