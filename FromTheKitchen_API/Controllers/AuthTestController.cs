using FromTheKitchen_API.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FromTheKitchen_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthTestController : Controller
    {
        [HttpGet]
        [Authorize]
        public ActionResult<string> GetSomething()
        {
            return "Authorized user";
        }

        [HttpGet("{someValue:int}")]
        [Authorize(Roles = SD.Role_Admin)]
        public ActionResult<string> GetSomething(int someValue)
        {
            return $"Authorized admin with value: {someValue}";
        }
    }
}
