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
    public UserAccount UserAccount { get; set; }
    public AddOn AddOn { get; set; }
    public long Count { get; set; }
}