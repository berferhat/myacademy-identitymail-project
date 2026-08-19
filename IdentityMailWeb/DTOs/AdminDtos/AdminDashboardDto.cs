namespace IdentityMail.Web.DTOs.AdminDtos
{
    public class AdminDashboardDto
    {
        public int UserCount { get; set; }
        public int MessageCount { get; set; }
        public int UnreadCount { get; set; }
        public int TrashCount { get; set; }

        public List<TopSenderDto> TopSenders { get; set; }
        public List<TopCategoryDto> TopCategories { get; set; }
    }
}
