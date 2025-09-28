using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CommonLibrary.Services
{
    public class JwtValidator
    {
        private readonly JwtSettings settings;
        private readonly JwtSecurityTokenHandler tokenHandler;
        private readonly TokenValidationParameters validationParameters;

        public JwtValidator(IOptions<JwtSettings> options)
        {
            settings = options.Value;
            tokenHandler = new JwtSecurityTokenHandler();
            validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = settings.Issuer,
                ValidAudience = settings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(settings.Key)),
                ClockSkew = TimeSpan.Zero
            };
        }

        public ClaimsPrincipal? Validate(string token)
        {
            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
    }
}
