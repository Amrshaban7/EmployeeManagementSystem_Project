using System.ComponentModel.DataAnnotations;

namespace EMS_Project.ViewModels
{
    public class UserEmployeeViewModel
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ProfileImage { get; set; } = string.Empty;
        public int RolesId { get; set; }

        // Employee properties (if each user has one employee, or choose representative employee)
        public int EmployeeId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}";
        public string PhoneNumber { get; set; } = string.Empty;
        [DataType(DataType.Date)]
        public DateTime HireDate { get; set; } 
        public decimal Salary { get; set; }
        public string JobTitle { get; set; } = string.Empty;

    }
}
