using ABCRetailers.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Storage.Files.Shares;
using ABCRetailers.Services.Interfaces;
using ABCRetailers.Services.Implementations;

var builder = WebApplication.CreateBuilder(args);

// 1. Database Setup
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<AuthDbContext>();

builder.Services.AddControllersWithViews();

// 2. Azure Storage Configuration
// We prefer the real connection string, but fallback to Dev Storage if missing
var storageConnectionString = builder.Configuration.GetConnectionString("AzureStorage") ?? "UseDevelopmentStorage=true";

// A. Register the Azure SDK Clients (Low-level clients)
builder.Services.AddSingleton(x => new BlobServiceClient(storageConnectionString));
builder.Services.AddSingleton(x => new QueueServiceClient(storageConnectionString));
builder.Services.AddSingleton(x => new TableServiceClient(storageConnectionString));
builder.Services.AddSingleton(x => new ShareServiceClient(storageConnectionString));

// B. Register Your Custom Services (The wrappers your Controllers use)
// This specifically fixes the "Unable to resolve service for type ITableStorageService" error
builder.Services.AddScoped(typeof(ITableStorageService<>), typeof(TableStorageService<>));
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
builder.Services.AddScoped<IQueueStorageService, QueueStorageService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
// If you have these services, register them too:
builder.Services.AddScoped<IProductsService, ProductsService>();
builder.Services.AddScoped<IOrdersService, OrdersService>();

// 3. Register HttpClient (Critical for calling your Azure Functions)
builder.Services.AddHttpClient();

var app = builder.Build();

// 4. Pipeline Setup
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();