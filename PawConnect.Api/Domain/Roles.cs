namespace PawConnect.Api.Domain;

public static class Roles
{
    public const string Adopter = "Adopter";
    public const string Rescuer = "Rescuer";
    public const string Admin = "Admin";

    public static readonly string[] All = { Adopter, Rescuer, Admin };
}