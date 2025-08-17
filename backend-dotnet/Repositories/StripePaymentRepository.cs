using Cinemadle.Interfaces;
using Stripe;
using Cinemadle.Datamodel.Domain;
using Cinemadle.Datamodel.DTO;
using Cinemadle.Database;

namespace Cinemadle.Repositories;

public class StripePaymentRepository : IPaymentRepository
{
    private readonly ILogger<StripePaymentRepository> _logger;
    private readonly CinemadleConfig _config;
    private readonly StripeClient _stripeClient;
    private readonly DatabaseContext _db;

    public StripePaymentRepository(
        ILogger<StripePaymentRepository> logger,
        IConfigRepository config,
        DatabaseContext db
    )
    {
        _logger = logger;
        _logger.LogDebug("+StripePaymentRepository.ctor");

        _config = config.GetConfig();
        _db = db;

        if (string.IsNullOrWhiteSpace(_config.PaymentsApiKey))
        {
            _logger.LogDebug("-StripePaymentRepository.ctor");
            throw new InvalidDataException("Unable to find payment API key");
        }

        _stripeClient = new(_config.PaymentsApiKey);

        _logger.LogDebug("-StripePaymentRepository.ctor");

    }
    public async Task<string?> CreatePayment(PurchaseDetailsDto details, string customerId)
    {
        _logger.LogDebug("+StripePaymentRepository.CreatePayment()");
        try
        {
            IEnumerable<string> priceIds = _stripeClient.V1.Prices.List().Select(x => x.Id);
            if (!priceIds.Contains(details.ProductId))
            {
                _logger.LogDebug("StripePaymentRepository.CreatePayment(): invalid price id {id} received", details.ProductId);
                _logger.LogDebug("-StripePaymentRepository.CreatePayment()");
                return null;
            }

            string orderId = Guid.NewGuid().ToString();
            var session = _stripeClient.V1.Checkout.Sessions.Create(new()
            {
                LineItems = [
                    new() {
                        Price = details.ProductId,
                        Quantity = details.Quantity,
                    }
                ],
                Mode = "payment",
                ClientReferenceId = orderId,
                SuccessUrl = _config.PaymentSuccessUrl,
                CancelUrl = _config.PaymentFailureUrl,
            });

            PurchaseDetails purchaseDetails = new()
            {
                CustomerId = customerId,
                ProductId = details.ProductId,
                OrderId = orderId,
                PaymentService = PaymentService.Stripe,
                PaymentStatus = PaymentStatus.Pending,
                Quantity = details.Quantity,
            };

            _logger.LogDebug("CreatePayment(): Creating purchase of {productId} for quantity {quantity}, ID: {orderId}", details.ProductId, details.Quantity, orderId);

            try
            {
                _db.Purchases.Add(purchaseDetails);
                await _db.SaveChangesAsync();
            }
            catch
            {
                _logger.LogDebug("StripePaymentRepository.CreatePayment(): failed to record to database");
                _stripeClient.V1.Checkout.Sessions.Expire(session.Id);
            }

            _logger.LogDebug("-StripePaymentRepository.CreatePayment()");
            return session.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError("CreatePayment Exception. Message: {message}, StackTrace: {stackTrace}, InnerException: {innerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message);
            _logger.LogDebug("-StripePaymentRepository.CreatePayment()");

            return null;
        }
    }

    public bool UpdatePaymentFailed(string orderId)
    {
        PurchaseDetails? data = _db.Purchases.Where(x => x.OrderId == orderId).FirstOrDefault();

        if (data is null)
        {
            return false;
        }

        data.PaymentStatus = PaymentStatus.Declined;
        return true;
    }

    public bool UpdatePaymentSuccess(string orderId)
    {
        PurchaseDetails? data = _db.Purchases.Where(x => x.OrderId == orderId).FirstOrDefault();

        if (data is null)
        {
            return false;
        }

        data.PaymentStatus = PaymentStatus.Approved;
        return true;
    }
}