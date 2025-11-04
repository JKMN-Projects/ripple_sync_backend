using Microsoft.AspNetCore.Mvc;
using Infrastructure.FakePlatform;

namespace Infrastructure.FakePlatform.Controllers;

public class FakePlatformUnauthorizedController : Controller
{
    [HttpGet]
    [Route("FakePlatform")]
    public IActionResult FakePlatform()
    {
        // Pass the in-memory post data to the view
        return View(FakePlatformInMemoryData.PostData);
    }
}
