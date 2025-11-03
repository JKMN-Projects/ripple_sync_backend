using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RippleSync.API.Common.Extensions;
using RippleSync.Application.Posts;

namespace RippleSync.API.Dashboard;

[Route("api/[controller]")]
[Authorize]
[ApiController]
public class DashboardController(
    PostManager postManager) : ControllerBase
{
    [HttpGet("total")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetTotal([FromQuery] string period = "alltime")
    {
        Guid userId = User.GetUserId();
        DateTime? fromDate = period switch
        {
            "alltime" => null,
            _ => null
        };
        var stats = await postManager.GetPostStatForPeriodAsync(userId, fromDate);
        return Ok(stats);
    }
}
