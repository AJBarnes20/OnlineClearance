namespace OnlineClearanceSystem.Models
{
    public class StudentProfileViewModel
    {
        public string StudentId     { get; set; } = "";
        public string FirstName     { get; set; } = "";
        public string MiddleInitial { get; set; } = "";
        public string LastName      { get; set; } = "";
        public string Suffix        { get; set; } = "";
        public string Course        { get; set; } = "";
        public string YearLevel     { get; set; } = "";
        public string Section       { get; set; } = "";
        public string Email         { get; set; } = "";
        public string Password      { get; set; } = "";
        public List<string> AvailableCourses { get; set; } = [];
        public string? SignaturePath { get; set; }
        public List<OrganizationSignatory> Positions { get; set; } = new();

        public string FullName =>
            $"{FirstName} {(string.IsNullOrEmpty(MiddleInitial) ? "" : MiddleInitial + ". ")}{LastName} {Suffix}".Trim();
    }

    // ← This is what was missing
     
}