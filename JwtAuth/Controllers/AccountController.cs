using JwtAuth.Data;
using JwtAuth.Models;
using JwtAuth.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace JwtAuth.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _context;
        private readonly TokenService _tokenService;

        public AccountController(UserManager<ApplicationUser> userManager, AppDbContext context, TokenService tokenService)
        {
            this._userManager = userManager;
            this._context = context;
            this._tokenService = tokenService;
        }

        [HttpPost("/register")]
        public async Task<IActionResult> Register(RegistrationRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = new ApplicationUser
            {
                Email = request.Email,
                UserName = request.Username,
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (result.Succeeded)
            {
                return CreatedAtAction(nameof(Register), new { Name = user.UserName, Email = user.Email });
            }

            return BadRequest(result.Errors);
        }

        [HttpPost("/login")]
        public async Task<IActionResult> Login([FromBody] AuthRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByEmailAsync(request.Email!);
            if (user is null)
            {
                return BadRequest("Bad credentials. User email not found!");
            }

            var isValidPassowrd = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!isValidPassowrd)
            {
                return BadRequest("Incorrect password!");
            }

            var accessToken = _tokenService.CreateAccessToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken(user);

            return Ok(new AuthResponse
            {
                UserName = user.UserName,
                Email = user.Email,
                Token = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiryTime = DateTime.UtcNow.AddMinutes(TokenService.AccessTokenExpirationMinutes),
                ExpirationTokenExpiryTime = DateTime.UtcNow.AddDays(TokenService.RefreshTokenExpirationDays)
            });
        }

        [HttpPost("/refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] TokenModel tokenModel)
        {
            if (tokenModel is null)
            {
                return BadRequest("Invalid client request");
            }

            var (isValid, userId) = _tokenService.ValidateRefreshToken(tokenModel.RefreshToken);
            if (!isValid)
            {
                return BadRequest("Invalid Refresh token or token expired");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return BadRequest("User not found!");
            }

            var newAccessToken = _tokenService.CreateAccessToken(user);
            var newRefreshToken = _tokenService.GenerateRefreshToken(user);

            return Ok(new
            {
                accessToken = newAccessToken,
                refreshToken = newRefreshToken
            });
        }
    }
}
