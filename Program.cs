
using Microsoft.EntityFrameworkCore;
using Assignment1EventHandler.Data;
using Azure.Storage.Blobs;
using Assignment1EventHandler.Services;

namespace Assignment1EventHandler
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Register Azure SQL Database

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

            // Configure Azure Blob Storage
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

            // Initialize database and seed data
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();

                await DbInitializer.Initialize(context);
            }

            // Configure HTTP request pipeline
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=EventManager}/{action=Index}/{id?}");

            app.MapGet("/blob-test", async (
            Azure.Storage.Blobs.BlobServiceClient blobClient,
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
