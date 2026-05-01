namespace OnlineClearanceSystem.Models
{
    public class StudentClearancePdfViewModel
    {
        public string StudentName  { get; set; } = "";
        public string StudentId    { get; set; } = "";
        public string CourseYear   { get; set; } = "";
        public string AySemester   { get; set; } = "";
        public List<PdfSubjectItem>      Subjects      { get; set; } = new();
        public List<PdfOrganizationItem> Organizations { get; set; } = new();
    }

    public class PdfSubjectItem
    {
        public string MisCode        { get; set; } = "";
        public string SubjectCode    { get; set; } = "";
        public string Description    { get; set; } = "";
        public string InstructorName { get; set; } = "";
        public string Status         { get; set; } = "";
    }

    public class PdfOrganizationItem
    {
        public string Role       { get; set; } = "";
        public string PersonName { get; set; } = "";
        public string Status     { get; set; } = "";
    }
}