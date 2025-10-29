using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RippleSync.API.Common.Extensions;
using RippleSync.Application.Common.Responses;
using RippleSync.Application.Posts;

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
    public async Task<IActionResult> GetPostsByUser()
    {
        Guid userId = User.GetUserId();

        var response = await _postManager.GetPostsByUserAsync(userId);

        return Ok(response);
    }
}

