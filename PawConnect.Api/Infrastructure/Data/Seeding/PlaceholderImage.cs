using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using PawConnect.Api.Domain.Enums;

namespace PawConnect.Api.Infrastructure.Data.Seeding;

/// <summary>Creates small, valid PNG pictures (a colour gradient) for demo listings, with no imaging library.</summary>
public static class PlaceholderImage
{
    private const int Width = 640;
    private const int Height = 480;

    private static readonly uint[] CrcTable = BuildCrcTable();

    public static byte[] For(Species species, int variant)
    {
        var (top, bottom) = species switch
        {
            Species.Dog => ((251, 191, 36), (180, 83, 9)),
            Species.Cat => ((196, 181, 253), (109, 40, 217)),
            Species.Rabbit => ((251, 207, 232), (190, 24, 93)),
            Species.Bird => ((153, 246, 228), (15, 118, 110)),
            _ => ((214, 211, 209), (87, 83, 78)),
        };

        // The second photo of a pet runs the gradient the other way round, so the two look different.
        if (variant % 2 != 0)
        {
            (top, bottom) = (bottom, top);
        }

        return Create(top, bottom);
    }

    private static byte[] Create((int R, int G, int B) top, (int R, int G, int B) bottom)
    {
        var rowLength = 1 + Width * 3;
        var raw = new byte[rowLength * Height];

        for (var y = 0; y < Height; y++)
        {
            var t = y / (double)(Height - 1);
            var r = (byte)Math.Round(top.R + (bottom.R - top.R) * t);
            var g = (byte)Math.Round(top.G + (bottom.G - top.G) * t);
            var b = (byte)Math.Round(top.B + (bottom.B - top.B) * t);

            var offset = y * rowLength;
            raw[offset] = 0; // PNG filter type "none"

            for (var x = 0; x < Width; x++)
            {
                var pixel = offset + 1 + x * 3;
                raw[pixel] = r;
                raw[pixel + 1] = g;
                raw[pixel + 2] = b;
            }
        }

        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
        {
            zlib.Write(raw);
        }

        var header = new byte[13];
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(0, 4), Width);
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(4, 4), Height);
        header[8] = 8; // bit depth
        header[9] = 2; // colour type: RGB

        using var png = new MemoryStream();
        png.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
        WriteChunk(png, "IHDR", header);
        WriteChunk(png, "IDAT", compressed.ToArray());
        WriteChunk(png, "IEND", Array.Empty<byte>());

        return png.ToArray();
    }

    private static void WriteChunk(Stream stream, string type, byte[] data)
    {
        var length = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(length, (uint)data.Length);
        stream.Write(length);

        var typeBytes = Encoding.ASCII.GetBytes(type);
        stream.Write(typeBytes);
        stream.Write(data);

        var checkedBytes = new byte[typeBytes.Length + data.Length];
        typeBytes.CopyTo(checkedBytes, 0);
        data.CopyTo(checkedBytes, typeBytes.Length);

        var crc = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crc, Crc32(checkedBytes));
        stream.Write(crc);
    }

    private static uint Crc32(byte[] data)
    {
        var crc = 0xFFFFFFFFu;

        foreach (var value in data)
        {
            crc = CrcTable[(crc ^ value) & 0xFF] ^ (crc >> 8);
        }

        return crc ^ 0xFFFFFFFFu;
    }

    private static uint[] BuildCrcTable()
    {
        var table = new uint[256];

        for (uint n = 0; n < 256; n++)
        {
            var c = n;

            for (var k = 0; k < 8; k++)
            {
                c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            }

            table[n] = c;
        }

        return table;
    }
}