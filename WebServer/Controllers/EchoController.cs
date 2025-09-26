using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WebServer.Controllers
{
    [ApiController]
    [Route("api/echo")]
    [Authorize]
    public class EchoController : ControllerBase
    {
        [HttpGet("{message}")]
        public IActionResult Echo(string message)
        {
            return Ok(new
            {
                message
            });
        }

        [HttpGet("me")]
        public IActionResult GetUsername()
        {
            var uid = User.Identity?.Name;
            return Ok(new
            {
                uid
            });
        }

        [HttpGet("role")]
        public IActionResult GetRole()
        {
            var roles = User.Claims
                .Where(claim => claim.Type is ClaimTypes.Role)
                .Select(claim => claim.Value)
                .ToArray();
            return Ok(new
            {
                roles
            });
        }

        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public IActionResult OnlyAdmin()
        {
            return Ok(new
            {
                message = "Hello, Admin!"
            });
        }
    }
}
