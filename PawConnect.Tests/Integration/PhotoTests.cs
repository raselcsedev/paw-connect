using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using PawConnect.Api.Application.DTOs.Pets;
using PawConnect.Api.Application.DTOs.Photos;
using static PawConnect.Tests.Integration.TestHelpers;
using static PawConnect.Tests.Integration.Workflow;

namespace PawConnect.Tests.Integration;

public class PhotoTests(ApiFactory factory)
{
    private static MultipartFormDataContent Form(
        byte[] bytes, string fileName = "photo.png", string contentType = "image/png", string field = "file")
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(file, field, fileName);
        return form;
    }

    private async Task<HttpResponseMessage> UploadRaw(
        TestUser? user, int petId, byte[] bytes, string fileName = "photo.png", string contentType = "image/png")
    {
        using var client = factory.As(user);
        using var form = Form(bytes, fileName, contentType);
        return await client.PostAsync($"/api/pets/{petId}/photos", form, Ct);
    }

    private async Task<PetPhotoDto> Upload(TestUser user, int petId, byte[]? bytes = null)
    {
        var response = await UploadRaw(user, petId, bytes ?? ImageBytes.Png());
        await response.ShouldBe(HttpStatusCode.Created);
        return await response.ReadAsync<PetPhotoDto>();
    }

    private async Task<List<PetPhotoDto>> ListPhotos(TestUser? viewer, int petId)
    {
        using var client = factory.As(viewer);
        var response = await client.GetAsync($"/api/pets/{petId}/photos", Ct);
        await response.ShouldBe(HttpStatusCode.OK);
        return await response.ReadAsync<List<PetPhotoDto>>();
    }

    private async Task<(TestUser Owner, PetDto Pet)> OwnedDraft()
    {
        var owner = await factory.RegisterAsync("Rescuer");
        return (owner, await factory.CreateDraftPetAsync(owner));
    }

    [Theory]
    [InlineData("png", "image/png")]
    [InlineData("jpeg", "image/jpeg")]
    [InlineData("webp", "image/webp")]
    public async Task RealImageFormats_AreAccepted_AndStoredWithTheDetectedType(string kind, string contentType)
    {
        var (owner, pet) = await OwnedDraft();
        var bytes = kind switch { "png" => ImageBytes.Png(), "jpeg" => ImageBytes.Jpeg(), _ => ImageBytes.WebP() };

        var photo = await Upload(owner, pet.Id, bytes);

        Assert.Equal(contentType, photo.ContentType);
        Assert.Equal(bytes.Length, photo.SizeInBytes);
        Assert.True(photo.IsPrimary);
    }

    [Fact]
    public async Task TheFirstPhotoIsTheCover_TheSecondIsNot()
    {
        var (owner, pet) = await OwnedDraft();

        var first = await Upload(owner, pet.Id);
        var second = await Upload(owner, pet.Id);

        Assert.True(first.IsPrimary);
        Assert.False(second.IsPrimary);
    }

    [Theory]
    [InlineData("text")]
    [InlineData("svg")]
    [InlineData("html")]
    [InlineData("gif")]
    [InlineData("pdf")]
    [InlineData("tiny")]
    [InlineData("truncatedPng")]
    public async Task ThingsThatAreNotAcceptedImages_AreRefused_EvenWithAnImageFileNameAndType(string kind)
    {
        var (owner, pet) = await OwnedDraft();
        var bytes = kind switch
        {
            "text" => ImageBytes.Text("this is not an image"),
            "svg" => ImageBytes.Text("<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>"),
            "html" => ImageBytes.Text("<html><script>alert(1)</script></html>"),
            "gif" => ImageBytes.Gif(),
            "pdf" => ImageBytes.Pdf(),
            "tiny" => [0xFF],
            _ => ImageBytes.Png(4),
        };

        var response = await UploadRaw(owner, pet.Id, bytes, fileName: "holiday.png", contentType: "image/png");

        await response.ShouldBe(HttpStatusCode.BadRequest);
        Assert.True((await response.ReadValidationAsync()).HasErrorFor("File"));
        Assert.Empty(await ListPhotos(owner, pet.Id));
    }

    [Fact]
    public async Task AnEmptyFileOrAMissingFileField_IsRefused()
    {
        var (owner, pet) = await OwnedDraft();

        await (await UploadRaw(owner, pet.Id, [])).ShouldBe(HttpStatusCode.BadRequest);

        using var client = factory.As(owner);
        using var wrongField = Form(ImageBytes.Png(), field: "upload");
        await (await client.PostAsync($"/api/pets/{pet.Id}/photos", wrongField, Ct)).ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task TheFileNameAndTypeTheClientClaims_AreIgnored()
    {
        var (owner, pet) = await OwnedDraft();

        var response = await UploadRaw(owner, pet.Id, ImageBytes.Png(), fileName: "x.jpg", contentType: "image/jpeg");
        await response.ShouldBe(HttpStatusCode.Created);

        Assert.Equal("image/png", (await response.ReadAsync<PetPhotoDto>()).ContentType);
    }

    [Fact]
    public async Task APathTraversalFileName_IsHarmless_BecauseTheServerChoosesTheName()
    {
        var (owner, pet) = await OwnedDraft();

        var response = await UploadRaw(owner, pet.Id, ImageBytes.Png(), fileName: "../../evil.png");
        await response.ShouldBe(HttpStatusCode.Created);
        var photo = await response.ReadAsync<PetPhotoDto>();

        Assert.DoesNotContain("evil", photo.Url);

        using var scope = factory.Services.CreateScope();
        var stored = scope.Db().PetPhotos.Single(p => p.Id == photo.Id).StoredFileName;
        Assert.Matches(new Regex("^[a-f0-9]{32}\\.png$"), stored);
        Assert.True(File.Exists(Path.Combine(factory.StorageRoot, stored)));

        Assert.False(File.Exists(Path.GetFullPath(Path.Combine(factory.StorageRoot, "..", "..", "evil.png"))));
        Assert.All(
            Directory.GetFiles(factory.StorageRoot).Select(Path.GetFileName),
            name => Assert.Matches(new Regex("^[a-f0-9]{32}\\.(jpg|png|webp)$"), name!));
    }

    [Fact]
    public async Task TheSizeLimit_IsFiveMegabytes_Inclusive()
    {
        var (owner, pet) = await OwnedDraft();

        await (await UploadRaw(owner, pet.Id, ImageBytes.Png(5 * 1024 * 1024))).ShouldBe(HttpStatusCode.Created);

        var tooBig = await UploadRaw(owner, pet.Id, ImageBytes.Png(5 * 1024 * 1024 + 1));
        await tooBig.ShouldBe(HttpStatusCode.BadRequest);

        var problem = await tooBig.ReadValidationAsync();
        var message = problem.Errors.First(e => e.Key.Equals("File", StringComparison.OrdinalIgnoreCase)).Value[0];
        Assert.Contains("too large", message);
    }

    [Fact]
    public async Task AListing_CanHaveAtMostEightPhotos()
    {
        var (owner, pet) = await OwnedDraft();

        for (var i = 0; i < 8; i++)
        {
            await Upload(owner, pet.Id);
        }

        await (await UploadRaw(owner, pet.Id, ImageBytes.Png())).ShouldBe(HttpStatusCode.Conflict);
        Assert.Equal(8, (await ListPhotos(owner, pet.Id)).Count);
    }

    [Fact]
    public async Task OnlyTheOwnerOrAnAdmin_CanChangePhotos()
    {
        var owner = await factory.RegisterAsync("Rescuer");
        var intruder = await factory.RegisterAsync("Rescuer");
        var adopter = await factory.RegisterAsync();
        var admin = await factory.SharedUserAsync("Admin");
        var published = await factory.CreateAvailablePetAsync(owner);
        var draft = await factory.CreateDraftPetAsync(owner);

        Assert.Equal(HttpStatusCode.Unauthorized, (await UploadRaw(null, published.Id, ImageBytes.Png())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await UploadRaw(adopter, published.Id, ImageBytes.Png())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await UploadRaw(intruder, published.Id, ImageBytes.Png())).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await UploadRaw(intruder, draft.Id, ImageBytes.Png())).StatusCode);

        var byAdmin = await UploadRaw(admin, published.Id, ImageBytes.Png());
        await byAdmin.ShouldBe(HttpStatusCode.Created);

        using var adminClient = factory.As(admin);
        var photo = await byAdmin.ReadAsync<PetPhotoDto>();
        await (await adminClient.DeleteAsync($"/api/pets/{published.Id}/photos/{photo.Id}", Ct)).ShouldBe(HttpStatusCode.NoContent);

        using var intruderClient = factory.As(intruder);
        var mine = await Upload(owner, published.Id);
        await (await intruderClient.DeleteAsync($"/api/pets/{published.Id}/photos/{mine.Id}", Ct)).ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PhotosOfADraft_AreHiddenFromThePublic_UntilTheListingIsPublished()
    {
        var (owner, pet) = await OwnedDraft();
        var bytes = ImageBytes.Jpeg();
        var photo = await Upload(owner, pet.Id, bytes);

        using var anonymous = factory.CreateApiClient();
        await (await anonymous.GetAsync($"/api/pets/{pet.Id}/photos", Ct)).ShouldBe(HttpStatusCode.NotFound);
        await (await anonymous.GetAsync(photo.Url, Ct)).ShouldBe(HttpStatusCode.NotFound);

        using var ownerClient = factory.As(owner);
        await (await ownerClient.GetAsync(photo.Url, Ct)).ShouldBe(HttpStatusCode.OK);

        await factory.PublishPetAsync(owner, pet.Id);

        var file = await anonymous.GetAsync(photo.Url, Ct);
        await file.ShouldBe(HttpStatusCode.OK);
        Assert.Equal(bytes, await file.Content.ReadAsByteArrayAsync(Ct));
    }

    [Fact]
    public async Task ServedImages_CarryTheHeadersThatStopThemBeingTreatedAsAWebPage()
    {
        var (owner, pet) = await OwnedDraft();
        var photo = await Upload(owner, pet.Id);
        await factory.PublishPetAsync(owner, pet.Id);

        using var anonymous = factory.CreateApiClient();
        var file = await anonymous.GetAsync(photo.Url, Ct);
        await file.ShouldBe(HttpStatusCode.OK);

        Assert.Equal("image/png", file.Content.Headers.ContentType?.MediaType);
        Assert.Contains("nosniff", file.Headers.GetValues("X-Content-Type-Options"));
        Assert.Contains("sandbox", string.Join(";", file.Headers.GetValues("Content-Security-Policy")));
    }

    [Fact]
    public async Task APngSignatureFollowedByScript_IsAccepted_ButServedSafely_ThisIsTheDocumentedLimit()
    {
        var (owner, pet) = await OwnedDraft();
        var polyglot = ImageBytes.Png(32).Concat(ImageBytes.Text("<script>alert(1)</script>")).ToArray();
        var photo = await Upload(owner, pet.Id, polyglot);
        await factory.PublishPetAsync(owner, pet.Id);

        using var anonymous = factory.CreateApiClient();
        var file = await anonymous.GetAsync(photo.Url, Ct);

        Assert.Equal("image/png", file.Content.Headers.ContentType?.MediaType);
        Assert.Contains("nosniff", file.Headers.GetValues("X-Content-Type-Options"));
        Assert.Contains("default-src 'none'", string.Join(";", file.Headers.GetValues("Content-Security-Policy")));
    }

    [Fact]
    public async Task ChoosingACover_AndDeletingIt_KeepsExactlyOneCover_AndRemovesTheFile()
    {
        var (owner, pet) = await OwnedDraft();
        var first = await Upload(owner, pet.Id);
        var second = await Upload(owner, pet.Id);

        using var client = factory.As(owner);
        await (await client.SendJsonAsync(HttpMethod.Patch, $"/api/pets/{pet.Id}/photos/{second.Id}/primary")).ShouldBe(HttpStatusCode.NoContent);

        var photos = await ListPhotos(owner, pet.Id);
        Assert.Equal(second.Id, Assert.Single(photos, p => p.IsPrimary).Id);

        // A photo cannot be deleted through another listing's URL.
        var otherPet = await factory.CreateDraftPetAsync(owner);
        await (await client.DeleteAsync($"/api/pets/{otherPet.Id}/photos/{first.Id}", Ct)).ShouldBe(HttpStatusCode.NotFound);

        string storedName;
        using (var scope = factory.Services.CreateScope())
        {
            storedName = scope.Db().PetPhotos.Single(p => p.Id == second.Id).StoredFileName;
        }

        Assert.True(File.Exists(Path.Combine(factory.StorageRoot, storedName)));

        await (await client.DeleteAsync($"/api/pets/{pet.Id}/photos/{second.Id}", Ct)).ShouldBe(HttpStatusCode.NoContent);

        Assert.False(File.Exists(Path.Combine(factory.StorageRoot, storedName)));
        var remaining = Assert.Single(await ListPhotos(owner, pet.Id));
        Assert.Equal(first.Id, remaining.Id);
        Assert.True(remaining.IsPrimary);
    }

    [Fact]
    public async Task ArchivedListings_CannotReceiveNewPhotos()
    {
        var owner = await factory.RegisterAsync("Rescuer");
        var pet = await factory.CreateAvailablePetAsync(owner);
        await factory.ArchiveRawAsync(owner, pet.Id);

        await (await UploadRaw(owner, pet.Id, ImageBytes.Png())).ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task TheCoverPhoto_ShowsUpInSearchResults()
    {
        var owner = await factory.RegisterAsync("Rescuer");
        var draft = await factory.CreateDraftPetAsync(owner);
        var photo = await Upload(owner, draft.Id);
        await factory.PublishPetAsync(owner, draft.Id);

        var result = Assert.Single(await factory.SearchAsync(draft.Name));
        Assert.Equal(photo.Id, result.PrimaryPhotoId);
    }
}