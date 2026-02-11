using Microsoft.AspNetCore.Mvc;

namespace Onyx.IdP.Web.Features.Error;

public class ErrorController : Controller
{
    [Route("Error/{statusCode}")]
    public IActionResult Index(int statusCode)
    {
        if (statusCode == 404)
        {
            return View("NotFound");
        }

        return View("Error");
    }
}
