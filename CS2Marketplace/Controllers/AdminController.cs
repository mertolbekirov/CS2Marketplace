using Microsoft.AspNetCore.Mvc;
using CS2Marketplace.Services.Interfaces;
using System.Threading.Tasks;
using CS2Marketplace.Filters;

namespace CS2Marketplace.Controllers
{
    [RequireAdmin]
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly IUserService _userService;

        public AdminController(IAdminService adminService, IUserService userService)
        {
            _adminService = adminService;
            _userService = userService;
        }

        // GET: /Admin/Index
        public IActionResult Index()
        {
            return View();
        }

        // GET: /Admin/Disputes
        public async Task<IActionResult> Disputes()
        {
            var disputes = await _adminService.GetDisputedTradesAsync();
            return View(disputes);
        }

        // GET: /Admin/DisputeDetails/{id}
        public async Task<IActionResult> DisputeDetails(int id)
        {
            var trade = await _adminService.GetDisputeDetailsAsync(id);

            if (trade == null)
                return NotFound();

            return View(trade);
        }

        // POST: /Admin/ResolveDispute/{id}
        [HttpPost]
        public async Task<IActionResult> ResolveDispute(int id, bool refundBuyer, string resolution, string adminNotes)
        {
            var (success, message) = await _adminService.ResolveDisputeAsync(id, refundBuyer, resolution, adminNotes);

            if (!success)
            {
                TempData["Error"] = message;
                return RedirectToAction(nameof(DisputeDetails), new { id });
            }

            TempData["Success"] = message;
            return RedirectToAction(nameof(Disputes));
        }

        // POST: /Admin/ProcessExpiredTrades
        [HttpPost]
        public async Task<IActionResult> ProcessExpiredTrades()
        {
            var (success, message) = await _adminService.ProcessExpiredTradesAsync();

            if (!success)
            {
                TempData["Error"] = message;
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = message;
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/FindUser
        public IActionResult FindUser()
        {
            return View();
        }

        // POST: /Admin/FindUserBysteamId
        [HttpPost]
        public async Task<IActionResult> FindUserBySteamId(string steamId)
        {
            var user = await _userService.GetUserWithTransactionsAsync(steamId);
            
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(FindUser));
            }

            return RedirectToAction(nameof(UserDetails), new { id = user.Id });
        }

        // GET: /Admin/UserDetails/{id}
        public async Task<IActionResult> UserDetails(int id)
        {
            var user = await _userService.GetUserDetailsByIdAsync(id);
            
            if (user == null)
                return NotFound();

            return View(user);
        }

        // POST: /Admin/MakeUserAdmin/{id}
        [HttpPost]
        public async Task<IActionResult> MakeUserAdmin(string steamId)
        {
            var (success, message) = await _adminService.MakeUserAdminAsync(steamId);

            if (!success)
            {
                TempData["Error"] = message;
                return RedirectToAction(nameof(FindUser));
            }

            TempData["Success"] = message;
            return RedirectToAction(nameof(FindUser));
        }
    }
} 