using IdentityMail.Web.Context;
using IdentityMail.Web.DTOs.UserMessageDtos;
using IdentityMail.Web.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace IdentityMail.Web.Controllers
{
    [Authorize]
    public class MessageController(UserManager<AppUser> _userManager, AppDbContext _context) : Controller
    {
        public async Task<IActionResult> Index(
            string filter = "all",
            int? categoryId = null,
            string search = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int page = 1
            )
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            ViewBag.fullName = user.FirstName + " " + user.LastName;
            ViewBag.filter = filter;
            ViewBag.categoryId = categoryId;
            ViewBag.Categories = await _context.Categories
                .OrderBy(x => x.Name)
                .ToListAsync();
            ViewBag.search = search;
            ViewBag.startDate = startDate;
            ViewBag.endDate = endDate;

            IQueryable<UserMessage> query = _context.UserMessages
                .Include(x => x.Sender)
                .Include(x => x.Category)
                .Where(x => x.ReceiverId == user.Id && !x.IsDeleted && !x.IsDraft);


            const int pageSize = 10;

            if (page < 1)
                page = 1;

            ViewBag.page = page;
            ViewBag.pageSize = pageSize;

            if (filter == "unread")
                query = query.Where(x => !x.IsRead);
            else if (filter == "read")
                query = query.Where(x => x.IsRead);

            if (categoryId.HasValue)
                query = query.Where(x => x.CategoryId == categoryId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(x =>
                x.Subject.Contains(search) ||
                x.Sender.FirstName.Contains(search) ||
                x.Sender.LastName.Contains(search) ||
                x.Sender.Email.Contains(search));
            }

            if (startDate.HasValue)
                query = query.Where(x => x.SendDate >= startDate.Value.Date);

            if (endDate.HasValue)
                query = query.Where(x => x.SendDate < endDate.Value.Date.AddDays(1));

            int totalCount = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            if (totalPages > 0 && page > totalPages)
                page = 1;

            ViewBag.totalCount = totalCount;
            ViewBag.totalPages = totalPages;
            ViewBag.page = page;

            var messages = await query
                .OrderByDescending(x => x.SendDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return View(messages);
        }

        public async Task<IActionResult> Sent()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var messages = await _context.UserMessages
                .Include(x => x.Receiver)
                .Include(x => x.Category)
                .Where(x => x.SenderId == user.Id && !x.IsDraft)
                .OrderByDescending(x => x.SendDate)
                .ToListAsync();

            return View(messages);
        }

        public async Task<IActionResult> Important()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var messages = await _context.UserMessages
                .Include(x => x.Sender)
                .Include(x => x.Category)
                .Where(x => x.ReceiverId == user.Id && x.IsImportant && !x.IsDeleted && !x.IsDraft)
                .OrderByDescending(x => x.SendDate)
                .ToListAsync();

            return View(messages);
        }

        public async Task<IActionResult> Trash()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var messages = await _context.UserMessages
                .Include(x => x.Sender)
                .Include(x => x.Category)
                .Where(x => x.ReceiverId == user.Id && x.IsDeleted)
                .OrderByDescending(x => x.SendDate)
                .ToListAsync();

            return View(messages);
        }

        [HttpGet]
        public async Task<IActionResult> SendMail()
        {
            await LoadCategoriesAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SendMail(SendMailDto sendMailDto)
        {
            ModelState.Remove("CategoryId");
            ModelState.Remove("Id");

            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync();
                return View(sendMailDto);
            }

            var sender = await _userManager.GetUserAsync(User);
            var receiver = await _userManager.FindByEmailAsync(sendMailDto.ReceiverMail);

            if (sender == null)
                return Challenge();

            if (receiver is null)
            {
                ModelState.AddModelError(string.Empty, "Girdiğiniz mail ile sistemde kayıtlı kullanıcı bulunamadı.");
                await LoadCategoriesAsync();
                return View(sendMailDto);
            }

            var newMessage = new UserMessage
            {
                SendDate = DateTime.Now,
                ReceiverId = receiver.Id,
                SenderId = sender.Id,
                Subject = sendMailDto.Subject,
                Body = sendMailDto.Body,
                IsDraft = false,
                CategoryId = sendMailDto.CategoryId
            };

            _context.UserMessages.Add(newMessage);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Reply(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return Challenge();

            var message = await _context.UserMessages
                .Include(x => x.Sender)
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.ReceiverId == currentUser.Id &&
                    !x.IsDeleted);

            if (message == null)
                return NotFound();

            var subject = message.Subject.StartsWith("Re: ")
                ? message.Subject
                : "Re: " + message.Subject;

            var sendMailDto = new SendMailDto
            {
                ReceiverMail = message.Sender.Email,
                Subject = subject,
                Body = $"\n\n---\n{message.Sender.FirstName} {message.Sender.LastName} yazdı:\n{message.Body}"
            };

            await LoadCategoriesAsync();
            return View("SendMail", sendMailDto);
        }

        public async Task<IActionResult> MailDetail(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return Challenge();

            var message = await _context.UserMessages
                .Include(x => x.Sender)
                .Include(x => x.Receiver)
                .Include(x => x.Category)
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    (x.ReceiverId == currentUser.Id || x.SenderId == currentUser.Id));

            if (message == null)
                return NotFound();

            if (message.ReceiverId == currentUser.Id && !message.IsRead && !message.IsDeleted)
            {
                message.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return View(message);
        }

        public async Task<IActionResult> ToggleImportant(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return Challenge();

            var message = await _context.UserMessages
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.ReceiverId == currentUser.Id &&
                    !x.IsDeleted);

            if (message == null)
                return NotFound();

            message.IsImportant = !message.IsImportant;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> MoveToTrash(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return Challenge();

            var message = await _context.UserMessages
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.ReceiverId == currentUser.Id &&
                    !x.IsDeleted);

            if (message == null)
                return NotFound();

            message.IsDeleted = true;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Restore(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return Challenge();

            var message = await _context.UserMessages
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.ReceiverId == currentUser.Id &&
                    x.IsDeleted);

            if (message == null)
                return NotFound();

            message.IsDeleted = false;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Trash));
        }

        public async Task<IActionResult> Drafts()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var drafts = await _context.UserMessages
                .Include(x => x.Receiver)
                .Include(x => x.Category)
                .Where(x => x.SenderId == user.Id && x.IsDraft)
                .OrderByDescending(x => x.SendDate)
                .ToListAsync();

            return View(drafts);
        }

        [HttpPost]
        public async Task<IActionResult> SaveDraft(SendMailDto sendMailDto)
        {
            ModelState.Remove("CategoryId");
            ModelState.Remove("Id");
            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync();
                return View("SendMail", sendMailDto);
            }

            var sender = await _userManager.GetUserAsync(User);
            var receiver = await _userManager.FindByEmailAsync(sendMailDto.ReceiverMail);

            if (sender == null)
                return Challenge();

            if (receiver is null)
            {
                ModelState.AddModelError(string.Empty, "Girdiğiniz mail ile sistemde kayıtlı kullanıcı bulunamadı.");
                await LoadCategoriesAsync();

                return View("SendMail", sendMailDto);
            }

            var draft = new UserMessage
            {
                SendDate = DateTime.Now,
                ReceiverId = receiver.Id,
                SenderId = sender.Id,
                Subject = sendMailDto.Subject,
                Body = sendMailDto.Body,
                IsDraft = true,
                CategoryId = sendMailDto.CategoryId
            };

            _context.UserMessages.Add(draft);
            await _context.SaveChangesAsync();
            await LoadCategoriesAsync();

            return RedirectToAction(nameof(Drafts));
        }

        public async Task<IActionResult> SendDraft(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var draft = await _context.UserMessages
                .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.SenderId == user.Id &&
                x.IsDraft);

            if (draft == null)
                return NotFound();

            draft.IsDraft = false;
            draft.SendDate = DateTime.Now;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Sent));
        }

        public async Task<IActionResult> DeleteDraft(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var draft = await _context.UserMessages
                .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.SenderId == user.Id &&
                x.IsDraft);

            if (draft == null)
                return NotFound();

            _context.UserMessages.Remove(draft);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Drafts));
        }

        [HttpGet]
        public async Task<IActionResult> EditDraft(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var draft = await _context.UserMessages
                .Include(x => x.Receiver)
                .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.SenderId == user.Id &&
                x.IsDraft);

            if (draft == null)
                return NotFound();

            var dto = new SendMailDto
            {
                Id = draft.Id,
                ReceiverMail = draft.Receiver.Email,
                Subject = draft.Subject,
                Body = draft.Body,
                CategoryId = draft.CategoryId
            };

            await LoadCategoriesAsync();
            return View("SendMail", dto);
        }

        [HttpPost]
        public async Task<IActionResult> EditDraft(SendMailDto sendMailDto)
        {
            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync();
                return View("SendMail", sendMailDto);
            }

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var draft = await _context.UserMessages
                .FirstOrDefaultAsync(x =>
                x.Id == sendMailDto.Id &&
                x.SenderId == user.Id &&
                x.IsDraft);

            if (draft == null)
                return NotFound();

            var receiver = await _userManager.FindByEmailAsync(sendMailDto.ReceiverMail);

            if (receiver is null)
            {
                ModelState.AddModelError(string.Empty, "Girdiğiniz mail ile sistemde kayıtlı kullanıcı bulunamadı.");
                await LoadCategoriesAsync();
                return View("SendMail", sendMailDto);
            }

            draft.ReceiverId = receiver.Id;
            draft.Subject = sendMailDto.Subject;
            draft.Body = sendMailDto.Body;
            draft.SendDate = DateTime.Now;
            draft.CategoryId = sendMailDto.CategoryId;

            await _context.SaveChangesAsync();
            await LoadCategoriesAsync();

            return RedirectToAction(nameof(Drafts));
        }

        private async Task LoadCategoriesAsync()
        {
            ViewBag.Categories = new SelectList(
                await _context.Categories.OrderBy(x => x.Name).ToListAsync(),
                "Id",
                "Name");
        }
    }
}