using DotNetOpenAuth.InfoCard;
using JwtAuth.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace JwtAuth.Services
{
    public class TokenService
    {
        public const int AccessTokenExpirationMinutes = 60;
        public const int RefreshTokenExpirationDays = 7;
        private readonly ILogger<TokenService> _logger;
        private readonly IConfiguration _configuration;

        public TokenService(ILogger<TokenService> logger, IConfiguration configuration)
        {
            this._logger = logger;
            this._configuration = configuration;
        }

        public string CreateAccessToken(ApplicationUser user)
        {
            var expiration = DateTime.Now.AddMinutes(AccessTokenExpirationMinutes);
            var token = CreateJwtToken(
                CreateClaims(user),
                CreateSigningCredentials(),
                expiration
            );
            var tokenHandler = new JwtSecurityTokenHandler();

            _logger.LogInformation("JWT Token created");

            return tokenHandler.WriteToken(token);
        }
        public string GenerateRefreshToken(ApplicationUser user)
        {
            var timestamp = DateTimeOffset.UtcNow.AddDays(RefreshTokenExpirationDays).ToUnixTimeSeconds();
            var tokenData = $"{user.Id}:{timestamp}";
            var secret = _configuration["JwtTokenSettings:SymmetricSecurityKey"];

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(tokenData));
            var signature = Convert.ToBase64String(hash);

            var refreshToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{tokenData}:{signature}"));
            return refreshToken;
        }

        public (bool isValid, string userId) ValidateRefreshToken(string refreshToken)
        {
            try
            {
                var tokenBytes = Convert.FromBase64String(refreshToken);
                var tokenParts = Encoding.UTF8.GetString(tokenBytes).Split(':');

                if (tokenParts.Length != 3)
                    return (false, null);

                var userId = tokenParts[0];
                var timestamp = long.Parse(tokenParts[1]);
                var providedSignature = tokenParts[2];

                var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (timestamp < currentTime)
                    return (false, null);

                var tokenData = $"{userId}:{timestamp}";
                var secret = _configuration["JwtTokenSettings:SymmetricSecurityKey"];

                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(tokenData));
                var computedSignature = Convert.ToBase64String(hash);

                return (providedSignature == computedSignature, userId);
            }
            catch
            {
                return (false, null);
            }
        }

        private JwtSecurityToken CreateJwtToken(List<Claim> claims, SigningCredentials cretentials, DateTime expiration)
        {
            return new JwtSecurityToken(
                _configuration.GetValue<string>("JwtTokenSettings:ValidIssuer"),
                _configuration.GetValue<string>("JwtTokenSettings:ValidAudience"),
                claims: claims,
                expires: expiration,
                signingCredentials: cretentials);
        }

        private List<Claim> CreateClaims(ApplicationUser user)
        {
            try
            {
                var claims = new List<Claim>()
                {
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString().ToString()),
                    new Claim(JwtRegisteredClaimNames.Iat,  DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(ClaimTypes.Email, user.Email),
                };

                return claims;
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                throw;
            }
        }

        private SigningCredentials CreateSigningCredentials()
        {
            var symmetrickSecurityKey = _configuration.GetValue<string>("JwtTokenSettings:SymmetricSecurityKey");

            return new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(symmetrickSecurityKey)), SecurityAlgorithms.HmacSha256);
        }
    }
}
