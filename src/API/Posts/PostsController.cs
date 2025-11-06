using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RippleSync.API.Common.Extensions;
using RippleSync.Application.Common.Responses;
using RippleSync.Application.Posts;
using RippleSync.Domain.Posts.Exceptions;

namespace RippleSync.API.Posts;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public partial class PostsController(PostManager postManager) : ControllerBase
{
    [HttpGet("byUser")]
    [ProducesResponseType<ListResponse<GetPostsByUserResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPostsByUser([FromQuery] string? status = default, CancellationToken cancellationToken = default)
    {
        Guid userId = User.GetUserId();

        var response = await postManager.GetPostsByUserAsync(userId, status, cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id}")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any, NoStore = false)]
    public async Task<IActionResult> GetImage(Guid id)
    {
        // Retrieve the base64 string from your database/service
        string? base64Image = await postManager.GetImageByIdAsync(id);

        if (string.IsNullOrWhiteSpace(base64Image))
        {
            return NotFound();
        }

        byte[] imageBytes = Convert.FromBase64String(base64Image);

        return File(imageBytes, "image/png");
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreatePost([FromForm] FormData request)
    {
        Guid userId = User.GetUserId();

        var mediaAttachments = await ExtractFilesToBase64(request.Files);

        try
        {
            await postManager.CreatePostAsync(userId,
                    request.MessageContent,
                    request.Timestamp,
                    mediaAttachments.Count > 0 ? mediaAttachments.ToArray() : null,
                    request.IntegrationIds != null && request.IntegrationIds.Count > 0 ? request.IntegrationIds.ToArray() : null
                );
            return Created();
        }
        catch (ScheduledWithNoPostEventsException ex)
        {
            return Problem(
                title: "Invalid request",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest,
                extensions: new Dictionary<string, object?>
                {
                    { "validationErrors", new Dictionary<string, string[]>
                        {
                            { nameof(FormData.IntegrationIds), [ex.Message] }
                        }
                    }
                }
            );
        }
        catch (DraftWithPostEventsException ex)
        {
            return Problem(
                title: "Invalid request",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest,
                extensions: new Dictionary<string, object?>
                {
                    { "validationErrors", new Dictionary<string, string[]>
                        {
                            { nameof(FormData.IntegrationIds), [ex.Message] }
                        }
                    }
                }
            );
        }
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdatePost([FromForm] FormData request, CancellationToken cancellationToken = default)
    {
        var mediaAttachments = await ExtractFilesToBase64(request.Files);

        await postManager.UpdatePostAsync(
                User.GetUserId(),
                request.PostId ?? new Guid(),
                request.MessageContent,
                request.Timestamp,
                mediaAttachments.Count > 0 ? mediaAttachments.ToArray() : null,
                request.IntegrationIds != null && request.IntegrationIds.Count > 0 ? request.IntegrationIds.ToArray() : null,
                cancellationToken: cancellationToken
            );

        return Ok();
    }

    private static async Task<List<string>> ExtractFilesToBase64(List<IFormFile>? formFiles)
    {
        var mediaAttachments = new List<string>();

        if (formFiles != null)
        {
            foreach (var item in formFiles)
            {
                using var memoryStream = new MemoryStream();
                await item.CopyToAsync(memoryStream);
                var base64 = Convert.ToBase64String(memoryStream.ToArray());
                mediaAttachments.Add(base64);
            }
        }

        return mediaAttachments;
    }

    [HttpDelete("{postId:Guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeletePost([FromRoute] Guid postId, CancellationToken cancellationToken = default)
    {
        Guid userId = User.GetUserId();

        await postManager.DeletePostByIdAsync(userId, postId, cancellationToken);

        return NoContent();

    }
}

