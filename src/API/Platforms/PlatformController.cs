using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RippleSync.Application.Common.Responses;
using RippleSync.Application.Platforms;

namespace RippleSync.API.Platforms;
[Route("api/[controller]")]
[ApiController]
public class PlatformController(PlatformManager platformManager) : ControllerBase
{
    [HttpGet("")]
    [AllowAnonymous]
    [ProducesResponseType<ListResponse<PlatformResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllPlatformsAsync()
    {
        var response = await platformManager.GetPlatformsAsync();

        return Ok(response);
    }
}
