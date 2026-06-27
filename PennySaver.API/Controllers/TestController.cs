namespace PennySaver.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    [HttpGet]
    public IActionResult GetSecureData()
    {
        return Ok(new { Message = "If you can see this, your token worked!" });
    }
}