namespace IdentityMail.Web.DTOs.AdminDtos
{
    public class TopSenderDto
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public int MessageCount { get; set; }
    }
}