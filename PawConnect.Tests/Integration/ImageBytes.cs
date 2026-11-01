namespace PawConnect.Tests.Integration;

/// <summary>Minimal byte sequences that start like real images. The API only inspects the first bytes.</summary>
public static class ImageBytes
{
    private static byte[] Pad(string headerHex, int size)
    {
        var header = Convert.FromHexString(headerHex);
        var bytes = new byte[Math.Max(size, header.Length)];
        header.CopyTo(bytes, 0);
        return bytes;
    }

    public static byte[] Png(int size = 256) => Pad("89504E470D0A1A0A0000000D49484452", size);
    public static byte[] Jpeg(int size = 256) => Pad("FFD8FFE000104A4649460001", size);
    public static byte[] WebP(int size = 256) => Pad("52494646A438000057454250", size);
    public static byte[] Gif() => Pad("474946383961010001000000", 64);
    public static byte[] Pdf() => Pad("255044462D312E37", 64);
    public static byte[] Text(string text) => System.Text.Encoding.UTF8.GetBytes(text);
}