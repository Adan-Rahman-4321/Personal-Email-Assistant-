using Microsoft.EntityFrameworkCore;
using DAL.Data;
using DAL.Repositories;
using BLL.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Add API Controllers
builder.Services.AddControllers();

// Configure Entity Framework Core with SQL Server
builder.Services.AddDbContext<EmailDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("DAL")));

// Register Repositories
builder.Services.AddScoped<IEmailRepository, EmailRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();

// Register HTTP Client for Gemini API (must be registered before AIClassificationService)
builder.Services.AddHttpClient<BLL.Services.IGeminiAIService, BLL.Services.GeminiAIService>();
builder.Services.AddScoped<BLL.Services.IGeminiAIService>(sp => 
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient();
    var logger = sp.GetRequiredService<ILogger<BLL.Services.GeminiAIService>>();
    var configuration = sp.GetRequiredService<IConfiguration>();
    return new BLL.Services.GeminiAIService(logger, configuration, httpClient);
});

// Register Business Logic Services
builder.Services.AddScoped<IEmailFetcherService, EmailFetcherService>();
builder.Services.AddSingleton<IGmailTokenService>(sp =>
    new GmailTokenService(
        new HttpClient(),
        sp.GetRequiredService<IConfiguration>(),
        sp.GetRequiredService<ILogger<GmailTokenService>>()));
// AIClassificationService uses Native C# Engine (or Gemini AI if API key configured)
builder.Services.AddScoped<IAIClassificationService, AIClassificationService>();
builder.Services.AddScoped<IEmailProcessingService, EmailProcessingService>();

// Add CORS for API calls (if needed)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseCors();

app.UseAuthorization();

// Map Razor Pages
app.MapRazorPages();

// Map API Controllers
app.MapControllers();

// Ensure database is created and migrations are applied
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<EmailDbContext>();
        context.Database.Migrate(); // Applies migrations and ensures DB & seed data exist
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred creating the DB.");
        // Don't crash the app if DB creation fails
    }
}

app.Run();
