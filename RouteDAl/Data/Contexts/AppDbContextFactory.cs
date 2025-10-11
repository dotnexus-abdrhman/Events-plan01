using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RouteDAl.Data.Contexts;
using System.IO;

namespace EvenDAL.Data.Contexts
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            // Read connection string from the web project's appsettings.*
            var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "RourtPPl01");
            var config = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddJsonFile("appsettings.Production.json", optional: true)
                .Build();

            var cs = config.GetConnectionString("DefaultconnectionString")
                     ?? "Server=(localdb)\\MSSQLLocalDB;Database=MinaEvents;Trusted_Connection=True;TrustServerCertificate=True";

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(cs)
                .Options;

            return new AppDbContext(options);
        }
    }
}
