using Microsoft.AspNetCore.Mvc;
using CS2Marketplace.Services;
using System.Threading.Tasks;
using CS2Marketplace.Data;
using Microsoft.EntityFrameworkCore;

namespace CS2Marketplace.Controllers
{
    public class WithdrawalController : Controller
    {
        private readonly PaymentService _paymentService;
        private readonly ApplicationDbContext _dbContext;

        public WithdrawalController(PaymentService paymentService, ApplicationDbContext dbContext)
        {
            _paymentService = paymentService;
            _dbContext = dbContext;
        }

        // GET: /Withdrawal/Request
        public IActionResult Request()
        {
            return View();
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

            // Ensure user has enough balance.
            if (user.Balance < amount)
            {
                TempData["Error"] = "Insufficient balance for withdrawal.";
                return RedirectToAction("Profile", "Account");
            }

            bool success = await _paymentService.ProcessWithdrawalAsync(user, amount);
            if (success)
            {
                user.Balance -= amount;
                await _dbContext.SaveChangesAsync();
                TempData["Message"] = $"Withdrawal of {amount:C} processed successfully.";
            }
            else
            {
                TempData["Error"] = "Withdrawal failed. Please try again later.";
            }
            return RedirectToAction("Profile", "Account");
        }
    }
}
