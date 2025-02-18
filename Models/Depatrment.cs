using System;
using System.Collections.Generic;

namespace EMS_Project.Models;

public partial class Depatrment
{
    public int Id { get; set; }

    public string DepartmentName { get; set; } = null!;

    public string? Description { get; set; }

    public int? ManagerId { get; set; }

    public DateOnly? CreatedDate { get; set; }

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
