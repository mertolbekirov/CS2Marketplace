using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;

namespace CS2Marketplace.Filters
{
    public class RequireAuthenticationAttribute : TypeFilterAttribute
    {
        public RequireAuthenticationAttribute() : base(typeof(RequireAuthenticationFilter))
        {
        }

        private class RequireAuthenticationFilter : IAuthorizationFilter
        {
            public void OnAuthorization(AuthorizationFilterContext context)
            {
                var steamId = context.HttpContext.Session.GetString("SteamId");
                if (string.IsNullOrEmpty(steamId))
                {
                    context.Result = new RedirectToActionResult("SignIn", "Auth", null);
                }
            }
        }
    }
} 