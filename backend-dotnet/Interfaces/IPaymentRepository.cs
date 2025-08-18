using Cinemadle.Datamodel.DTO;

namespace Cinemadle.Interfaces;

public interface IPaymentRepository
{
    public Task<string?> CreatePayment(PurchaseDetailsDto details, string customerId);
    public bool UpdatePaymentSuccess(string orderId);
    public bool UpdatePaymentFailed(string orderId);
}