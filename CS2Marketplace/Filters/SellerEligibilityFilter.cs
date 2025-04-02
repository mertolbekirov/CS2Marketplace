using System;
using System.Threading.Tasks;
using CS2Marketplace.Models;
using CS2Marketplace.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using CS2Marketplace.Data;
using CS2Marketplace.Services;

namespace CS2Marketplace.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class SellerEligibilityFilter : Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var userService = context.HttpContext.RequestServices.GetRequiredService<IUserService>();
            var dbContext = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
            
            // Get SteamId from session
            var steamId = context.HttpContext.Session.GetString("SteamId");
            if (string.IsNullOrEmpty(steamId))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            // Get user from database
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.SteamId == steamId);
            if (user == null)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var isEligible = await userService.VerifyUserSellerEligibilityAsync(user);
            if (!isEligible)
            {
                context.Result = new BadRequestObjectResult(new
                {
                    Error = "User is not eligible for trading",
                    Message = user.VerificationMessage
                });
                return;
            }

            // Save any changes to the user (like LastVerificationCheck)
            await dbContext.SaveChangesAsync();

            await next();
        }
    }
} 