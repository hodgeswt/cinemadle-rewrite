using System.ComponentModel.DataAnnotations.Schema;
using Cinemadle.Datamodel.Domain;
using Microsoft.EntityFrameworkCore;

namespace Cinemadle.Database;

[Index(nameof(UserId), IsUnique = true)]
public class UserAccount
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public required string UserId { get; set; }
    public required List<AddOnRecord> AddOns { get; set; } = [];
}