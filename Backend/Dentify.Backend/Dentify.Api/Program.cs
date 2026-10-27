using Dentify.Api.Strategies;
using Dentify.Infrastructure.DbContexts;
using Dentify.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// ---------- SERVICES (before Build) ----------
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<DentifyContext>(options =>
    options.UseSqlServer(
        builder.Configuration["ConnectionStrings:DentifyDBConnectionString"]));

// License key comes from user-secrets or appsettings.Development.json ("AutoMapper:LicenseKey")
builder.Services.AddAutoMapper(
    cfg => cfg.LicenseKey = builder.Configuration["AutoMapper:LicenseKey"],
    AppDomain.CurrentDomain.GetAssemblies());

// Repositories: always Scoped
builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
builder.Services.AddScoped<IInspectionRepository, InspectionRepository>();
builder.Services.AddScoped<IDamageRepository, DamageRepository>();
builder.Services.AddScoped<IInspectionPhotoRepository, InspectionPhotoRepository>();

// Azure strategies: a real implementation is only used when "Azure:UseFakeServices" is false
// and that service is configured (user-secrets). Otherwise the fake is used, so the app works without Azure.
var useFakeServices = builder.Configuration.GetValue("Azure:UseFakeServices", builder.Environment.IsDevelopment());

if (!useFakeServices && !string.IsNullOrWhiteSpace(builder.Configuration["Azure:Vision:Endpoint"]))
    builder.Services.AddSingleton<IPlateReaderService, AzurePlateReaderService>();
else
    builder.Services.AddTransient<IPlateReaderService, FakePlateReaderService>();

if (!useFakeServices && !string.IsNullOrWhiteSpace(builder.Configuration["Azure:CustomVision:PredictionEndpoint"]))
    builder.Services.AddHttpClient<IDamageDetectionService, CustomVisionDamageDetectionService>();
else
    builder.Services.AddTransient<IDamageDetectionService, FakeDamageDetectionService>();

builder.Services.AddTransient<IReportService, FakeReportService>();

builder.Services.AddSingleton<IPhotoStorageService, LocalPhotoStorageService>();

var app = builder.Build();

// ---------- PIPELINE (after Build) ----------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Serve uploaded inspection photos at /uploads/...
var uploadPath = Path.Combine(app.Environment.ContentRootPath, LocalPhotoStorageService.FolderName);
Directory.CreateDirectory(uploadPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadPath),
    RequestPath = "/" + LocalPhotoStorageService.FolderName
});

app.UseAuthorization();
app.MapControllers();

app.Run();
