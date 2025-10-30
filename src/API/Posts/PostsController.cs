using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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

    [HttpDelete("{postId:Guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeletePost([FromRoute] Guid postId)
    {
        Guid userId = User.GetUserId();

        await _postManager.DeletePostByIdOnUser(userId, postId);

        return NoContent();

    }
}

