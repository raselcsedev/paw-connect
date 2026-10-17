namespace PawConnect.Api.Infrastructure.Identity;

public static class Policies
{
    public const string ManagePets = "ManagePets";
    public const string ModeratePets = "ModeratePets";
    public const string ApplyForPets = "ApplyForPets";
    public const string AdminOnly = "AdminOnly";
    public const string AuthRateLimit = "auth";
    public const string SubmitRateLimit = "submit";
}