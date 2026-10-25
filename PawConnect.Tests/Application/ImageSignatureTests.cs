using System.Text;
using PawConnect.Api.Application.Services;

namespace PawConnect.Tests;

public class ImageSignatureTests
{
    [Theory]
    [InlineData("FFD8FFE000104A4649460001", "image/jpeg", ".jpg")]
    [InlineData("FFD8FFDB00430003020203", "image/jpeg", ".jpg")]
    [InlineData("89504E470D0A1A0A0000000D", "image/png", ".png")]
    [InlineData("52494646A438000057454250", "image/webp", ".webp")]
    public void Detect_RealImageSignatures_AreRecognised(string hex, string contentType, string extension)
    {
        var detected = ImageSignature.Detect(Convert.FromHexString(hex));

        Assert.NotNull(detected);
        Assert.Equal(contentType, detected.ContentType);
        Assert.Equal(extension, detected.Extension);
    }

    [Theory]
    [InlineData("")]                          // empty
    [InlineData("FFD8")]                      // too short to be a JPEG
    [InlineData("89504E470D0A1A")]            // truncated PNG signature
    [InlineData("89504E470D0A1A0B0000000D")]  // PNG signature with one wrong byte
    [InlineData("474946383961010001000000")]  // GIF
    [InlineData("255044462D312E37")]          // PDF
    [InlineData("52494646A438000057415645")]  // RIFF container, but WAVE not WEBP
    [InlineData("52494646A4380000574542")]    // WEBP cut one byte short
    public void Detect_ThingsThatAreNotAcceptedImages_AreRejected(string hex)
    {
        Assert.Null(ImageSignature.Detect(Convert.FromHexString(hex)));
    }

    [Theory]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>")]
    [InlineData("<?xml version=\"1.0\"?><svg></svg>")]
    [InlineData("<html><script>alert(1)</script></html>")]
    [InlineData("this is not an image")]
    public void Detect_TextDisguisedAsAnImage_IsRejected(string text)
    {
        Assert.Null(ImageSignature.Detect(Encoding.UTF8.GetBytes(text)));
    }

    [Fact]
    public void Detect_ARealSignatureFollowedByScript_StillLooksLikeAPng_ThisIsADocumentedLimit()
    {
        // The check only proves the file STARTS like a PNG. That is why served images carry
        // "nosniff" and a sandbox Content-Security-Policy (Phase 9). Re-encoding uploads would close this gap.
        var bytes = Convert.FromHexString("89504E470D0A1A0A")
            .Concat(Encoding.ASCII.GetBytes("<script>alert(1)</script>"))
            .ToArray();

        var detected = ImageSignature.Detect(bytes);

        Assert.NotNull(detected);
        Assert.Equal("image/png", detected.ContentType);
    }
}