using Microsoft.EntityFrameworkCore;

using System.ComponentModel.DataAnnotations.Schema;

namespace Cinemadle.Database;

[Index(nameof(UserId), nameof(GameId))]
public class Clue
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public string Id { get; set; }

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