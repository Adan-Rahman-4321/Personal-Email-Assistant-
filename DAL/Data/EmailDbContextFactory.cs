using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace DAL.Data;

/// <summary>
/// Design-time factory for creating DbContext instances during migrations
/// This allows EF Core tools to find the DbContext when running migrations
/// </summary>
public class EmailDbContextFactory : IDesignTimeDbContextFactory<EmailDbContext>
{
    public EmailDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<EmailDbContext>();
        
        // Try to get connection string from configuration file
        // When running migrations, look for appsettings.json in the EmailAssistant project
        var basePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "EmailAssistant"));
        
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=(localdb)\\mssqllocaldb;Database=EmailAssistantDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=true";

        optionsBuilder.UseSqlServer(connectionString, b => b.MigrationsAssembly("DAL"));

        return new EmailDbContext(optionsBuilder.Options);
    }
}

