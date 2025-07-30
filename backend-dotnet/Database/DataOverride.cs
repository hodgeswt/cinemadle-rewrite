using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Cinemadle.Database;

[Index(nameof(MovieId))]
public class DataOverride
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int MovieId { get; set; }

    public required string Category { get; set; }

    public required string Data { get; set; }
}
