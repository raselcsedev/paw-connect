using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using PawConnect.Api.Application.DTOs.Pets;
using PawConnect.Api.Controllers;
using PawConnect.Api.Infrastructure.Identity;

namespace PawConnect.Tests;

/// <summary>Reflection tests that guard design rules across the whole API.</summary>
public class ApiContractTests
{
    private const string EntityNamespace = "PawConnect.Api.Domain.Entities";
    private const string DtoNamespace = "PawConnect.Api.Application.DTOs";

    private static readonly Assembly Api = typeof(PetDto).Assembly;

    private static readonly string[] ForbiddenDtoProperties =
    [
        "PasswordHash", "SecurityStamp", "ConcurrencyStamp", "NormalizedEmail", "NormalizedUserName",
        "TokenHash", "OwnerId", "StoredFileName",
    ];

    private static IEnumerable<Type> Controllers() =>
        Api.GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.IsPublic && typeof(ControllerBase).IsAssignableFrom(t));

    private static IEnumerable<MethodInfo> Actions(Type controller) =>
        controller
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttributes<HttpMethodAttribute>(inherit: true).Any());

    private static bool DeclaresAuthorization(ICustomAttributeProvider member) =>
        member.GetCustomAttributes(inherit: true).Any(a => a is IAuthorizeData or IAllowAnonymous);

    private static IEnumerable<Type> Flatten(Type type)
    {
        if (type.HasElementType)
        {
            foreach (var inner in Flatten(type.GetElementType()!)) yield return inner;
            yield break;
        }

        var underlying = Nullable.GetUnderlyingType(type);
        if (underlying is not null)
        {
            foreach (var inner in Flatten(underlying)) yield return inner;
            yield break;
        }

        yield return type;

        if (type.IsGenericType)
        {
            foreach (var argument in type.GetGenericArguments())
            foreach (var inner in Flatten(argument))
            {
                yield return inner;
            }
        }
    }

    private static bool IsEntity(Type type) => type.Namespace == EntityNamespace;

    [Fact]
    public void ThereAreControllersToCheck()
    {
        Assert.NotEmpty(Controllers());
        Assert.NotEmpty(Controllers().SelectMany(Actions));
    }

    [Fact]
    public void EveryAction_DeclaresItsAuthorizationExplicitly()
    {
        var missing = new List<string>();

        foreach (var controller in Controllers())
        {
            var classLevel = DeclaresAuthorization(controller);

            foreach (var action in Actions(controller))
            {
                if (!classLevel && !DeclaresAuthorization(action))
                {
                    missing.Add($"{controller.Name}.{action.Name}");
                }
            }
        }

        Assert.True(
            missing.Count == 0,
            "Add [Authorize] or [AllowAnonymous] to: " + string.Join(", ", missing));
    }

    [Theory]
    [InlineData(typeof(AdminController))]
    [InlineData(typeof(AdminModerationController))]
    public void AdminControllers_AreAdminOnly_ForEveryAction(Type controller)
    {
        var authorize = controller.GetCustomAttributes<AuthorizeAttribute>(inherit: true).ToList();

        Assert.Contains(authorize, a => a.Policy == Policies.AdminOnly);
        Assert.DoesNotContain(
            Actions(controller),
            action => action.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true).Any());
    }

    [Fact]
    public void Controllers_NeverTakeOrReturnDatabaseEntities()
    {
        var offenders = new List<string>();

        foreach (var controller in Controllers())
        foreach (var action in Actions(controller))
        {
            var types = Flatten(action.ReturnType)
                .Concat(action.GetParameters().SelectMany(p => Flatten(p.ParameterType)));

            if (types.Any(IsEntity))
            {
                offenders.Add($"{controller.Name}.{action.Name}");
            }
        }

        Assert.True(offenders.Count == 0, "Entities exposed by: " + string.Join(", ", offenders));
    }

    [Fact]
    public void Dtos_NeverContainEntitiesOrSecrets()
    {
        var offenders = new List<string>();

        var dtoTypes = Api.GetTypes().Where(t =>
            t.IsPublic && !t.IsNested && t.Namespace is not null && t.Namespace.StartsWith(DtoNamespace));

        foreach (var dto in dtoTypes)
        foreach (var property in dto.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (ForbiddenDtoProperties.Contains(property.Name))
            {
                offenders.Add($"{dto.Name}.{property.Name} (forbidden name)");
            }

            if (Flatten(property.PropertyType).Any(IsEntity))
            {
                offenders.Add($"{dto.Name}.{property.Name} (entity type)");
            }
        }

        Assert.True(offenders.Count == 0, "Unsafe DTO members: " + string.Join(", ", offenders));
    }
}