using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Onyx.IdP.Core.Entities;
using Onyx.IdP.Core.Interfaces;
using Onyx.IdP.Infrastructure.Data;
using Onyx.IdP.Infrastructure.Data.Services;
using Onyx.IdP.Infrastructure.Services;

namespace Onyx.IdP.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            options.UseOpenIddict();
        });

        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 6;
            options.SignIn.RequireConfirmedEmail = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders()
        .AddSignInManager<ApplicationSignInManager>()
        .AddClaimsPrincipalFactory<ApplicationClaimsPrincipalFactory>();

        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                       .UseDbContext<ApplicationDbContext>();
            });

        services.AddTransient<IEmailSender, MailKitEmailSender>();
        services.AddTransient<DataSeeder>();

        services.AddScoped<IDatabaseBackupService, DatabaseBackupService>();

        services.Configure<BackupSettingsOptions>(configuration.GetSection("BackupSettings"));

        services.AddHostedService<AutomatedBackupHostedService>();

        return services;
    }
}
