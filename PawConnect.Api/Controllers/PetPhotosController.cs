using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PawConnect.Api.Application.DTOs.Photos;
using PawConnect.Api.Application.Interfaces;
using PawConnect.Api.Infrastructure.Identity;

namespace PawConnect.Api.Controllers;

[ApiController]
[Route("api/pets/{petId:int}/photos")]
public class PetPhotosController : ControllerBase
{
    private const int MaxRequestBytes = 6 * 1024 * 1024;

    private readonly IPetPhotoService _photos;

    public PetPhotosController(IPetPhotoService photos)
    {
        _photos = photos;
    }

    /// <summary>Photo metadata for a pet. Photos of drafts and archived pets are visible to the owner and admins only.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<PetPhotoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<PetPhotoDto>>> List(int petId, CancellationToken cancellationToken)
    {
        return Ok(await _photos.ListAsync(petId, User.ToCurrentUserOrNull(), cancellationToken));
    }

    /// <summary>The image bytes. Served only through this authorizing endpoint, never as a static file.</summary>
    [HttpGet("{photoId:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFile(int petId, int photoId, CancellationToken cancellationToken)
    {
        var file = await _photos.GetFileAsync(petId, photoId, User.ToCurrentUserOrNull(), cancellationToken);

        Response.Headers.XContentTypeOptions = "nosniff";
        Response.Headers.ContentSecurityPolicy = "default-src 'none'; sandbox";
        Response.Headers.CacheControl = "private, max-age=300";

        return File(file.Content, file.ContentType);
    }

    /// <summary>Upload one JPEG, PNG or WebP image (max 5 MB, 8 per listing). Send it as form field "file".</summary>
    [HttpPost]
    [Authorize(Policy = Policies.ModeratePets)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
    [ProducesResponseType(typeof(PetPhotoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    public async Task<ActionResult<PetPhotoDto>> Upload(
        int petId, IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();

        var photo = await _photos.UploadAsync(petId, stream, file.Length, User.ToCurrentUser(), cancellationToken);

        return CreatedAtAction(nameof(GetFile), new { petId, photoId = photo.Id }, photo);
    }

    [HttpPatch("{photoId:int}/primary")]
    [Authorize(Policy = Policies.ModeratePets)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetPrimary(int petId, int photoId, CancellationToken cancellationToken)
    {
        await _photos.SetPrimaryAsync(petId, photoId, User.ToCurrentUser(), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{photoId:int}")]
    [Authorize(Policy = Policies.ModeratePets)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int petId, int photoId, CancellationToken cancellationToken)
    {
        await _photos.DeleteAsync(petId, photoId, User.ToCurrentUser(), cancellationToken);
        return NoContent();
    }
}