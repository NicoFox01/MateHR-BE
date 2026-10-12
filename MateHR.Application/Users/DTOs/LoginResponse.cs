using System.Text.Json.Serialization;

namespace MateHR.Application.Users.DTOs
{
    public class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTimeOffset AccessTokenExpiresAt { get; set; }
        public UserResponse User { get; set; } = new UserResponse();

        [JsonIgnore]
        public string RefreshToken { get; set; } = string.Empty;

        [JsonIgnore]
        public DateTimeOffset RefreshTokenExpiresAt { get; set; }
    }
}