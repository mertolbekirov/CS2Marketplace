using Microsoft.AspNetCore.Mvc;
using CS2Marketplace.Services;
using System.Threading.Tasks;
using CS2Marketplace.Data;
using Microsoft.EntityFrameworkCore;
using CS2Marketplace.Models;
using CS2Marketplace.Services.Interfaces;

namespace CS2Marketplace.Controllers
{
    public class WithdrawalController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly ApplicationDbContext _dbContext;

        public WithdrawalController(IPaymentService paymentService, ApplicationDbContext dbContext)
        {
            _paymentService = paymentService;
            _dbContext = dbContext;
        }

        // GET: /Withdrawal/Request
        public async Task<IActionResult> Request()
        {
            string steamId = HttpContext.Session.GetString("SteamId");
            var user = await _dbContext.Users
                .Include(u => u.WalletTransactions)
                .FirstOrDefaultAsync(u => u.SteamId == steamId);
                
            if (user == null)
            {
                return RedirectToAction("SignIn", "Auth");
            }

            // Check if user has a valid email address
            if (string.IsNullOrEmpty(user.Email) || !user.Email.Contains("@"))
            {
                TempData["Error"] = "A valid email address is required to withdraw funds. Please update your email address in your profile.";
                return RedirectToAction("Profile", "Account");
            }

            return View(user);
        }

        // POST: /Withdrawal/Request
        [HttpPost]
        public async Task<IActionResult> Request(decimal amount)
        {
            string steamId = HttpContext.Session.GetString("SteamId");
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.SteamId == steamId);
            if (user == null)
            {
                return RedirectToAction("SignIn", "Auth");
            }

            // Validate amount
            if (amount <= 0)
            {
                TempData["Error"] = "Please enter a valid withdrawal amount.";
                return RedirectToAction("Request");
            }

            // Ensure user has enough balance
            if (user.Balance < amount)
            {
                TempData["Error"] = "Insufficient balance for withdrawal.";
                return RedirectToAction("Request");
            }

            // Process the withdrawal
            var (success, errorMessage) = await _paymentService.ProcessWithdrawalAsync(user, amount);
            
            if (success)
            {
                // Update user's balance only after successful withdrawal processing
                await _dbContext.SaveChangesAsync();
                TempData["Message"] = $"Withdrawal of {amount:C} has been initiated. The transaction is pending and will be processed within 1-3 business days.";
            }
            else
            {
                TempData["Error"] = errorMessage ?? "Withdrawal failed. Please try again later.";
            }
            
            return RedirectToAction("Request");
        }
    }
}
