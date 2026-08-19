using IdentityMail.Web.Context;
using IdentityMail.Web.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace IdentityMail.Web.Controllers
{
    [Authorize]
    public class CategoryController(AppDbContext _context) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .OrderBy(x => x.Name)
                .ToListAsync();

            return View(categories);
        }

        [HttpPost]
        public async Task<IActionResult> Create(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Kategori adı boş olamaz.";
                return RedirectToAction(nameof(Index));
            }

            var category = new Category
            {
                Name = name.Trim()
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(x => x.Id == id);

            if (category == null)
                return NotFound();

            bool hasMessages = await _context.UserMessages
                .AnyAsync(x => x.CategoryId == id);

            if (hasMessages)
            {
                TempData["Error"] = "Bu kategoriye bağlı mesajlar var. Önce mesajların kategorisini kaldırın veya değiştirin.";
                return RedirectToAction(nameof(Index));
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));

        }
    }
}
