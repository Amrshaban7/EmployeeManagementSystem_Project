using System;
using System.Collections.Generic;

namespace EMS_Project.Models;

public partial class LeaveType
{
    public int Id { get; set; }

    public string LeaveTypeName { get; set; } = null!;

    public virtual ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
}
