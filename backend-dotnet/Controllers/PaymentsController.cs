using Cinemadle.Datamodel.DTO;
using Microsoft.AspNetCore.Mvc;
using Cinemadle.Database;
using Cinemadle.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Stripe;
using Stripe.Checkout;
using Cinemadle.Datamodel.Domain;
using Microsoft.EntityFrameworkCore;

namespace Cinemadle.Controllers;

[Route("api/payments")]
[ApiController]
public class PaymentsController : CinemadleControllerBase
{
    private readonly ILogger<PaymentsController> _logger;
    private readonly DatabaseContext _db;
    private readonly IPaymentRepository _paymentRepo;
    private readonly CinemadleConfig _config;
    private readonly IFeatureFlagRepository _flagRepo;
    private bool? _paymentsEnabled;
    public PaymentsController(
        ILogger<PaymentsController> logger,
        DatabaseContext db,
        IPaymentRepository paymentRepo,
        IConfigRepository config,
        IFeatureFlagRepository flagRepo
    )
    {
        _logger = logger;
        _logger.LogDebug("+PaymentsController.ctor");

        _db = db;
        _paymentRepo = paymentRepo;
        _config = config.GetConfig();
        _flagRepo = flagRepo;

        _logger.LogDebug("-PaymentsController.ctor");
    }

    private async Task<bool> PaymentsEnabled()
    {
        if (_paymentsEnabled is not null)
        {
            return (bool)_paymentsEnabled;
        }

        _paymentsEnabled = await _flagRepo.Get(nameof(FeatureFlags.PaymentsEnabled));

        return (bool)_paymentsEnabled;
    }

    [HttpGet("quantities")]
    [Authorize]
    public async Task<ActionResult> GetQuantities()
    {
        _logger.LogDebug("+GetQuantities()");
        if (!await PaymentsEnabled())
        {
            _logger.LogDebug("-GetQuantities()");
            return new NotFoundResult();
        }

        string? userId = GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogError("GetQuantities(): Unauthorized access");
            _logger.LogError("-GetQuantities()");
            return new UnauthorizedResult();
        }

        try
        {
            UserAccount? user = _db.UserAccounts.Include(x => x.AddOns).FirstOrDefault(x => x.UserId == userId);
            if (user is null)
            {
                _logger.LogError("-GetQuantities()");
                return new NotFoundResult();
            }

            Dictionary<AddOn, long> quantities = [];

            _logger.LogDebug("GetQuantities: Found {count} add ons", user.AddOns.Count);

            foreach (AddOnRecord record in user.AddOns)
            {
                _logger.LogDebug("GetQuantities: adding {addOn} with quantity {quantity}", record.AddOn, record.Count);
                quantities[record.AddOn] = record.Count;
            }

            _logger.LogError("-GetQuantities()");
            return new OkObjectResult(new QuantitiesDto
            {
                Quantities = quantities.OrderByDescending(x => x.Value).ToDictionary()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("GetQuantities Exception. Message: {message}, StackTrace: {stackTrace}, InnerException: {innerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message);
            _logger.LogDebug("-GetQuantities()");
            return new StatusCodeResult(500);
        }
    }

    [HttpPost("purchase")]
    [Authorize]
    public async Task<ActionResult> MakePurchase([FromBody] PurchaseDetailsDto purchase)
    {
        if (!await PaymentsEnabled())
        {
            _logger.LogDebug("-MakePurchase()");
            return new NotFoundResult();
        }
        _logger.LogDebug("+MakePurchase({purchase})", purchase);
        string? userId = GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogError("MakePurchase(): Unauthorized access");
            return new UnauthorizedResult();
        }

        string? sessionId = await _paymentRepo.CreatePayment(purchase, userId);

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            _logger.LogError("MakePurchase(): invalid session id");
            return new StatusCodeResult(500);
        }

        _logger.LogDebug("-MakePurchase({purchase})", purchase);

        return new OkObjectResult(new PurchaseResponseDto() { SessionId = sessionId });
    }

    private async Task<bool> TryProcessLineItem(string userId, string productId, long quantity)
    {
        if (!await PaymentsEnabled())
        {
            _logger.LogDebug("-TryProcessLineItem()");
            return false;
        }
        UserAccount? acct = _db.UserAccounts.Include(x => x.AddOns).Where(x => x.UserId == userId).FirstOrDefault();

        bool addAccount = acct is null;

        acct ??= new()
            {
                UserId = userId,
                AddOns = []
            };

        if (!_config.AddOnMapping.TryGetValue(productId, out AddOnDetails? addOn))
        {
            return false;
        }

        Dictionary<AddOn, long> additions = [];
        AddOn key = addOn.Name;
        if (!additions.ContainsKey(key))
        {
            additions[key] = 0;
        }

        additions[key] += addOn.Quantity * quantity;

        foreach (KeyValuePair<AddOn, long> kvp in additions)
        {
            AddOnRecord? record = acct.AddOns.Where(x => x.AddOn == kvp.Key).FirstOrDefault();
            if (record == null)
            {
                acct.AddOns.Add(new AddOnRecord()
                {
                    AddOn = kvp.Key,
                    Count = kvp.Value,
                });
                continue;
            }

            record.Count += kvp.Value;
        }

        if (addAccount)
        {
            _db.UserAccounts.Add(acct);
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("TryProcessLineItems Exception. Message: {message}, StackTrace: {stackTrace}, InnerException: {innerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message);
            return false;
        }

        return true;
    }

    [HttpPost("webhook")]
    public async Task<ActionResult> Webhook()
    {
        _logger.LogDebug("+Webhook()");
        if (!await PaymentsEnabled())
        {
            _logger.LogDebug("-Webhook()");
            return new NotFoundResult();
        }
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        try
        {
            var stripeEvent = EventUtility.ParseEvent(json);
            var signatureHeader = Request.Headers["Stripe-Signature"];

            stripeEvent = EventUtility.ConstructEvent(json,
                    signatureHeader, _config.WebhookSecret);

            if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted)
            {
                if (stripeEvent.Data.Object is not Session checkoutSession)
                {
                    _logger.LogDebug("Webhook(): received bad checkout session object");
                    _logger.LogDebug("-Webhook()");
                    return new BadRequestResult();
                }

                bool success = checkoutSession.PaymentStatus == "paid" && checkoutSession.Status == "complete";

                if (success)
                {
                    _paymentRepo.UpdatePaymentSuccess(checkoutSession.ClientReferenceId);

                    PurchaseDetails? purchase = _db.Purchases.Where(x => x.OrderId == checkoutSession.ClientReferenceId).FirstOrDefault();

                    if (purchase is null)
                    {
                        _logger.LogError("Webhook(): Unable to reconcile purchase {id}", checkoutSession.Id);
                        return new StatusCodeResult(500);
                    }

                    _logger.LogDebug("Webhook(): found purchase of {productId} for quantity {quantity}, orderId {orderId}", purchase.ProductId, purchase.Quantity, checkoutSession.ClientReferenceId);

                    if (!await TryProcessLineItem(purchase.CustomerId, purchase.ProductId, purchase.Quantity))
                    {
                        _logger.LogError("Webhook(): Unable to complete purchase {id}", checkoutSession.Id);
                        _logger.LogDebug("-Webhook()");
                        return new StatusCodeResult(500);
                    }

                    _logger.LogDebug("-Webhook()");
                    return new OkResult();
                }
                else
                {
                    _paymentRepo.UpdatePaymentFailed(checkoutSession.ClientReferenceId);
                }
            }

            _logger.LogDebug("Webhook(): Unhandled event type: {type}", stripeEvent.Type);
            _logger.LogDebug("-Webhook()");
            return new StatusCodeResult(500);
        }
        catch (StripeException ex)
        {
            _logger.LogError("Webhook Exception. Message: {message}, StackTrace: {stackTrace}, InnerException: {innerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message);
            _logger.LogDebug("-Webhook()");
            return new BadRequestResult();
        }
        catch (Exception ex)
        {
            _logger.LogError("Webhook Exception. Message: {message}, StackTrace: {stackTrace}, InnerException: {innerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message);
            _logger.LogDebug("-Webhook()");
            return new StatusCodeResult(500);
        }
    }
}