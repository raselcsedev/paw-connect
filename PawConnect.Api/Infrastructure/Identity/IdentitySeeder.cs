using Microsoft.AspNetCore.Identity;
using PawConnect.Api.Domain;
using PawConnect.Api.Domain.Entities;
using PawConnect.Api.Domain.Enums;


namespace PawConnect.Api.Infrastructure.Identity;

public static class IdentitySeeder
{
    public static async Task SeedRolesAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();

        foreach (var role in Roles.All)
        {
            if (await roleManager.RoleExistsAsync(role))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(new ApplicationRole { Name = role });

            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Could not create role '{role}': {errors}");
            }
        }
    }

    public static async Task SeedAdminAsync(IServiceProvider services, IConfiguration configuration)
    {
        var email = configuration["Seed:AdminEmail"];
        var password = configuration["Seed:AdminPassword"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var users = services.GetRequiredService<UserManager<ApplicationUser>>();

        if (await users.FindByEmailAsync(email) is not null)
        {
            return;
        }

        var admin = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = "Administrator",
            Status = AccountStatus.Active
        };

        var created = await users.CreateAsync(admin, password);
        if (!created.Succeeded)
        {
            var errors = string.Join("; ", created.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Could not create the seed admin: {errors}");
        }

        await users.AddToRoleAsync(admin, Roles.Admin);
    }

        public static async Task<Guid?> SeedDemoRescuerAsync(IServiceProvider services, IConfiguration configuration)
    {
        const string email = "demo.rescuer@example.com";
        var password = configuration["Seed:DemoRescuerPassword"];

        var users = services.GetRequiredService<UserManager<ApplicationUser>>();

        var existing = await users.FindByEmailAsync(email);
        if (existing is not null)
        {
            return existing.Id;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        var rescuer = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = "Demo Rescue Shelter",
            Status = AccountStatus.Active
        };

        var created = await users.CreateAsync(rescuer, password);
        if (!created.Succeeded)
        {
            var errors = string.Join("; ", created.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Could not create the demo rescuer: {errors}");
        }

        await users.AddToRoleAsync(rescuer, Roles.Rescuer);
        return rescuer.Id;
    }
}