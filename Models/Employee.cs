using System;
using System.Collections.Generic;

namespace EMS_Project.Models;

public partial class Employee
{
    public int Id { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public DateTime HireDate { get; set; }

    public decimal Salary { get; set; }

    public string? JobTitle { get; set; }

    public int UsersId { get; set; }

    public int DepartmentId { get; set; }

    public virtual Depatrment Department { get; set; } = null!;

    public virtual ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();

    public virtual User Users { get; set; } = null!;
}
