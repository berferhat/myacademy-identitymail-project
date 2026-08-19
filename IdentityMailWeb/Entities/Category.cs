namespace IdentityMail.Web.Entities
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public List<UserMessage> Messages { get; set; }
    }
}
