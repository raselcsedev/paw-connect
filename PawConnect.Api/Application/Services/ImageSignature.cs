namespace PawConnect.Api.Application.Services;

public record DetectedImage(string ContentType, string Extension);

public static class ImageSignature
{
    public const int HeaderLength = 12;

    private static ReadOnlySpan<byte> Png => new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    public static DetectedImage? Detect(ReadOnlySpan<byte> header)
    {
        if (header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            return new DetectedImage("image/jpeg", ".jpg");
        }

        if (header.StartsWith(Png))
        {
            return new DetectedImage("image/png", ".png");
        }

        if (header.Length >= 12 && header.StartsWith("RIFF"u8) && header.Slice(8, 4).SequenceEqual("WEBP"u8))
        {
            return new DetectedImage("image/webp", ".webp");
        }

        return null;
    }
}