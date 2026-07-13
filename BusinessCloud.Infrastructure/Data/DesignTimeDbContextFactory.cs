using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace BusinessCloud.Infrastructure.Data;

public class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var configuration = BuildConfiguration();

        var optionsBuilder = new DbContextOptionsBuilder<IdentityDbContext>();
        optionsBuilder.UseSqlServer(configuration.GetConnectionString("PaymentsConnection"));
        return new IdentityDbContext(optionsBuilder.Options);
    }

    internal static IConfiguration BuildConfiguration() =>
        new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../BusinessCloud.Api"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
}

public class BazaresDbContextFactory : IDesignTimeDbContextFactory<BazaresDbContext>
{
    public BazaresDbContext CreateDbContext(string[] args)
    {
        var configuration = IdentityDbContextFactory.BuildConfiguration();

        var connectionString = configuration.GetConnectionString("BazaresConnection")
            ?? configuration.GetConnectionString("PaymentsConnection");

        var optionsBuilder = new DbContextOptionsBuilder<BazaresDbContext>();
        optionsBuilder.UseSqlServer(connectionString);
        return new BazaresDbContext(optionsBuilder.Options, new DummyCurrentUserService());
    }
}
