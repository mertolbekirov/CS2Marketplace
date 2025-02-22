using Microsoft.AspNetCore.Mvc;

namespace CS2Marketplace.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View(); // Will look for Views/Home/Index.cshtml
        }
    }
}