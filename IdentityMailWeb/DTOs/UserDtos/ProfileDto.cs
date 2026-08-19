using System.ComponentModel.DataAnnotations;

namespace IdentityMail.Web.DTOs.UserDtos
{
    public class ProfileDto
    {
        [Required(ErrorMessage = "Ad alanı boş bırakılamaz.")]
        public string FirstName { get; set; }
        
        [Required(ErrorMessage = "Soyad alanı boş bırakılamaz.")]
        public string LastName { get; set; }
        
        [Url(ErrorMessage = "Geçerli bir görsel bağlantısı giriniz.")]
        public string? ProfileImageUrl { get; set; }

    }
}
