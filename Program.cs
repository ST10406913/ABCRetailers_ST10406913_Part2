using ABCRetailers.Services.Implementations;
using ABCRetailers.Services.Interfaces;
using ABCRetailers.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure Azure Storage Settings
builder.Services.Configure<AzureStorageSettings>(builder.Configuration.GetSection("AzureStorage"));

// Register Services with Dependency Injection
builder.Services.AddScoped(typeof(ITableStorageService<>), typeof(TableStorageService<>));
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
builder.Services.AddScoped<IQueueStorageService, QueueStorageService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();

// Add logging
builder.Services.AddLogging();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Only use HTTPS redirection in production
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();