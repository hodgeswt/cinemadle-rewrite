using System.ComponentModel.DataAnnotations.Schema;
using Cinemadle.Datamodel.Domain;
using Microsoft.EntityFrameworkCore;

namespace Cinemadle.Database;

[Index(nameof(UserAccountId), nameof(AddOn), IsUnique = true)]
public class AddOnRecord
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int UserAccountId { get; set; }

    [ForeignKey(nameof(UserAccountId))]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public UserAccount UserAccount { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public AddOn AddOn { get; set; }
    public long Count { get; set; }
}