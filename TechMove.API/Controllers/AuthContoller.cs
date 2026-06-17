using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace TechMove.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;

        public AuthController(IConfiguration config)
        {
            _config = config;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            // Simple hardcoded check for prototype demonstration matching assignment scope
            if (request.Username == "admin" && request.Password == "password123")
            {
                // FIX: Added a fallback secret key to ensure the API never crashes with a null value
                var secretString = _config["Jwt:Key"] ?? "SuperSecretTechMoveLogisticsKey2026!";
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretString));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, request.Username),
                    new Claim(ClaimTypes.Role, "Administrator")
                };

                // FIX: Added fallback strings for Issuer and Audience parameters
                var issuerString = _config["Jwt:Issuer"] ?? "TechMoveAPI";
                var audienceString = _config["Jwt:Audience"] ?? "TechMoveMVC";

                var token = new JwtSecurityToken(
                    issuer: issuerString,
                    audience: audienceString,
                    claims: claims,
                    expires: DateTime.Now.AddHours(2),
                    signingCredentials: credentials);

                return Ok(new { Token = new JwtSecurityTokenHandler().WriteToken(token) });
            }

            return Unauthorized("Invalid credentials.");
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
