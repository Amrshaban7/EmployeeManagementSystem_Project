namespace EMS_Project.ViewModels
{
    public class UserViewModel
    {
        public int Id { get; set; }
        public string ProfileImage { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public DateTime CreatedDate { get; set; }
        public int RolesId { get; set; }
        public string DepartmentName { get; set; }  // For non-employees, this could be "Admin" or blank
    }
}
