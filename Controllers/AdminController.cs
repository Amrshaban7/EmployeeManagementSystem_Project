using EMS_Project.Models;
using EMS_Project.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using Newtonsoft.Json;
using System.Drawing;
using X.PagedList;
using X.PagedList.Extensions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;


namespace EMS_Project.Controllers
{
    
    public class AdminController : Controller
    {
        // static Random 
        private static readonly Random rand = new Random();
        private readonly EmsDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AdminController(EmsDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {

            // Check if user is logged in
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "LoginPage");
            }

            // Check if user is an admin
            int role = (int)HttpContext.Session.GetInt32("UserRole");
            if (role != 1)
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            // Create an HttpClient instance
            using (HttpClient client = new HttpClient())
            {
                // Get the location data using the ip-api.com API
                string locationUrl = "http://ip-api.com/json/";
                string locationResponse = await client.GetStringAsync(locationUrl);
                dynamic locationData = JsonConvert.DeserializeObject(locationResponse);
                // Extract country, city, latitude and longitude
                ViewBag.Country = locationData.country;
                ViewBag.City = locationData.city;
            }

            ViewBag.AdminName = HttpContext.Session.GetString("AdminName");
            //Profile Image
            string profileImage = HttpContext.Session.GetString("ProfileImage");
            var userWithImage = _context.Users.FirstOrDefault(x => x.ProfileImage == profileImage);

            // Set the value in the ViewBag; for example, you might set the full URL or image path:
            ViewBag.ProfileImage = userWithImage?.ProfileImage ?? "~/images/default-profile.png";


            // Motivational quotes
            string[] quotes = {
                 "Believe you can and you're halfway there.",
                 "Doubt kills more dreams than failure ever will.",
                 "Happiness depends upon ourselves.",
                 "Do what you can, with what you have, where you are.",
                 "Don't let yesterday take up too much of today."
            };
            // Random quote using the static Random instance
            int index = rand.Next(quotes.Length);
            ViewBag.Quote = quotes[index];

            int currentHour = DateTime.Now.Hour;
            // Decide the icon based on time of day (6 AM to 6 PM for day)
            if (currentHour >= 6 && currentHour < 18)
            {
                ViewBag.WeatherIconClass = "icon-sun mr-2";
            }
            else
            {
                ViewBag.WeatherIconClass = "icon-moon mr-2";
            }

            ViewBag.CurrentTime = DateTime.Now.ToString("HH:mm");

            // Leave request in this month (Approved, Pending,Rejected) & Total
            ViewBag.PendingRequest = _context.LeaveRequests.Count(x => x.RequestStatus == "Pending" && x.StartDate.Month == DateTime.Now.Month
             && x.StartDate.Year == DateTime.Now.Year);
            ViewBag.ApprovedRequest = _context.LeaveRequests.Count(x => x.RequestStatus == "Approved" && x.StartDate.Month == DateTime.Now.Month
             && x.StartDate.Year == DateTime.Now.Year);
            ViewBag.RejectedRequest = _context.LeaveRequests.Count(x => x.RequestStatus == "Rejected" && x.StartDate.Month == DateTime.Now.Month
             && x.StartDate.Year == DateTime.Now.Year);
            ViewBag.TotalRequests = ViewBag.PendingRequest + ViewBag.ApprovedRequest + ViewBag.RejectedRequest;
            // Number of users & number of lockout users
            ViewBag.Users = _context.Users.Count();
            ViewBag.LockoutUsers = _context.Users.Count(x => x.LockoutEndTime != null);
            // Number of departmens & new deapartment this year
            ViewBag.Departmens = _context.Depatrments.Count();
            ViewBag.NewDepartmens = _context.Depatrments.Count(x => x.CreatedDate.Value.Year >= DateTime.Now.Year);
            // sum of all salaries & new employess this month
            ViewBag.NewEmployees = _context.Employees.Count(x => x.HireDate.Month == DateTime.Now.Month);
            ViewBag.Salaries = _context.Employees.Sum(x => x.Salary);

            // summaries to show each department details
            var summaries = _context.Employees
            .Include(e => e.Department)
            .GroupBy(e => new { e.Department.Id, e.Department.DepartmentName })
            .Select(g => new DepartmentSummaryViewModel
            {
               DepartmentName = g.Key.DepartmentName,
               EmployeeCount = g.Count(),
               TotalSalary = g.Sum(e => e.Salary)
            }).ToList();

           return View(summaries);
        }


        public IActionResult ManageDepartments()
        {
            // Check if user is logged in
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "LoginPage");
            }

            // Check if user is an admin
            int role = (int)HttpContext.Session.GetInt32("UserRole");
            if (role != 1)
            {
                return RedirectToAction("AccessDenied", "Home");
            }
            //Profile Image
            string profileImage = HttpContext.Session.GetString("ProfileImage");
            var userWithImage = _context.Users.FirstOrDefault(x => x.ProfileImage == profileImage);

            // Set the value in the ViewBag; for example, you might set the full URL or image path:
            ViewBag.ProfileImage = userWithImage?.ProfileImage ?? "~/images/default-profile.png";



            var departments = _context.Depatrments.ToList();
            var managers = _context.Users.ToDictionary(u => u.Id, u => u.UserName);
            ViewBag.Managers = managers;

            return View(departments);
        }

        public IActionResult AddDepartments()
        {
            // Check if user is logged in
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "LoginPage");
            }

            // Check if user is an admin
            int role = (int)HttpContext.Session.GetInt32("UserRole");
            if (role != 1)
            {
                return RedirectToAction("AccessDenied", "Home");
            }
            //Profile Image
            string profileImage = HttpContext.Session.GetString("ProfileImage");
            var userWithImage = _context.Users.FirstOrDefault(x => x.ProfileImage == profileImage);

            // Set the value in the ViewBag; for example, you might set the full URL or image path:
            ViewBag.ProfileImage = userWithImage?.ProfileImage ?? "~/images/default-profile.png";

            ViewBag.Manager = new SelectList(_context.Users.Where(x => x.RolesId == 2), "Id", "UserName");

            return View();
        }

        // GET: Depatrments/Edit/5
        public async Task<IActionResult> EditDepartments(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var depatrment = await _context.Depatrments.FindAsync(id);
            if (depatrment == null)
            {
                return NotFound();
            }

            // Check if user is logged in
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "LoginPage");
            }

            // Check if user is an admin
            int role = (int)HttpContext.Session.GetInt32("UserRole");
            if (role != 1)
            {
                return RedirectToAction("AccessDenied", "Home");
            }
            //Profile Image
            string profileImage = HttpContext.Session.GetString("ProfileImage");
            var userWithImage = _context.Users.FirstOrDefault(x => x.ProfileImage == profileImage);

            // Set the value in the ViewBag; for example, you might set the full URL or image path:
            ViewBag.ProfileImage = userWithImage?.ProfileImage ?? "~/images/default-profile.png";

            ViewBag.Manager = new SelectList(_context.Users.Where(x => x.RolesId == 2), "Id", "UserName");

            return View(depatrment);
        }


        // POST: Depatrments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDepartments([Bind("Id,DepartmentName,Description,ManagerId,CreatedDate")] Depatrment depatrment)
        {
            if (ModelState.IsValid)
            {

                _context.Add(depatrment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ManageDepartments));
            }
            return View(depatrment);
        }


        // POST: Depatrments/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDepartments(int id, [Bind("Id,DepartmentName,Description,ManagerId")] Depatrment depatrment)
        {
            if (id != depatrment.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Retrieve the existing department from the database
                    var existingDept = await _context.Depatrments.FindAsync(id);
                    if (existingDept == null)
                    {
                        return NotFound();
                    }
                    // Update only the allowed fields
                    existingDept.DepartmentName = depatrment.DepartmentName;
                    existingDept.Description = depatrment.Description;
                    existingDept.ManagerId = depatrment.ManagerId;

                    //_context.Update(depatrment);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DepatrmentExists(depatrment.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(ManageDepartments));
            }
            return View(depatrment);
        }

        // POST: Depatrments/Delete/5
        [HttpPost]
        public IActionResult DeleteDepartments(int id)
        {
            var department = _context.Depatrments.Find(id);
            if (department == null)
            {
                return Json(new { success = false, message = "Department not found" });
            }

            _context.Depatrments.Remove(department);
            _context.SaveChanges();

            return Json(new { success = true, message = "Department deleted successfully" });
        }

        private bool DepatrmentExists(int id)
        {
            return _context.Depatrments.Any(e => e.Id == id);
        }
        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }

        public IActionResult ManageUsers(int? page)
        {
            // Check if user is logged in
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "LoginPage");
            }

            // Check if user is an admin
            int role = (int)HttpContext.Session.GetInt32("UserRole");
            if (role != 1)
            {
                return RedirectToAction("AccessDenied", "Home");
            }
            //Profile Image
            string profileImage = HttpContext.Session.GetString("ProfileImage");
            var userWithImage = _context.Users.FirstOrDefault(x => x.ProfileImage == profileImage);
            // Set the value in the ViewBag; for example, you might set the full URL or image path:
            ViewBag.ProfileImage = userWithImage?.ProfileImage ?? "~/images/default-profile.png";

            //pagination 
            int pageSize = 5; // 5 records per page
            int pageNumber = page ?? 1;
            // Retrieve users ordered by, for example, Id
            var users = (from u in _context.Users
                         join e in _context.Employees.Include(e => e.Department)
                             on u.Id equals e.Users.Id into empGroup
                         from emp in empGroup.DefaultIfEmpty()  // Left join: if no employee, emp is null
                         select new UserViewModel
                         {
                             Id = u.Id,
                             ProfileImage = u.ProfileImage,
                             UserName = u.UserName,
                             Email = u.Email,
                             CreatedDate = u.CreatedDate,
                             RolesId = u.RolesId,
                             DepartmentName = emp != null ? emp.Department.DepartmentName : "~" // or string.Empty
                         })
             .OrderBy(x => x.RolesId)
             .ThenBy(x => x.Id)
             .ToPagedList(pageNumber, pageSize);

            return View(users);
        }

        public IActionResult AddUsers()
        {
            // Check if user is logged in
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "LoginPage");
            }

            // Check if user is an admin
            int role = (int)HttpContext.Session.GetInt32("UserRole");
            if (role != 1)
            {
                return RedirectToAction("AccessDenied", "Home");
            }
            //Profile Image
            string profileImage = HttpContext.Session.GetString("ProfileImage");
            var userWithImage = _context.Users.FirstOrDefault(x => x.ProfileImage == profileImage);
            // Set the value in the ViewBag; for example, you might set the full URL or image path:
            ViewBag.ProfileImage = userWithImage?.ProfileImage ?? "~/images/default-profile.png";

            ViewBag.Roles = new SelectList(_context.Roles, "Id", "RoleName");
            ViewBag.Departments = new SelectList(_context.Depatrments, "Id", "DepartmentName");

            return View();
        }

        //// POST: User/Create
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> AddUsers([Bind("UserName,Email,PasswordHash,RolesId,ImageFile")] User user)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        if (user.ImageFile != null)
        //        {
        //            // Access w3root folder
        //            string wwwRootPath = _webHostEnvironment.WebRootPath; // W3root Folder
        //            string imageName = Guid.NewGuid().ToString() + "_" + user.ImageFile.FileName;

        //            string path = Path.Combine(wwwRootPath + "/images/" + imageName);

        //            using (var fileStream = new FileStream(path, FileMode.Create))
        //            {
        //                await user.ImageFile.CopyToAsync(fileStream);
        //            }
        //            // Save Image --> Uploaded by the user to DB
        //            user.ProfileImage = imageName;
                    
        //        }
        //        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
        //        _context.Add(user);
        //        await _context.SaveChangesAsync();
        //        return RedirectToAction(nameof(ManageUsers));
        //    }
            
        //    return View(user);
        //}



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddUsers(CreateUserEmployeeViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Create the User
                var user = new User
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    // Hash the password before saving
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    RolesId = model.RolesId,
                    IsActive = true
                };

                // Handle file upload if provided
                if (model.ImageFile != null)
                {
                    // Access w3root folder
                    string wwwRootPath = _webHostEnvironment.WebRootPath; // W3root Folder
                    string imageName = Guid.NewGuid().ToString() + "_" + model.ImageFile.FileName;

                    string path = Path.Combine(wwwRootPath + "/images/" + imageName);

                    using (var fileStream = new FileStream(path, FileMode.Create))
                    {
                        await model.ImageFile.CopyToAsync(fileStream);
                    }
                    // Save Image --> Uploaded by the user to DB
                    user.ProfileImage = imageName;

                }
                // Add and save the user to generate its Id
                try
                {
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    var employee = new Employee
                    {
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        Email = model.Email,
                        PhoneNumber = model.PhoneNumber,
                        Salary = model.Salary,
                        HireDate = model.HireDate,
                        JobTitle = model.JobTitle,
                        UsersId = user.Id,
                        DepartmentId = model.Department_Id
                    };

                    _context.Employees.Add(employee);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    // Log the exception message for debugging
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                    throw;  // Or handle the error appropriately
                }

                return RedirectToAction(nameof(ManageUsers));
            }

            // If model is not valid, return the view with the model for correction
            return View(model);
        }


        // GET: user/Edit/5
        public async Task<IActionResult> EditUsers(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.Include(u => u.Employees).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            // Check if user is logged in
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "LoginPage");
            }

            // Check if user is an admin
            int role = (int)HttpContext.Session.GetInt32("UserRole");
            if (role != 1)
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            // Populate ViewModel
            var model = new ViewModels.CreateUserEmployeeViewModel
            {
                // User Data
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                RolesId = user.RolesId,
                ImageFile = user.ImageFile,

                // Employee Data (if exists)
                FirstName = user.Employees.FirstOrDefault()?.FirstName,
                LastName = user.Employees.FirstOrDefault()?.LastName,
                PhoneNumber = user.Employees.FirstOrDefault()?.PhoneNumber,
                Salary = user.Employees.FirstOrDefault()?.Salary ?? 0,
                JobTitle = user.Employees.FirstOrDefault()?.JobTitle,
                Department_Id = user.Employees.FirstOrDefault()?.DepartmentId ?? 0
            };

            //Profile Image
            string profileImage = HttpContext.Session.GetString("ProfileImage");
            var userWithImage = _context.Users.FirstOrDefault(x => x.ProfileImage == profileImage);
            // Set the value in the ViewBag; for example, you might set the full URL or image path:
            ViewBag.ProfileImage = userWithImage?.ProfileImage ?? "~/images/default-profile.png";

            ViewBag.Roles = new SelectList(_context.Roles, "Id", "RoleName");
            ViewBag.Departments = new SelectList(_context.Depatrments, "Id", "DepartmentName");

            ViewBag.Image = user.ProfileImage;

            return View(model);
        }




        // POST: User/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUsers(int id, CreateUserEmployeeViewModel model)
        {
            
            if (id != model.Id)
            {
                return NotFound();
            }

            // Remove the PasswordHash error since we are not binding it on edit.
            ModelState.Remove("Password");

            if (ModelState.IsValid)
            {
                try
                {
                    // Retrieve the existing user from the database
                    var existingUser = await _context.Users.Include(u => u.Employees).FirstOrDefaultAsync(u => u.Id == id);
                    if (existingUser == null)
                    {
                        return NotFound();
                    }

                    if (model.ImageFile != null)
                    {
                        // Access w3root folder
                        string wwwRootPath = _webHostEnvironment.WebRootPath; // W3root Folder
                        string imageName = Guid.NewGuid().ToString() + "_" + model.ImageFile.FileName;

                        string path = Path.Combine(wwwRootPath + "/images/" + imageName);

                        using (var fileStream = new FileStream(path, FileMode.Create))
                        {
                            await model.ImageFile.CopyToAsync(fileStream);
                        }
                        
                        existingUser.ProfileImage = imageName;
                        

                    }


                    existingUser.UserName = model.UserName;
                    existingUser.Email = model.Email;
                    // Hash the password before saving
                    existingUser.UpdatedDate = DateTime.Now;
                    existingUser.RolesId = model.RolesId;
                    existingUser.IsActive = true;


                    // Update password only if a new password is provided
                    if (!string.IsNullOrEmpty(model.NewPassword))
                    {
                        existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                    }

                    // Update Employee fields
                    var existingEmployee = existingUser.Employees.FirstOrDefault();
                    if (existingEmployee != null)
                    {
                        existingEmployee.FirstName = model.FirstName;
                        existingEmployee.LastName = model.LastName;
                        existingEmployee.PhoneNumber = model.PhoneNumber;
                        existingEmployee.Salary = model.Salary;
                        existingEmployee.JobTitle = model.JobTitle;
                        existingEmployee.DepartmentId = model.Department_Id;

                        _context.Employees.Update(existingEmployee);
                    }

                    // Save changes
                    _context.Users.Update(existingUser);
                    await _context.SaveChangesAsync();


                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(model.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(ManageUsers));
            }
            // Repopulate ViewBags in case of validation errors
            ViewBag.Roles = new SelectList(_context.Roles, "Id", "RoleName", model.RolesId);
            ViewBag.Departments = new SelectList(_context.Depatrments, "Id", "DepartmentName", model.Department_Id);

            return View(model);
        }


        //// POST: User/Edit/5
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> EditUsers(int id, [Bind("Id,UserName,Email,NewPassword,RolesId,ImageFile")] User user)
        //{
        //    if (id != user.Id)
        //    {
        //        return NotFound();
        //    }

        //    // Remove the PasswordHash error since we are not binding it on edit.
        //    ModelState.Remove("PasswordHash");

        //    if (ModelState.IsValid)
        //    {
        //        try
        //        {
        //            // Retrieve the existing user from the database
        //            var existingUser = await _context.Users.FindAsync(id);
        //            if (existingUser == null)
        //            {
        //                return NotFound();
        //            }

        //            if (user.ImageFile != null)
        //            {
        //                // Access w3root folder
        //                string wwwRootPath = _webHostEnvironment.WebRootPath; // W3root Folder
        //                string imageName = Guid.NewGuid().ToString() + "_" + user.ImageFile.FileName;

        //                string path = Path.Combine(wwwRootPath + "/images/" + imageName);

        //                using (var fileStream = new FileStream(path, FileMode.Create))
        //                {
        //                    await user.ImageFile.CopyToAsync(fileStream);
        //                }
        //                // Save Image --> Uploaded by the user to DB
        //                //user.ProfileImage = imageName;
        //                existingUser.ProfileImage = imageName;
        //                // ViewBag.Message = category.ImageFile.FileName + " Uploaded Successfully";

        //            }

        //            // Update only the allowed fields
        //            existingUser.UserName = user.UserName;
        //            existingUser.Email = user.Email;
        //            existingUser.RolesId = user.RolesId;


        //            // Update password only if a new password is provided
        //            if (!string.IsNullOrEmpty(user.NewPassword))
        //            {
        //                existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.NewPassword);
        //            }

        //            _context.Update(existingUser);
        //            await _context.SaveChangesAsync();
        //        }
        //        catch (DbUpdateConcurrencyException)
        //        {
        //            if (!DepatrmentExists(user.Id))
        //            {
        //                return NotFound();
        //            }
        //            else
        //            {
        //                throw;
        //            }
        //        }
        //        return RedirectToAction(nameof(ManageUsers));
        //    }
        //    return View(user);
        //}


        // POST: User/Delete/5
        [HttpPost]
        public IActionResult DeleteUsers(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            _context.Users.Remove(user);
            _context.SaveChanges();

            return Json(new { success = true, message = "User deleted successfully" });
        }


    }
}
