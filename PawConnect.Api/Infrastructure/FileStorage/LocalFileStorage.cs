using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using PawConnect.Api.Application.Interfaces;

namespace PawConnect.Api.Infrastructure.FileStorage;

public class LocalFileStorage : IFileStorage
{
    private static readonly Regex SafeName =
        new("^[a-f0-9]{32}\\.(jpg|png|webp)\\z", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly string _root;
    private readonly ILogger<LocalFileStorage> _logger;

    public LocalFileStorage(
        IOptions<StorageOptions> options,
        IHostEnvironment environment,
        ILogger<LocalFileStorage> logger)
    {
        _logger = logger;

        var configured = options.Value.RootPath;
        var root = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(environment.ContentRootPath, "..", "storage", "pet-photos")
            : configured;

        _root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        Directory.CreateDirectory(_root);
    }

    public async Task SaveAsync(string storedFileName, Stream content, CancellationToken cancellationToken)
    {
        var path = ResolvePath(storedFileName);

        await using var target = new FileStream(
            path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);

        try
        {
            await content.CopyToAsync(target, cancellationToken);
        }
        catch
        {
            await target.DisposeAsync();
            TryDelete(path);
            throw;
        }
    }

    public Task<Stream?> OpenReadAsync(string storedFileName, CancellationToken cancellationToken)
    {
        var path = ResolvePath(storedFileName);

        if (!File.Exists(path))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(
            path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);

        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(string storedFileName, CancellationToken cancellationToken)
    {
        TryDelete(ResolvePath(storedFileName));
        return Task.CompletedTask;
    }

    private string ResolvePath(string storedFileName)
    {
        if (!SafeName.IsMatch(storedFileName))
        {
            throw new ArgumentException("Invalid stored file name.", nameof(storedFileName));
        }

        var fullPath = Path.GetFullPath(Path.Combine(_root, storedFileName));

        if (!fullPath.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Invalid storage path.", nameof(storedFileName));
        }

        return fullPath;
    }

    private void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Could not delete stored file {Path}", path);
        }
    }
}