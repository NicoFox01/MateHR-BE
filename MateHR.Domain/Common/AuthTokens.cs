namespace MateHR.Domain.Common
{
    public sealed record AuthTokens(IssuedToken AccessToken, IssuedToken RefreshToken);
}