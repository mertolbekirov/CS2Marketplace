using Microsoft.AspNetCore.Mvc;
using CS2Marketplace.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace CS2Marketplace.ViewComponents
{
    public class BalanceViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _db;

        public BalanceViewComponent(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Get the current user's SteamId from session
            string steamId = HttpContext.Session.GetString("SteamId");
            decimal balance = 0;
            if (!string.IsNullOrEmpty(steamId))
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.SteamId == steamId);
                if (user != null)
                {
                    balance = user.Balance;
                }
            }
            return View("Default", balance);
        }
    }
}
