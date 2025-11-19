using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ABCRetailers.Services.Implementations;
using ABCRetailers.Services.Interfaces;
using ABCRetailers.Models;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddFunctionsWorkerCore();
builder.Services.ConfigureFunctionsApplicationInsights();

// Register your services
builder.Services.AddScoped<ITableStorageService<Products>, TableStorageService<Products>>();
builder.Services.AddScoped<ITableStorageService<Orders>, TableStorageService<Orders>>();
builder.Services.AddScoped<ITableStorageService<Customers>, TableStorageService<Customers>>();
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IQueueStorageService, QueueStorageService>();
builder.Services.AddScoped<IOrdersService, OrdersService>();
builder.Services.AddScoped<IProductsService, ProductsService>();

var host = builder.Build();
host.Run();