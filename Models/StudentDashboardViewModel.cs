namespace OnlineClearanceSystem.Models
{
    public class StudentDashboardViewModel
    {
        public required string StudentName      { get; set; }
        public int             SubjectCleared   { get; set; }
        public int             SubjectIncomplete { get; set; }
        public int             OrgCleared       { get; set; }
        public int             TotalSubjects    { get; set; }  // ← new
        public int             TotalOrgs        { get; set; }  // ← new
        public string          ActivePeriod     { get; set; } = "A.Y. 2025-2026, 2nd Semester"; // ← new
        public List<AnnouncementItem> Announcements { get; set; } = new(); // ← new
    }

    public class AnnouncementItem
    {
        public string Title   { get; set; } = "";
        public string Content { get; set; } = "";
        public string Type    { get; set; } = "General"; // Pinned, Urgent, Event, General
        public string Date    { get; set; } = "";
    }
}