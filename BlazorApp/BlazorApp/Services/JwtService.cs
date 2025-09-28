using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BlazorApp.Services
{
    public class JwtService
    {
        private readonly JwtSettings settings;
        private readonly JwtSecurityTokenHandler tokenHandler;
        private readonly SymmetricSecurityKey key;
        private readonly SigningCredentials creds;

        public JwtService(IOptions<JwtSettings> options)
        {
            settings = options.Value;
            tokenHandler = new JwtSecurityTokenHandler();
            key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key));
            creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        }

        public string CreateToken(string username)
        {
            var jwt = new JwtSecurityToken(
                issuer: settings.Issuer,
                audience: settings.Audience,
                claims: [
                    new Claim(ClaimTypes.Name, username),
                    new Claim(ClaimTypes.Role, "User")
                ],
                expires: DateTime.UtcNow.AddMinutes(settings.ExpiryMinutes),
                signingCredentials: creds
            );
            return tokenHandler.WriteToken(jwt);
        }
    }
}
