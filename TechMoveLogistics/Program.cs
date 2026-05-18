using Microsoft.EntityFrameworkCore;
using TechMoveLogistics.Data;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<LogisticsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<LogisticsWorkflowService>();


// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<TechMoveLogistics.Data.LogisticsDbContext>();

        // Check if any clients exist in the database table
        if (!context.Clients.Any())
        {
            context.Clients.Add(new TechMoveLogistics.Models.Client
            {
                Name = "TechMove International Ltd",
                ContactDetails = "ops@techmovelogistics.com",
                Region = "Standard"
            });
            context.SaveChanges();
        }
    }
    catch (Exception ex)
    {
       
        System.Diagnostics.Debug.WriteLine($"Data Seed Warning: {ex.Message}");
    }
}
app.Run();
