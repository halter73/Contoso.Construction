using Microsoft.AspNetCore.Mvc;

namespace Contoso.Construction.Controllers;
[ApiController]
[Route("[controller]")]
public class PhotosController : ControllerBase
{
    private readonly ILogger<PhotosController> _logger;

    public PhotosController(ILogger<PhotosController> logger)
    {
        _logger = logger;
    }

    //[HttpPost]
    //[Route("/upload", Name = nameof(UploadImage))]
    //public async Task UploadImage()
    //{
    //    IFormFile file = Request.Form.Files[0];
    //    using var stream = System.IO.File.OpenWrite(Path.GetFileName(file.FileName));
    //    await file.CopyToAsync(stream);
    //}
}
