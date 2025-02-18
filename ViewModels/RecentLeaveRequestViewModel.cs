namespace EMS_Project.ViewModels
{
    public class RecentLeaveRequestViewModel
    {
        public string EmployeeName  { get; set; } = string.Empty;
        public string ProfileImage  { get; set; } = string.Empty;
        public string LeaveTypeName { get; set; } = string.Empty;
        public string RequestStatus { get; set; } = string.Empty;
        public string RequestTime   { get; set; } = string.Empty;
        public string StartDate { get; set; } = string.Empty; // Added StartDate
        public string EndDate { get; set; } = string.Empty;   // Added EndDate
    }
}
