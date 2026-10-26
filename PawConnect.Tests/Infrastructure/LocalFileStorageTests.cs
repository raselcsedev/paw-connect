using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PawConnect.Api.Infrastructure.FileStorage;

namespace PawConnect.Tests;

public sealed class LocalFileStorageTests : IDisposable
{
    private const string Hex = "0123456789abcdef0123456789abcdef";

    private readonly string _root = Path.Combine(Path.GetTempPath(), "pawconnect-tests", Guid.NewGuid().ToString("N"));
    private readonly LocalFileStorage _storage;

    public LocalFileStorageTests()
    {
        _storage = new LocalFileStorage(
            Options.Create(new StorageOptions { RootPath = _root }),
            new FakeEnvironment(),
            NullLogger<LocalFileStorage>.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;
        private static MemoryStream Bytes(params byte[] data) => new(data);

    [Fact]
    public void Constructor_CreatesTheStorageFolder() => Assert.True(Directory.Exists(_root));

    [Fact]
    public async Task SaveThenOpen_ReturnsTheSameBytes()
    {
        var content = new byte[] { 1, 2, 3, 4, 5 };

        await _storage.SaveAsync(Hex + ".png", new MemoryStream(content), Token);

        await using var stream = await _storage.OpenReadAsync(Hex + ".png", Token);
        Assert.NotNull(stream);

        using var copy = new MemoryStream();
        await stream.CopyToAsync(copy, Token);
        Assert.Equal(content, copy.ToArray());
    }

    [Theory]
    [InlineData("../evil.png")]
    [InlineData("..\\evil.png")]
    [InlineData("/etc/passwd")]
    [InlineData("evil.png")]
    [InlineData("")]
    [InlineData(Hex + ".exe")]
    [InlineData(Hex + ".JPG")]
    [InlineData(Hex + ".jpg\n")]
    [InlineData(Hex + ".jpg ")]
    [InlineData("0123456789ABCDEF0123456789ABCDEF.jpg")]
    [InlineData("0123456789abcdef0123456789abcde.jpg")]
    [InlineData("0123456789abcdef0123456789abcdef0.jpg")]
    [InlineData("../" + Hex + ".jpg")]
    public async Task EveryOperation_RefusesNamesThatAreNotOurOwnRandomNames(string name)
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(() => _storage.SaveAsync(name, new MemoryStream([1]), Token));
        await Assert.ThrowsAnyAsync<ArgumentException>(() => _storage.OpenReadAsync(name, Token));
        await Assert.ThrowsAnyAsync<ArgumentException>(() => _storage.DeleteAsync(name, Token));

        Assert.Empty(Directory.GetFiles(_root));
    }

    [Fact]
    public async Task SaveAsync_WithTheSameNameTwice_DoesNotOverwriteTheFirstFile()
    {
        await _storage.SaveAsync(Hex + ".jpg", new MemoryStream([1, 1, 1]), Token);

        await Assert.ThrowsAnyAsync<IOException>(() => _storage.SaveAsync(Hex + ".jpg", new MemoryStream([9, 9]), Token));

        Assert.Equal([1, 1, 1], await File.ReadAllBytesAsync(Path.Combine(_root, Hex + ".jpg"), Token));
    }

    [Fact]
    public async Task SaveAsync_WhenTheUploadBreaksHalfway_LeavesNoPartialFile()
    {
        await Assert.ThrowsAnyAsync<IOException>(() => _storage.SaveAsync(Hex + ".png", new ThrowingStream(), Token));

        Assert.Empty(Directory.GetFiles(_root));
    }

    [Fact]
    public async Task OpenReadAsync_ForAMissingFile_ReturnsNull()
    {
        Assert.Null(await _storage.OpenReadAsync(Hex + ".webp", Token));
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheFile_AndIsSafeToRepeat()
    {
        await _storage.SaveAsync(Hex + ".png", new MemoryStream([1]), Token);

        await _storage.DeleteAsync(Hex + ".png", Token);
        await _storage.DeleteAsync(Hex + ".png", Token);

        Assert.Empty(Directory.GetFiles(_root));
    }
}

internal sealed class FakeEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = "Testing";
    public string ApplicationName { get; set; } = "PawConnect.Tests";
    public string ContentRootPath { get; set; } = Path.GetTempPath();
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}

/// <summary>A stream whose first read works and whose second read fails, like a dropped connection.</summary>
internal sealed class ThrowingStream : Stream
{
    private int _reads;

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush() { }

    public override int Read(byte[] buffer, int offset, int count) => ReadCore(buffer.AsSpan(offset, count));

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(ReadCore(buffer.Span));

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    private int ReadCore(Span<byte> buffer)
    {
        if (_reads++ == 0)
        {
            buffer[0] = 1;
            return 1;
        }

        throw new IOException("The connection was lost.");
    }
}