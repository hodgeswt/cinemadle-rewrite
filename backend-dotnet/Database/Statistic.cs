using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Cinemadle.Database;

[Index(nameof(Key), IsUnique = true)]
public class Statistic
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public string Id { get; set; }
    public required string Key { get; set; }
    public required int Count { get; set; }
    public DateTime LastUpdated { get; set; }
}
