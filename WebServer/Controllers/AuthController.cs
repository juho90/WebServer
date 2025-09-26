using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebServer.Services;

namespace WebServer.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController(JwtService jwtService) : ControllerBase
    {
        private readonly JwtService jwtService = jwtService;

        [HttpPost("test-login")]
        [AllowAnonymous]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.UID))
            {
                return BadRequest("UID은 필수입니다.");
            }
            var token = jwtService.CreateToken(request.UID);
            return Ok(new
            {
                accessToken = token
            });
        }
    }

    public class LoginRequest
    {
        public string UID { get; set; } = string.Empty;
    }
}