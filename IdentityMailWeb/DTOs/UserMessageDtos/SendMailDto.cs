namespace IdentityMail.Web.DTOs.UserMessageDtos
{
    public class SendMailDto
    {
        public int Id { get; set; }   // 0 = yeni mail, >0 = düzenlenen taslak
        public string ReceiverMail { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }

        public int? CategoryId { get; set; }

    }
}
