using IdentityMail.Web.Context;
using IdentityMail.Web.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IdentityMail.Web.ViewComponents
{
    public class UnreadMessageCountViewComponent(
        UserManager<AppUser> _userManager,
        AppDbContext _context) : ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
                return View(0);

            var user = await _userManager.GetUserAsync(HttpContext.User);

            if (user == null)
                return View(0);

            // Bana gelen + okunmamış + çöpte olmayan
            int unreadCount = await _context.UserMessages
                .CountAsync(x =>
                    x.ReceiverId == user.Id &&
                    x.IsRead == false &&
                    x.IsDeleted == false &&
                    x.IsDraft == false);

            return View(unreadCount);
        }
    }
}
