using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CS2Marketplace.Services.Interfaces;
using CS2Marketplace.Filters;

namespace CS2Marketplace.Controllers
{
    [RequireAuthentication]
    public class WithdrawalController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly IUserService _userService;

        public WithdrawalController(IPaymentService paymentService, IUserService userService)
        {
            _paymentService = paymentService;
            _userService = userService; 
        }

        // GET: /Withdrawal
        public async Task<IActionResult> Request()
        {
            string steamId = HttpContext.Session.GetString("SteamId");
            var user = await _userService.GetUserBySteamIdAsync(steamId);
            return View(user);
        }

        // POST: /Withdrawal/Process
        [HttpPost]
        public async Task<IActionResult> Request(decimal amount)
        {
            string steamId = HttpContext.Session.GetString("SteamId");
            var user = await _userService.GetUserBySteamIdAsync(steamId);

            var (success, message) = await _paymentService.ProcessWithdrawalAsync(user, amount);

            if (!success)
            {
                TempData["Error"] = message;
                return RedirectToAction(nameof(Request));
            }

            TempData["Success"] = message;
            return RedirectToAction(nameof(Request));
        }
    }
}
