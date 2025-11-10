using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RippleSync.Application.Posts;

namespace Infrastructure.FakePlatform.Controllers;

[AllowAnonymous]
[Route("[controller]")]
public class FakePlatformController(
    PostManager postManager) : Controller
{
    [HttpGet]
    public IActionResult Index() =>
        // Pass the in-memory post data to the view
        View(FakePlatformInMemoryData.PostData);


    [HttpGet("image/{id}")]
    public async Task<IActionResult> GetImage(Guid id)
    {
        // Retrieve the base64 string from your database/service
        string? base64Image = await postManager.GetImageByIdAsync(id);

        if (string.IsNullOrEmpty(base64Image))
        {
            return NotFound();
        }

        // Convert the base64 string to a byte array
        byte[] imageBytes = Convert.FromBase64String(base64Image);

        // Return the image as a File result
        return File(imageBytes, "image/png"); // Adjust the content type as needed (e.g., "image/png")
    }
}
