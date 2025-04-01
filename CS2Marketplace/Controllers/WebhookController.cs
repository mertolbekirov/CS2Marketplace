using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Stripe;
using System;
using System.Threading.Tasks;
using CS2Marketplace.Services;

namespace CS2Marketplace.Controllers
{
    public class WebhookController : Controller
    {
        private readonly PaymentService _paymentService;
        private readonly string _webhookSecret;

        public WebhookController(PaymentService paymentService, IConfiguration configuration)
        {
            _paymentService = paymentService;
            _webhookSecret = configuration["Stripe:WebhookSecret"];
        }

        [HttpPost]
        public async Task<IActionResult> Stripe()
        {
            var json = await new System.IO.StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                _webhookSecret
            );

            try
            {
                if (stripeEvent.Type == "transfer.updated")
                {
                    var transfer = stripeEvent.Data.Object as Transfer;
                    if (transfer != null)
                    {
                        // A transfer is considered paid if it's not reversed and has no amount_reversed
                        if (!transfer.Reversed && transfer.AmountReversed == 0)
                        {
                            await _paymentService.HandleTransferWebhookAsync("transfer.paid", transfer.Id);
                        }
                        // If the transfer is reversed or has amount_reversed, it's either failed or canceled
                        else if (transfer.Reversed || transfer.AmountReversed > 0)
                        {
                            await _paymentService.HandleTransferWebhookAsync("transfer.canceled", transfer.Id);
                        }
                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                // Log the error but return 200 to acknowledge receipt
                // This prevents Stripe from retrying the webhook
                Console.WriteLine($"Error processing webhook: {ex.Message}");
                return Ok();
            }
        }
    }
} 