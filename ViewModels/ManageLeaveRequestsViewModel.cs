using EMS_Project.Models;
using X.PagedList;

namespace EMS_Project.ViewModels
{
    public class ManageLeaveRequestsViewModel
    {
        public IPagedList<LeaveRequest> PendingRequests { get; set; } = null!;
        public IPagedList<LeaveRequest> ProcessedRequests { get; set; } = null!;
    }
}
