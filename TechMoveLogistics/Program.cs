using Microsoft.EntityFrameworkCore;
using TechMoveLogistics.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register the named HttpClient pointing to your independent Service Layer API
builder.Services.AddHttpClient("TechMoveAPI", client =>
{
    // IMPORTANT: Change 7214 to the exact port number your TechMove.API project uses when running.
    client.BaseAddress = new Uri("https://localhost:7107/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Register HttpContextAccessor so controllers can access TempData or custom headers easily
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
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

app.Run();
