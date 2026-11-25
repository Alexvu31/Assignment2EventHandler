
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Assignment1EventHandler.Data;
using Assignment1EventHandler.Services;
using Assignment1EventHandler.Hubs;
using Azure.Storage.Blobs;

namespace Assignment1EventHandler
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // MVC and Razor Pages
            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();

            // SignalR
            builder.Services.AddSignalR();

            // Azure SQL Database
            builder.Services.AddDbContext<ApplicationDbContext>(
                options =>
                    options.UseSqlServer(
                        builder.Configuration.GetConnectionString(
                            "DefaultConnection"),
                        sqlOptions =>
                            sqlOptions.EnableRetryOnFailure(
                                maxRetryCount: 6,
                                maxRetryDelay: TimeSpan.FromSeconds(30),
                                errorNumbersToAdd: null
                            )
                    )
            );

            // ASP.NET Core Identity
            builder.Services
                .AddDefaultIdentity<ApplicationUser>(options =>
                {
                    options.SignIn.RequireConfirmedAccount = false;
                })
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();

            // Authentication cookie configuration
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Identity/Account/Login";
                options.LogoutPath = "/Identity/Account/Logout";
                options.AccessDeniedPath = "/Identity/Account/AccessDenied";

                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy =
                    CookieSecurePolicy.Always;
                options.ExpireTimeSpan = TimeSpan.FromHours(2);
                options.SlidingExpiration = true;
            });

            // Azure Blob Storage
            var blobConnectionString =
                builder.Configuration["AzureBlobStorage:ConnectionString"];

            if (string.IsNullOrWhiteSpace(blobConnectionString))
            {
                throw new InvalidOperationException(
                    "Azure Blob Storage connection string is missing.");
            }

            builder.Services.AddSingleton(
                new BlobServiceClient(blobConnectionString));

            builder.Services.AddScoped<BlobStorageService>();

            var app = builder.Build();

            // Initialize database and seed roles, users and events
            using (var scope = app.Services.CreateScope())
            {
                await DbInitializer.Initialize(scope.ServiceProvider);
            }

            // HTTP request pipeline
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();

            // Authentication MUST come before authorization
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapRazorPages();

            // SignalR Hub endpoint
            app.MapHub<EventHub>("/eventHub");

            // MVC routes
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=EventManager}/{action=Index}/{id?}");

            // Blob Storage connection test
            app.MapGet("/blob-test", async (
                BlobServiceClient blobClient,
                IConfiguration configuration) =>
            {
                try
                {
                    var containerName =
                        configuration["AzureBlobStorage:ContainerName"];

                    var container =
                        blobClient.GetBlobContainerClient(containerName);

                    await container.GetPropertiesAsync();

                    return Results.Ok(
                        "SUCCESS: Azure Blob Storage connection works!");
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        "Blob Storage test failed: " + ex.Message);
                }
            });

            app.Run();
        }
    }
}
