using EMS_Project.Models;
using EMS_Project.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Globalization;
using X.PagedList.Extensions;

namespace EMS_Project.Controllers
{
    public class ManagerController : Controller
    {
        // static Random 
        private static readonly Random rand = new Random();
        private readonly EmsDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ManagerController(EmsDbContext context, IWebHostEnvironment webHostEnvironment)
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

            // Check if user is an Manager
            int role = (int)HttpContext.Session.GetInt32("UserRole");
            if (role != 2)
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            ViewBag.ManagerName = HttpContext.Session.GetString("ManagerName");
            var userId = HttpContext.Session.GetInt32("UserId");
            var currDepatrment = _context.Depatrments.FirstOrDefault(x => x.ManagerId == userId);
            

            if (currDepatrment == null)
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            // Create an HttpClient instance
            using (HttpClient client = new HttpClient())
            {
                // Get the location data using the ip-api.com API
                string locationUrl = "http://ip-api.com/json/";
                client.Timeout = TimeSpan.FromSeconds(30); // Added timeout
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


            
            // Leave request in this month for this department (Approved, Pending,Rejected) & Total
            var leaveCounts = await _context.LeaveRequests
            .Where(x => x.StartDate.Month == DateTime.Now.Month
                && x.StartDate.Year == DateTime.Now.Year
                && x.Employee.DepartmentId == currDepatrment.Id)
            .GroupBy(x => 1) // Dummy group
            .Select(g => new
            {
               Pending = g.Count(x => x.RequestStatus == "Pending"),
               Approved = g.Count(x => x.RequestStatus == "Approved"),
               Rejected = g.Count(x => x.RequestStatus == "Rejected")
            }).FirstOrDefaultAsync();
            // Results
            ViewBag.DepartmentPendingRequest = leaveCounts?.Pending ?? 0;
            ViewBag.DepartmentApprovedRequest = leaveCounts?.Approved ?? 0;
            ViewBag.DepartmentRejectedRequest = leaveCounts?.Rejected ?? 0;
            ViewBag.DepartmentTotalRequests = leaveCounts?.Pending + leaveCounts?.Approved + leaveCounts?.Rejected ?? 0;


            // new employess this month & total employess under manager supervision
            ViewBag.NumOfEmployeeUnderThisManager = _context.Employees.Count(x => x.DepartmentId == currDepatrment.Id) - 1;
            ViewBag.DepartmentNewEmployees = _context.Employees.Count(x => x.HireDate.Month == DateTime.Now.Month && x.DepartmentId == currDepatrment.Id);

            // Days until Department BirthDay
            if (currDepatrment.CreatedDate.HasValue)
            {
                // Get today's date as DateOnly
                DateOnly today = DateOnly.FromDateTime(DateTime.Today);

                // Extract the CreatedDate value
                DateOnly createdDate = currDepatrment.CreatedDate.Value;

                // Create the anniversary date for the current year using the month and day from createdDate
                DateOnly currentYearAnniversary = new DateOnly(today.Year, createdDate.Month, createdDate.Day);

                // If the anniversary has passed, use next year
                if (currentYearAnniversary < today)
                {
                    currentYearAnniversary = currentYearAnniversary.AddYears(1);
                }

                // Convert both DateOnly values to DateTime (using midnight as the time) and subtract
                int daysUntilAnniversary = (currentYearAnniversary.ToDateTime(TimeOnly.MinValue) -
                                             today.ToDateTime(TimeOnly.MinValue)).Days;

                // Prepare a message
                string birthdayMessage = daysUntilAnniversary == 0
                    ? "Happy Department Birthday!"
                    : $"{daysUntilAnniversary}";

                ViewBag.DepartmentBirthdayMessage = birthdayMessage;
            }
            else
            {
                ViewBag.DepartmentBirthdayMessage = "Department creation date is not set.";
            }
            // sum of all salaries 
            ViewBag.DepartmentTotalSalaries = _context.Employees.Where(x => x.DepartmentId == currDepatrment.Id).Sum(x => x.Salary);

            // Number of Leaves for each leave type in department this month (chart)





             var leaveTypeCounts = await _context.LeaveRequests
            .Where(x => x.StartDate.Month == DateTime.Now.Month
                        && x.StartDate.Year == DateTime.Now.Year
                        && x.Employee.DepartmentId == currDepatrment.Id)
            .GroupBy(x => x.LeaveTypeId)
            .Select(g => new
            {
                LeaveTypeId = g.Key,
                Count = g.Count()
            }).ToListAsync();

            ViewBag.DepSickLeaveRequest = leaveTypeCounts.FirstOrDefault(x => x.LeaveTypeId == 1)?.Count ?? 0;
            ViewBag.DepAnnualLeaveRequest = leaveTypeCounts.FirstOrDefault(x => x.LeaveTypeId == 2)?.Count ?? 0;
            ViewBag.DepUnpaidLeaveRequest = leaveTypeCounts.FirstOrDefault(x => x.LeaveTypeId == 3)?.Count ?? 0;
            ViewBag.DepReligiousLeaveRequest = leaveTypeCounts.FirstOrDefault(x => x.LeaveTypeId == 4)?.Count ?? 0;
            ViewBag.DepMarriageLeaveRequest = leaveTypeCounts.FirstOrDefault(x => x.LeaveTypeId == 5)?.Count ?? 0;
            ViewBag.DepPublicHolidayLeaveRequest = leaveTypeCounts.FirstOrDefault(x => x.LeaveTypeId == 6)?.Count ?? 0;


            // Recent Activity
            var recentLeaveRequests = await _context.LeaveRequests
           .Include(lr => lr.Employee)
           .ThenInclude(e => e.Users)
           .Include(lr => lr.LeaveType)
           .Where(lr => (lr.RequestStatus == "Pending" ||
                         lr.RequestStatus == "Approved" ||
                         lr.RequestStatus == "Rejected") &&
                         lr.Employee.DepartmentId == currDepatrment.Id)
           .OrderByDescending(lr => lr.ModifiedDate ?? lr.CreatedDate) // Sort by ModifiedDate, fallback to CreatedDate
           .Take(6) // Get the last 6 requests
           .Select(lr => new RecentLeaveRequestViewModel
           {
               EmployeeName = lr.Employee.FirstName + " " + lr.Employee.LastName,
               ProfileImage = lr.Employee.Users.ProfileImage,
               LeaveTypeName = lr.LeaveType.LeaveTypeName,
               RequestStatus = lr.RequestStatus,
               RequestTime = (lr.ModifiedDate ?? lr.CreatedDate).ToString("MM/dd h:mm tt", CultureInfo.InvariantCulture) // Show the most recent date
           })
           .AsNoTracking() // Read-only query
           .ToListAsync();
            // Optionally store in ViewBag
            ViewBag.RecentLeaveRequests = recentLeaveRequests;


            //
            // (Leave request trends for the last 6 months) (Bar Chart)
            //
            DateOnly startDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(-5));
            // Set the start to the first day of that month
            DateOnly firstOfStartMonth = new DateOnly(startDate.Year, startDate.Month, 1);
            // Query leave requests using DateOnly comparison
            var query = _context.LeaveRequests
                .Where(lr => lr.StartDate >= firstOfStartMonth && lr.Employee.DepartmentId == currDepatrment.Id)
                .GroupBy(lr => new { Year = lr.StartDate.Year, Month = lr.StartDate.Month })
                .Select(g => new {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Count = g.Count()
                })
                .OrderBy(g => g.Year).ThenBy(g => g.Month)
                .ToList();
            // Prepare data arrays for the chart
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

            // (Rate)
            // Calculate Pending,approval,Rejected rate as a percentage (rounding if needed)
            double PendingRate = ViewBag.DepartmentTotalRequests > 0
               ? Math.Round((double)ViewBag.DepartmentPendingRequest / ViewBag.DepartmentTotalRequests * 100, 2)
               : 0;
            double approvalRate = ViewBag.DepartmentTotalRequests > 0
                ? Math.Round((double)ViewBag.DepartmentApprovedRequest / ViewBag.DepartmentTotalRequests * 100, 2)
               : 0;
            double RejectionRate = ViewBag.DepartmentTotalRequests > 0
               ? Math.Round((double)ViewBag.DepartmentRejectedRequest / ViewBag.DepartmentTotalRequests * 100, 2)
               : 0;
            // Pass the approval rate to the view (e.g., via ViewBag or a view model)
            ViewBag.ApprovalRate  = approvalRate;
            ViewBag.RejectionRate = RejectionRate;
            ViewBag.PendingRate = PendingRate;
            //
            var leaveRequests = _context.LeaveRequests
             .Where(x => x.RequestStatus == "Approved"
                         && x.StartDate.Month == DateTime.Now.Month
                         && x.EndDate.Year == DateTime.Now.Year
                         && x.Employee.DepartmentId == currDepatrment.Id)
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


            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;
            // Total employees in the department
            var totalEmployees = _context.Employees.Count(e => e.DepartmentId == currDepatrment.Id);
            // Engaged employees (submitted leave requests, logged in, etc.)
            var engagedEmployees = _context.Employees
                .Where(e => e.DepartmentId == currDepatrment.Id && e.LeaveRequests.Any(lr => lr.StartDate.Month == currentMonth && lr.StartDate.Year == currentYear)).Count();
            // Calculate engagement percentage
            ViewBag.EngagementRate = totalEmployees > 0 ? (engagedEmployees * 100) / totalEmployees : 0;

            if (ViewBag.EngagementRate >= 75)
            {
                ViewBag.DynamicIcon = "bi bi-people-fill";
            }
            else if (ViewBag.EngagementRate >= 50)
            {
                ViewBag.DynamicIcon = "bi bi-person-lines-fill";
            }
            else
            {
                ViewBag.DynamicIcon = "bi bi-exclamation-circle";
            }

            return View(recentLeaveRequests);
        }



        public IActionResult ManageEmployees(int? page)
        {
            // Check if user is logged in
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "LoginPage");
            }

            // Check if user is an Manager
            int role = (int)HttpContext.Session.GetInt32("UserRole");
            if (role != 2)
            {
                return RedirectToAction("AccessDenied", "Home");
            }
            //Profile Image
            string profileImage = HttpContext.Session.GetString("ProfileImage");
            var userWithImage = _context.Users.FirstOrDefault(x => x.ProfileImage == profileImage);
            // Set the value in the ViewBag; for example, you might set the full URL or image path:
            ViewBag.ProfileImage = userWithImage?.ProfileImage ?? "~/images/default-profile.png";

            var userId = HttpContext.Session.GetInt32("UserId");
            var currDepatrment = _context.Depatrments.FirstOrDefault(x => x.ManagerId == userId);
            if (currDepatrment == null)
            {
                // Handle the case where the department is not found (e.g., return an error or redirect)
                return RedirectToAction("AccessDenied", "Home");
            }
            // Get the current department ID
            int currDepartmentId = currDepatrment.Id;

            //pagination 
            int pageSize = 5; // 5 records per page
            int pageNumber = page ?? 1;
            // Retrieve Employees ordered by Id
            var userEmployees = _context.Users
            .Join(_context.Employees,
                  u => u.Id,
                  e => e.UsersId,
                  (u, e) => new { User = u, Employee = e })
            .Where(joined => joined.Employee.DepartmentId == currDepartmentId) // Filter by department
            .Select(joined => new UserEmployeeViewModel
            {
                UserId = joined.User.Id,
                UserName = joined.User.UserName,
                Email = joined.User.Email,
                ProfileImage = joined.User.ProfileImage,
                RolesId = joined.User.RolesId,
                EmployeeId = joined.Employee.Id,
                FirstName = joined.Employee.FirstName,
                LastName = joined.Employee.LastName,
                PhoneNumber = joined.Employee.PhoneNumber,
                HireDate = joined.Employee.HireDate,
                Salary = joined.Employee.Salary,
                JobTitle = joined.Employee.JobTitle
            })
            .OrderBy(x => x.UserId)
            .ToPagedList(pageNumber, pageSize);

            return View(userEmployees);
        }
    
    
        public IActionResult ManageLeaveRequests(int? pendingPage, int? processedPage)
        {
            // Check if user is logged in
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "LoginPage");
            }

            // Check if user is an Manager
            int role = (int)HttpContext.Session.GetInt32("UserRole");
            if (role != 2)
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            ViewBag.ManagerName = HttpContext.Session.GetString("ManagerName");
            var userId = HttpContext.Session.GetInt32("UserId");
            var currDepatrment = _context.Depatrments.FirstOrDefault(x => x.ManagerId == userId);


            if (currDepatrment == null)
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
                .Where(lr => lr.RequestStatus == "Pending" && lr.Employee.DepartmentId == currDepatrment.Id)
                .OrderBy(lr => lr.CreatedDate)
                .ToPagedList(pendingPageNumber, pageSize);

            // Query processed leave requests (approved or rejected) with sorting
            var processedRequests = _context.LeaveRequests
                .Include(lr => lr.Employee)
                .ThenInclude(e => e.Users)
                .Include(lr => lr.LeaveType)
                .Where(lr => lr.RequestStatus != "Pending" && lr.Employee.DepartmentId == currDepatrment.Id)
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


        [HttpPost]
        public async Task<IActionResult> ApproveLeave(int id)
        {
            var leaveRequest = await _context.LeaveRequests.FindAsync(id);
            if (leaveRequest == null)
            {
                return NotFound();
            }

            leaveRequest.RequestStatus = "Approved";
            leaveRequest.ModifiedDate = DateTime.Now;

            _context.Update(leaveRequest);
            await _context.SaveChangesAsync();

            TempData["Message"] = "The leave request has been approved!";
            TempData["MessageType"] = "success"; // Can use 'success', 'error', 'info', etc.

            return RedirectToAction("ManageLeaveRequests");
        }

        [HttpPost]
        public async Task<IActionResult> RejectLeave(int id)
        {
            var leaveRequest = await _context.LeaveRequests.FindAsync(id);
            if (leaveRequest == null)
            {
                return NotFound();
            }

            leaveRequest.RequestStatus = "Rejected";
            leaveRequest.ModifiedDate = DateTime.Now;

            _context.Update(leaveRequest);
            await _context.SaveChangesAsync();

            TempData["Message"] = "The leave request has been rejected!";
            TempData["MessageType"] = "error"; // Can use 'success', 'error', 'info', etc.

            return RedirectToAction("ManageLeaveRequests");
        }

    }
}
