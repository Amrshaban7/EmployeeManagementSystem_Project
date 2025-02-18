using EMS_Project.Models;
using EMS_Project.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Globalization;
using X.PagedList.Extensions;

namespace EMS_Project.Controllers
{
    public class EmployeeController : Controller
    {
        // static Random 
        private static readonly Random rand = new Random();
        private readonly EmsDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public EmployeeController(EmsDbContext context, IWebHostEnvironment webHostEnvironment)
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

            // Check if user is an Employee
            int role = (int)HttpContext.Session.GetInt32("UserRole");
            if (role != 3)
            {
                return RedirectToAction("AccessDenied", "Home");
            }


            ViewBag.EmployeeName = HttpContext.Session.GetString("EmployeeName");
            var userId = HttpContext.Session.GetInt32("UserId");
            var currEmployee = _context.Employees.FirstOrDefault(x => x.UsersId == userId);


            if (currEmployee == null)
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

            
            //Profile Image
            string profileImage = HttpContext.Session.GetString("ProfileImage");
            var userWithImage = _context.Users.FirstOrDefault(x => x.ProfileImage == profileImage);

            // Set the value in the ViewBag; for example, you might set the full URL or image path:
            ViewBag.ProfileImage = userWithImage?.ProfileImage ?? "~/images/default-profile.png";


            // Sample motivational quotes
            string[] quotes = {
                 "Believe you can and you're halfway there.",
                 "Doubt kills more dreams than failure ever will.",
                 "Happiness depends upon ourselves.",
                 "Do what you can, with what you have, where you are.",
                 "Don't let yesterday take up too much of today."
            };
            // Get a random quote using the static Random instance
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


            // Leave request in this year for this Employee (Approved, Pending,Rejected) & Total
            var leaveCounts = await _context.LeaveRequests
            .Where(x => x.StartDate.Year == DateTime.Now.Year
                && x.Employee.UsersId == userId)
            .GroupBy(x => 1) // Dummy group
            .Select(g => new
            {
                Pending = g.Count(x => x.RequestStatus == "Pending"),
                Approved = g.Count(x => x.RequestStatus == "Approved"),
                Rejected = g.Count(x => x.RequestStatus == "Rejected")
            }).FirstOrDefaultAsync();
            // Results
            ViewBag.EmployeePendingRequest = leaveCounts?.Pending ?? 0;
            ViewBag.EmployeeApprovedRequest = leaveCounts?.Approved ?? 0;
            ViewBag.EmployeeRejectedRequest = leaveCounts?.Rejected ?? 0;
            ViewBag.EmployeeTotalRequests = leaveCounts?.Pending + leaveCounts?.Approved + leaveCounts?.Rejected ?? 0;


            // Employee Details
            var employeeDetails = await _context.Employees
            .Include(x => x.Department)
            .FirstOrDefaultAsync(x => x.UsersId == userId);
            if (employeeDetails != null)
            {
                ViewBag.EmployeeFullName = employeeDetails.FirstName + " " + employeeDetails.LastName;
                ViewBag.EmployeeEmail = employeeDetails.Email;
                ViewBag.EmployeejobTilte = employeeDetails.JobTitle;
                ViewBag.EmployeePhoneNumber = employeeDetails.PhoneNumber;
                ViewBag.EmployeeSalary = employeeDetails.Salary;
                ViewBag.EmployeeHireDate = employeeDetails.HireDate.ToShortDateString();
                ViewBag.EmployeeDepartment = employeeDetails.Department.DepartmentName;
            }

            // Upcoming Approved Leave
            var upcomingApprovedLeave = _context.LeaveRequests
            .Where(lr => lr.RequestStatus == "Approved" && lr.StartDate.Day >= DateTime.Now.Day && lr.Employee.UsersId == userId)
            .OrderBy(lr => lr.StartDate)
            .FirstOrDefault();
            if (upcomingApprovedLeave != null)
            {
                // Format the start and end dates as MM/dd/yyyy
                string formattedRange = $"{upcomingApprovedLeave.StartDate.ToString("MM/dd/yyyy")} – {upcomingApprovedLeave.EndDate.ToString("MM/dd/yyyy")}";
                ViewBag.UpcomingApprovedLeave = formattedRange;
            }
            else
            {
                ViewBag.UpcomingApprovedLeave = "No upcoming approved leave.";
            }
            // <p>Upcoming Approved Leave: @ViewBag.UpcomingApprovedLeave</p>


            // Recent Activity
            var recentLeaveRequests = await _context.LeaveRequests
           .Include(lr => lr.Employee)
           .ThenInclude(e => e.Users)
           .Include(lr => lr.LeaveType)
           .Where(lr => (lr.RequestStatus == "Pending" ||
                         lr.RequestStatus == "Approved" ||
                         lr.RequestStatus == "Rejected") &&
                         lr.Employee.UsersId == userId)
           .OrderByDescending(lr => lr.ModifiedDate ?? lr.CreatedDate) // Sort by ModifiedDate, fallback to CreatedDate
           .Take(5) // Get the last 6 requests
           .Select(lr => new RecentLeaveRequestViewModel
           {
               EmployeeName = lr.Employee.FirstName + " " + lr.Employee.LastName,
               ProfileImage = lr.Employee.Users.ProfileImage,
               LeaveTypeName = lr.LeaveType.LeaveTypeName,
               RequestStatus = lr.RequestStatus,
               RequestTime = (lr.ModifiedDate ?? lr.CreatedDate).ToString("MM/dd h:mm tt", CultureInfo.InvariantCulture), // Show the most recent date
               StartDate = lr.StartDate.ToString("MM/dd/yyyy"), 
               EndDate = lr.EndDate.ToString("MM/dd/yyyy")
           })
           .AsNoTracking() // Read-only query
           .ToListAsync();
            // Optionally store in ViewBag
            ViewBag.RecentLeaveRequests = recentLeaveRequests;



            //
            // (Leave request trends for the last 6 months)
            //
            // Get today's date and subtract 6 months
            DateOnly startDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(-5));
            // Set the start to the first day of that month
            DateOnly firstOfStartMonth = new DateOnly(startDate.Year, startDate.Month, 1);
            // Query leave requests using DateOnly comparison
            var query = _context.LeaveRequests
                .Where(lr => lr.StartDate >= firstOfStartMonth && lr.Employee.UsersId == userId)
                .GroupBy(lr => new { Year = lr.StartDate.Year, Month = lr.StartDate.Month })
                .Select(g => new {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Count = g.Count()
                })
                .OrderBy(g => g.Year).ThenBy(g => g.Month)
                .ToList();
            // Prepare data arrays for the (Bar chart)
            var labels = new List<string>();
            var counts = new List<int>();
            // Loop through each month in the period, even if no records exist for that month.
            DateTime iterDate = new DateTime(startDate.Year, startDate.Month, 1);
            DateTime endDate = DateTime.Today;
            while (iterDate <= endDate)
            {
                labels.Add(iterDate.ToString("MMM yyyy", CultureInfo.InvariantCulture));
                // Try to find a group for this month
                var group = query.FirstOrDefault(q => q.Year == iterDate.Year && q.Month == iterDate.Month);
                counts.Add(group != null ? group.Count : 0);
                iterDate = iterDate.AddMonths(1);
            }
            // Pass data to the view via ViewBag or a view model
            ViewBag.Labels = JsonConvert.SerializeObject(labels);
            ViewBag.Counts = JsonConvert.SerializeObject(counts);


            // Avg Leave Duration
            var leaveRequests = _context.LeaveRequests
             .Where(x => x.RequestStatus == "Approved" && x.Employee.UsersId == userId)
             .Include(x => x.Employee)
             .Select(x => EF.Functions.DateDiffDay(x.StartDate, x.EndDate));
            if (leaveRequests.Any())  // Check if there are any leave requests
            {
                ViewBag.AvgLeaveDuration = Math.Round(leaveRequests.Average(), 1);  // Calculate average if data exists
            }
            else
            {
                ViewBag.AvgLeaveDuration = 0;  // Set a default value when no leave requests found
            }


            return View(recentLeaveRequests);
        }


        public IActionResult ManageLeaveRequests(int? pendingPage, int? processedPage)
        {
            // Check if user is logged in
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "LoginPage");
            }

            // Check if user is an Employee
            int role = (int)HttpContext.Session.GetInt32("UserRole");
            if (role != 3)
            {
                return RedirectToAction("AccessDenied", "Home");
            }


            ViewBag.EmployeeName = HttpContext.Session.GetString("EmployeeName");
            var userId = HttpContext.Session.GetInt32("UserId");
            var currEmployee = _context.Employees.FirstOrDefault(x => x.UsersId == userId);


            if (currEmployee == null)
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            //Profile Image
            string profileImage = HttpContext.Session.GetString("ProfileImage");
            var userWithImage = _context.Users.FirstOrDefault(x => x.ProfileImage == profileImage);
            // Set the value in the ViewBag; for example, you might set the full URL or image path:
            ViewBag.ProfileImage = userWithImage?.ProfileImage ?? "~/images/default-profile.png";



            // Set up pagination parameters
            int pendingPageNumber = pendingPage ?? 1;
            int processedPageNumber = processedPage ?? 1;
            int pageSize = 5; // Adjust page size as needed

            // Query pending leave requests (sort by CreatedDate ascending, for example)
            var pendingRequests = _context.LeaveRequests
                .Include(lr => lr.Employee)
                .ThenInclude(e => e.Users)
                .Include(lr => lr.LeaveType)
                .Where(lr => lr.RequestStatus == "Pending" && lr.Employee.UsersId == userId)
                .OrderBy(lr => lr.CreatedDate)
                .ToPagedList(pendingPageNumber, pageSize);

            // Query processed leave requests (approved or rejected) with sorting
            var processedRequests = _context.LeaveRequests
                .Include(lr => lr.Employee)
                .ThenInclude(e => e.Users)
                .Include(lr => lr.LeaveType)
                .Where(lr => lr.RequestStatus != "Pending" && lr.Employee.UsersId == userId)
                .OrderByDescending(lr => lr.ModifiedDate) // sort by most recent first
                .ToPagedList(processedPageNumber, pageSize);

            // Build the view model
            var model = new ManageLeaveRequestsViewModel
            {
                PendingRequests = pendingRequests,
                ProcessedRequests = processedRequests
            };

            return View(model);
        }



        // Edit
        [HttpGet]
        public IActionResult EditLeaveRequest(int id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Check if user is logged in
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "LoginPage");
            }

            // Check if user is an Employee
            int role = (int)HttpContext.Session.GetInt32("UserRole");
            if (role != 3)
            {
                return RedirectToAction("AccessDenied", "Home");
            }
            //Profile Image
            string profileImage = HttpContext.Session.GetString("ProfileImage");
            var userWithImage = _context.Users.FirstOrDefault(x => x.ProfileImage == profileImage);

            // Set the value in the ViewBag; for example, you might set the full URL or image path:
            ViewBag.ProfileImage = userWithImage?.ProfileImage ?? "~/images/default-profile.png";

            var leaveRequest = _context.LeaveRequests.FirstOrDefault(x => x.Id == id && x.RequestStatus == "Pending");
            if (leaveRequest == null)
            {
                TempData["Message"] = "Leave request not found or cannot be edited.";
                TempData["MessageType"] = "error";
                return RedirectToAction("ManageLeaveRequests");
            }

            ViewBag.LeaveTypes = new SelectList(_context.LeaveTypes, "Id", "LeaveTypeName", leaveRequest.LeaveTypeId);

            return View(leaveRequest);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLeaveRequest(LeaveRequest updatedRequest)
        {

            // Remove fields that are not being updated to bypass validation errors.
            ModelState.Remove("Employee");
            ModelState.Remove("LeaveType");
            ModelState.Remove("RequestStatus");

            if (!ModelState.IsValid)
            {
                // If the model state is invalid, repopulate any required ViewBag data and return the view
                ViewBag.LeaveTypes = new SelectList(_context.LeaveTypes, "Id", "LeaveTypeName", updatedRequest.LeaveTypeId);
                return View(updatedRequest);
            }

            // Find the existing request ensuring it's still pending
            var leaveRequest = await _context.LeaveRequests
                                    .FirstOrDefaultAsync(lr => lr.Id == updatedRequest.Id && lr.RequestStatus == "Pending");
            if (leaveRequest == null)
            {
                TempData["Message"] = "Leave request not found or cannot be edited.";
                TempData["MessageType"] = "error";
                return RedirectToAction("ManageLeaveRequests");
            }

            // Update allowed fields
            leaveRequest.StartDate = updatedRequest.StartDate;
            leaveRequest.EndDate = updatedRequest.EndDate;
            leaveRequest.LeaveTypeId = updatedRequest.LeaveTypeId;
            

            _context.Update(leaveRequest);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Leave request updated successfully.";
            TempData["MessageType"] = "success";
            return RedirectToAction("ManageLeaveRequests");
        }


        [HttpPost]
        public IActionResult DeleteLeaveRequest(int id)
        {
            var leaveRequest = _context.LeaveRequests.FirstOrDefault(x => x.Id == id && x.RequestStatus == "Pending");
            if (leaveRequest == null)
            {
                return Json(new { success = false, message = "Leave request not found or cannot be deleted." });
            }

            _context.LeaveRequests.Remove(leaveRequest);
            _context.SaveChanges();

            return Json(new { success = true, message = "Leave request deleted successfully." });
        }



        //Add
        public IActionResult AddLeaveRequest()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            // Check if user is logged in
            if (userId == null)
            {
                return RedirectToAction("Login", "LoginPage");
            }
            

            // Check if user is an Employee
            int role = (int)HttpContext.Session.GetInt32("UserRole");
            if (role != 3)
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            var employee = _context.Employees.FirstOrDefault(e => e.UsersId == userId);
            if (employee == null)
            {
                TempData["Message"] = "Employee record not found.";
                TempData["MessageType"] = "error";
                return RedirectToAction("Dashboard", "Home");
            }

            //Profile Image
            string profileImage = HttpContext.Session.GetString("ProfileImage");
            var userWithImage = _context.Users.FirstOrDefault(x => x.ProfileImage == profileImage);

            // Set the value in the ViewBag; for example, you might set the full URL or image path:
            ViewBag.ProfileImage = userWithImage?.ProfileImage ?? "~/images/default-profile.png";


            // Prepare the view model with the EmployeeId preset
            var model = new EMS_Project.ViewModels.AddLeaveRequestViewModel
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today
            };

            ViewBag.LeaveTypes = new SelectList(_context.LeaveTypes, "Id", "LeaveTypeName");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddLeaveRequest(EMS_Project.ViewModels.AddLeaveRequestViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var employee = _context.Employees.FirstOrDefault(e => e.UsersId == userId);

            if (!ModelState.IsValid)
            {
                ViewBag.LeaveTypes = new SelectList(_context.LeaveTypes, "Id", "LeaveTypeName", model.LeaveTypeId);
                return View(model);
            }

            // Validate EndDate after StartDate
            if (model.EndDate < model.StartDate)
            {
                ModelState.AddModelError("EndDate", "End Date must be after Start Date.");
                ViewBag.LeaveTypes = new SelectList(_context.LeaveTypes, "Id", "LeaveTypeName", model.LeaveTypeId);
                return View(model);
            }

            // Create a new LeaveRequest entity based on the view model.
            var leaveRequest = new LeaveRequest
            {
                EmployeeId = employee.Id,
                StartDate = DateOnly.FromDateTime(model.StartDate),
                EndDate = DateOnly.FromDateTime(model.EndDate),
                LeaveTypeId = model.LeaveTypeId,
                RequestStatus = "Pending",   // Default status is pending.
                CreatedDate = DateTime.Now,
                ModifiedDate = null
            };

            _context.LeaveRequests.Add(leaveRequest);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Leave request submitted successfully.";
            TempData["MessageType"] = "success";
            return RedirectToAction("ManageLeaveRequests");
        }

    }
}
