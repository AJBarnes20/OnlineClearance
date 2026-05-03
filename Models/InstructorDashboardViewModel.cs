namespace OnlineClearanceSystem.Models
{
    public class InstructorDashboardViewModel
    {
        public string InstructorName  { get; set; } = "";
        public string ActivePeriod    { get; set; } = "—";
        public int    SubjectAssigned { get; set; }
        public int    TotalStudents   { get; set; }
        public int    ClearedStudents { get; set; }
        public int    PendingStudents { get; set; }
        public List<AnnouncementItem> Announcements { get; set; } = new();
    }
}