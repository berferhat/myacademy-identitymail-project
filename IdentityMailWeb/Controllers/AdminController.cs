using IdentityMail.Web.Context;
using IdentityMail.Web.DTOs.AdminDtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Threading.Tasks;

namespace IdentityMail.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController(AppDbContext _context) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var dashboard = new AdminDashboardDto
            {
                UserCount = await _context.Users.CountAsync(),
                
                MessageCount = await _context.UserMessages
                    .CountAsync(x => !x.IsDraft),
                
                UnreadCount = await _context.UserMessages
                    .CountAsync(x => !x.IsRead && !x.IsDeleted && !x.IsDraft),
                
                TrashCount = await _context.UserMessages
                    .CountAsync(x => x.IsDeleted),
                
                TopSenders = await _context.UserMessages
                    .Where(x => !x.IsDraft)
                    .GroupBy(x => new
                    {
                        x.SenderId,
                        x.Sender.FirstName,
                        x.Sender.LastName,
                        x.Sender.Email
                    })
                    .Select(g => new TopSenderDto
                    {
                        FullName = g.Key.FirstName + " " + g.Key.LastName,
                        Email = g.Key.Email,
                        MessageCount = g.Count()
                    })
                    .OrderByDescending(x => x.MessageCount)
                    .Take(5)
                    .ToListAsync(),
                
                TopCategories = await _context.UserMessages
                    .Where(x => !x.IsDraft && x.CategoryId != null)
                    .GroupBy(x => x.Category.Name)
                    .Select(g => new TopCategoryDto
                    {
                        Name = g.Key,
                        MessageCount = g.Count()
                    })
                    .OrderByDescending(x => x.MessageCount)
                    .Take(5)
                    .ToListAsync()
            };
            return View(dashboard);
        }
    }
}
