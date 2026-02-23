namespace NextTurn.Api.Models
{
    public class Organization
    {
        public int OrganizationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}