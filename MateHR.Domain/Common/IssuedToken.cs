namespace MateHR.Domain.Common
{
    public sealed record IssuedToken(string Token, DateTimeOffset ExpiresAt);
}