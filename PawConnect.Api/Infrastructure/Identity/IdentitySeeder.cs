using Microsoft.AspNetCore.Identity;
using PawConnect.Api.Domain;
using PawConnect.Api.Domain.Entities;

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
}