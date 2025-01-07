namespace JwtAuth.Models
{
    public class AuthResponse
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;

        public DateTime AccessTokenExpiryTime { get; set; }
        public DateTime ExpirationTokenExpiryTime { get; set; }
    }
}
