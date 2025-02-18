using System;
using System.Collections.Generic;

namespace EMS_Project.Models;

public partial class LeaveRequest
{
    public int Id { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public string RequestStatus { get; set; } = null!;

    public DateTime CreatedDate { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public int EmployeeId { get; set; }

    public int LeaveTypeId { get; set; }

    public virtual Employee Employee { get; set; } = null!;

    public virtual LeaveType LeaveType { get; set; } = null!;
}
