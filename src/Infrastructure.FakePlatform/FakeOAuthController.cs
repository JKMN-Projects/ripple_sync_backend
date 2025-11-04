
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Infrastructure.FakePlatform;

[Route("api/[controller]")]
[AllowAnonymous]
public class FakeOAuthController : ControllerBase
{

    [HttpGet]
    public IActionResult Index()
        => Ok("Fake OAuth Controller is running.");

    [HttpPost("[action]")]
    public IActionResult Token()
    {
        return Ok(new
        {
            token_type = "Fake",
            expires_in = 60 * 60 * 24 * 30, // 30 days
            access_token = "fake_access_token",
            scope = "All the scope"
        });
    }
}
