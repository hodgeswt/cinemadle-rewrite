using System.ComponentModel.DataAnnotations.Schema;
using Cinemadle.Datamodel.Domain;
using Microsoft.EntityFrameworkCore;

namespace Cinemadle.Database;

[Index(nameof(CustomerId))]
public class PurchaseDetails
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public required PaymentService PaymentService { get; set; }
    public required string CustomerId { get; set; }
    public required string OrderId { get; set; }
    public required string ProductId { get; set; }
    public required long Quantity { get; set; }
    public required PaymentStatus PaymentStatus { get; set; }
}