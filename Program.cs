using EMS_Project.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EMS_Project
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            //Register the DbContext with the DI container((Handle the Db config)
            builder.Services.AddDbContext<EmsDbContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));

                // Enable detailed errors in development
                if (builder.Environment.IsDevelopment())
                {
                        options.EnableSensitiveDataLogging();
                        options.EnableDetailedErrors();
                }
            }
            
            );

            ////Register the DbContext with the DI container((Handle the Db config)
            //builder.Services.AddDbContext<EmsDbContext>(options =>
            
            //    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));

            //);

            builder.Services.AddIdentity<IdentityUser, IdentityRole>()
             .AddEntityFrameworkStores<EmsDbContext>()
             .AddDefaultTokenProviders();

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/LoginPage/Login";
                options.AccessDeniedPath = "/Home/AccessDenied";
            });


            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(20);
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseSession();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=LoginPage}/{action=Login}/{id?}");

            app.Run();
        }
    }
}
