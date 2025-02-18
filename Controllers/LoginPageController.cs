using EMS_Project.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EMS_Project.Controllers
{
    public class LoginPageController : Controller
    {
        private readonly EmsDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public LoginPageController(EmsDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }


        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginAsync(UserLogin userLogin, string Password)
        {
            //var auth = _context.UserLogins.Where(x => x.UserName == userLogin.UserName)
            //    .OrderByDescending(x => x.LoginTimestamp).FirstOrDefault();

            // Authentication: Find the user based on username and password
            var auth = _context.Users.SingleOrDefault(x => x.UserName == userLogin.UserName);

            bool loginSuccess = (auth != null);

            if (auth == null) // User doesn't exist
            {
                await RecordLoginAttempt(null, userLogin.UserName, false);
                ModelState.AddModelError("", "User does not exist.");
                return View();
            }



            // Check if the user is currently locked out
            if (auth.LockoutEndTime != null && auth.LockoutEndTime > DateTime.UtcNow)
            {
                ModelState.AddModelError("", $"Account locked until {auth.LockoutEndTime.Value.ToLocalTime()}.");
                return View(userLogin);
            }

            // Check if password is correct
            
            if (!BCrypt.Net.BCrypt.Verify(Password, auth.PasswordHash))
            {
                auth.FailedLoginAttempts++;
                // locking out after 3 failed attempts:
                if (auth.FailedLoginAttempts >= 3)
                {
                    // each additional failed attempt adds 5 minutes.
                    int lockoutMinutes = (int)(5 * (auth.FailedLoginAttempts - 2));
                    auth.LockoutEndTime = DateTime.UtcNow.AddMinutes(lockoutMinutes);
                }

                _context.Update(auth);
                await _context.SaveChangesAsync();
                await RecordLoginAttempt(auth.Id, auth.UserName, false);

                ModelState.AddModelError("", "Incorrect password.");
                return View();
            }

            // Reset failed attempts on successful login
            auth.FailedLoginAttempts = 0;
            auth.LockoutEndTime = null;
            
            _context.Update(auth);
            await _context.SaveChangesAsync();

            // Authentication successful, proceed to role-based redirection
            // Authorization: Check The User Role Id
            switch (auth.RolesId)
            {
                // If the user is an admin (RoleId == 1) store the username & etc... in session and redirect to the admin index page.
                case 1:
                    
                    HttpContext.Session.SetString("AdminName", auth.UserName);
                    HttpContext.Session.SetInt32("UserId", (int)auth.Id);
                    HttpContext.Session.SetString("ProfileImage", auth.ProfileImage);
                    HttpContext.Session.SetInt32("UserRole", (int)auth.RolesId); // Store the role
                    return RedirectToAction("Index", "Admin");

                // If the user is an Manager (RoleId == 2) store the etc... in session and redirect to the Manager index page.
                case 2:
                    HttpContext.Session.SetString("ManagerName", auth.UserName);
                    HttpContext.Session.SetInt32("UserId", (int)auth.Id);
                    HttpContext.Session.SetString("ProfileImage", auth.ProfileImage);
                    HttpContext.Session.SetInt32("UserRole", (int)auth.RolesId); // Store the role
                    return RedirectToAction("Index", "Manager");

                // If the user is an Employee (RoleId == 3) store the etc... in session and redirect to the Employee index page.
                case 3:
                    HttpContext.Session.SetString("EmployeeName", auth.UserName);
                    HttpContext.Session.SetInt32("UserId", (int)auth.Id);
                    HttpContext.Session.SetString("ProfileImage", auth.ProfileImage);
                    HttpContext.Session.SetInt32("UserRole", (int)auth.RolesId); // Store the role
                    return RedirectToAction("Index", "Employee");

                // Default case for any other RolesId values.
                default:
                    ModelState.AddModelError("", "Unauthorized role.");
                    return View();
            }

        }

        // Helper method to record login attempts
        private async Task RecordLoginAttempt(int? userId, string userName, bool success)
        {
            var loginAttempt = new UserLogin
            {
                UsersId = userId,
                UserName = userName,
                LoginTimestamp = DateTime.UtcNow,
                Ipaddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString(),
                BrowserInfo = Request.Headers["User-Agent"],
                LoginStatus = success
            };
            _context.Add(loginAttempt);
            await _context.SaveChangesAsync();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId.HasValue)
            {
                var user = _context.Users.Find(userId.Value);
                if (user != null)
                {

                    _context.Update(user); 
                    await _context.SaveChangesAsync();
                }
            }
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "LoginPage");
        }

    }

}
