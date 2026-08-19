using IdentityMail.Web.DTOs.UserDtos;
using IdentityMail.Web.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;


namespace IdentityMail.Web.Controllers
{
    [Authorize]
    public class ProfileController(UserManager<AppUser> _userManager) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var profileDto = new ProfileDto
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                ProfileImageUrl = user.ProfileImageUrl
            };

            return View(profileDto);
        }

        [HttpPost]
        public async Task<IActionResult> Index(ProfileDto profileDto)
        {
            if (!ModelState.IsValid)
                return View(profileDto);

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            user.FirstName = profileDto.FirstName;
            user.LastName = profileDto.LastName;
            user.ProfileImageUrl = profileDto.ProfileImageUrl;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                
                return View(profileDto);
            }

            TempData["SuccessMessage"] = "Profil bilgileriniz güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto changePasswordDto)
        {
            if (!ModelState.IsValid)
                return View(changePasswordDto);

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var result = await _userManager.ChangePasswordAsync(
                user,
                changePasswordDto.CurrentPassword,
                changePasswordDto.NewPassword);

            if(!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                return View(changePasswordDto);
            }

            TempData["SuccessMessage"] = "Şifreniz güncellendi.";

            return RedirectToAction(nameof(ChangePassword));
        }
    }
}
