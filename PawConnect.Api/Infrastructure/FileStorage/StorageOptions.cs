namespace PawConnect.Api.Infrastructure.FileStorage;

public class StorageOptions
{
    public const string SectionName = "Storage";

    public string RootPath { get; set; } = string.Empty;
}