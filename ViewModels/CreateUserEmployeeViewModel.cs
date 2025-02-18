using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace EMS_Project.ViewModels
{


    public class CreateUserEmployeeViewModel
    {
        public int Id { get; set; }

        // User Fields
        [Required]
        public string UserName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [MinLength(4, ErrorMessage = "Password must be at least 4 characters long.")]
        public string Password { get; set; }

        [NotMapped]
        [MinLength(4, ErrorMessage = "Password must be at least 4 characters long.")]
        public string? NewPassword { get; set; }


        // You might set a default role for employees, e.g. 3
        public int RolesId { get; set; }

        // This field is used to upload a profile image
        public IFormFile? ImageFile { get; set; }

        
        // ------------------------\\

        // Employee Fields
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public string PhoneNumber { get; set; }

        [Required]
        public DateTime HireDate { get; set; }

        [Required]
        public decimal Salary { get; set; }

        public string? JobTitle { get; set; }

        [Required]
        public int Department_Id { get; set; }
    }

}
