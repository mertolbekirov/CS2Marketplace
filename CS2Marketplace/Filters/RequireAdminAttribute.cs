using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using CS2Marketplace.Data;
using CS2Marketplace.Services.Interfaces;
using System.Threading.Tasks;

namespace CS2Marketplace.Filters
{
    public class RequireAdminAttribute : TypeFilterAttribute
    {
        public RequireAdminAttribute() : base(typeof(RequireAdminFilter))
        {
        }

        private class RequireAdminFilter : IAsyncAuthorizationFilter
        {
            private readonly IUserService _userService;

            public RequireAdminFilter(IUserService userService)
            {
                _userService = userService;
            }

            public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
            {
                var steamId = context.HttpContext.Session.GetString("SteamId");
                
                if (string.IsNullOrEmpty(steamId))
                {
                    context.Result = new RedirectToActionResult("SignIn", "Auth", null);
                    return;
                }

                var user = await _userService.GetUserBySteamIdAsync(steamId);
                
                if (user == null || !user.IsAdmin)
                {
                    context.Result = new ForbidResult();
                }
            }
        }
    }
} 