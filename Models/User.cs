using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EMS_Project.Models;

public partial class User
{
    public int Id { get; set; }

    public string UserName { get; set; } = null!;

    public string Email { get; set; } = null!;

    [NotMapped] 
    [MinLength(4, ErrorMessage = "Password must be at least 4 characters long.")]
    public string? NewPassword { get; set; }

    [MinLength(4, ErrorMessage = "Password must be at least 4 characters long.")]
    public string PasswordHash { get; set; } = null!;

    public string? ProfileImage { get; set; }
    [NotMapped] // there is no properity with this name in the database
    [ValidateNever] // Instructs ASP.NET Core’s model binding and validation system to skip validation for this property
    public virtual IFormFile ImageFile { get; set; } // Interface used to upload images

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public bool IsActive { get; set; }

    public int RolesId { get; set; }

    public int? FailedLoginAttempts { get; set; }

    public DateTime? LockoutEndTime { get; set; }

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();

    [ValidateNever]
    public virtual Role Roles { get; set; } = null!;

    public virtual ICollection<UserLogin> UserLogins { get; set; } = new List<UserLogin>();
}
