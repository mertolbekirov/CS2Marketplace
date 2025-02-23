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

            // Find the user in the database
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.SteamId == steamId);
            if (user == null)
            {
                return RedirectToAction("SignIn", "Auth");
            }

            return View(user);
        }
    }
}
