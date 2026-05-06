namespace OnlineClearanceSystem.Models
{
    public class OrganizationSignatory
    {
        public int    Id         { get; set; }
        public string OrgName    { get; set; } = "";  // ← add this
        public string OrgRole    { get; set; } = "";
        public string PersonName { get; set; } = "";
        public string Status     { get; set; } = "";
    }
}