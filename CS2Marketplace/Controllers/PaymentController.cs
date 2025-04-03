﻿using Microsoft.AspNetCore.Mvc;
using CS2Marketplace.Services;
using System.Threading.Tasks;
using Stripe.Checkout;
using CS2Marketplace.Data;
using Microsoft.EntityFrameworkCore;
using CS2Marketplace.Models;
using CS2Marketplace.Filters;

namespace CS2Marketplace.Controllers
{
    [RequireAuthentication]
    public class PaymentController : Controller
    {
        private readonly PaymentService _paymentService;

        public PaymentController(PaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpGet]
        public IActionResult Deposit()
        {
            return View();
        }

        // POST: /Payment/Deposit
        [HttpPost]
        public async Task<IActionResult> Deposit(decimal amount)
        {
            // Retrieve the current user from your database (e.g., via session SteamId)
            string steamId = HttpContext.Session.GetString("SteamId");


            Session session = await _paymentService.CreateDepositSessionAsync(user, amount);
            return Redirect(session.Url);
        }

        // GET: /Payment/Success?session_id=...
        public async Task<IActionResult> Success(string session_id)
        {
            if (string.IsNullOrEmpty(session_id))
            {
                return RedirectToAction("Index", "Home");
            }

            decimal? depositedAmount = await _paymentService.ConfirmDepositSessionAsync(session_id);
            if (depositedAmount.HasValue)
            {
                ViewBag.Message = $"Deposit successful: {depositedAmount.Value:C} added to your balance.";
            }
            else
            {
                ViewBag.Message = "Payment not confirmed. Please contact support.";
            }
            return View();
        }

        // GET: /Payment/Cancel
        public IActionResult Cancel()
        {
            ViewBag.Message = "Deposit canceled.";
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateTestCharge(decimal amount)
        {
            if (!_paymentService._isTestMode)
                return BadRequest("This endpoint is only available in test mode");

            var success = await _paymentService.CreateTestChargeForAvailableBalance(amount);
            if (success)
                return RedirectToAction("Profile", "Account");
            
            return BadRequest("Failed to create test charge");
        }
    }
}
