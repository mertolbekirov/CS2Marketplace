using Microsoft.AspNetCore.Mvc;
using CS2Marketplace.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using CS2Marketplace.Models;

namespace CS2Marketplace.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        public AccountController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // GET: /Account/Profile
        public async Task<IActionResult> Profile()
        {
            var steamId = HttpContext.Session.GetString("SteamId");
            if (string.IsNullOrEmpty(steamId))
            {
                return RedirectToAction("SignIn", "Auth");
            }

            // Find the user in the database and include their wallet transactions
            var user = await _dbContext.Users
                .Include(u => u.WalletTransactions)
                .FirstOrDefaultAsync(u => u.SteamId == steamId);
                
            if (user == null)
            {
                return RedirectToAction("SignIn", "Auth");
            }

            return View(user);
        }

        // POST: /Account/UpdateEmail
        [HttpPost]
        public async Task<IActionResult> UpdateEmail(string email)
        {
            if (string.IsNullOrEmpty(email) || !email.Contains("@"))
            {
                TempData["Error"] = "Please enter a valid email address.";
                return RedirectToAction("Profile");
            }

            var steamId = HttpContext.Session.GetString("SteamId");
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.SteamId == steamId);
            
            if (user == null)
            {
                return RedirectToAction("SignIn", "Auth");
            }

            user.Email = email;
            await _dbContext.SaveChangesAsync();

            TempData["Message"] = "Your email address has been updated successfully.";
            return RedirectToAction("Profile");
        }
    }
}
