using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RippleSync.API.Common.Extensions;
using RippleSync.Application.Common.Responses;
using RippleSync.Application.Posts;
using System.Net.NetworkInformation;

namespace RippleSync.API.Posts;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PostsController : ControllerBase
{
    private readonly PostManager _postManager;
    public PostsController(PostManager postManager)
    {
        _postManager = postManager;
    }
    [HttpGet("byUser")]
    [ProducesResponseType<ListResponse<GetPostsByUserResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPostsByUser([FromQuery] string? status = default)
    {
        Guid userId = User.GetUserId();

        var response = await _postManager.GetPostsByUserAsync(userId, status);

        return Ok(response);
    }

    [HttpGet("{id}")]
    public IActionResult GetImage(int id)
    {
        // Retrieve the base64 string from your database/service
        string base64Image = _postManager.GetImageByIdAsync(id);

        if (string.IsNullOrEmpty(base64Image))
        {
            return NotFound();
        }

        // Convert the base64 string to a byte array
        byte[] imageBytes = Convert.FromBase64String(base64Image);

        // Return the image as a File result
        return File(imageBytes, "image/png"); // Adjust the content type as needed (e.g., "image/png")
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreatePost([FromForm] FormData request)
    {
        Guid userId = User.GetUserId();

        var mediaAttachments = await ExtractFilesToBase64(request.Files);

        await _postManager.CreatePostAsync(userId,
                request.MessageContent,
                request.Timestamp,
                mediaAttachments.Count > 0 ? [.. mediaAttachments] : null,
                [.. request.IntegrationIds]
            );

        return Created();
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdatePost([FromForm] FormData request)
    {
        var mediaAttachments = await ExtractFilesToBase64(request.Files);

        await _postManager.UpdatePostAsync(request.PostId ?? 0,
                request.MessageContent,
                request.Timestamp,
                mediaAttachments.Count > 0 ? [.. mediaAttachments] : null,
                [.. request.IntegrationIds]
            );

        return Ok();
    }

    public class FormData
    {
        public int? PostId { get; set; }

        public required string MessageContent { get; set; }

        public long? Timestamp { get; set; }

        public required List<int> IntegrationIds { get; set; }

        public List<IFormFile>? Files { get; set; }
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

    [HttpDelete("{postId:int}")]
    public async Task<IActionResult> DeletePost([FromRoute] Guid postId)
    {
        Guid userId = User.GetUserId();

        await _postManager.DeletePostByIdOnUser(userId, postId);

        return NoContent();

    }
}

