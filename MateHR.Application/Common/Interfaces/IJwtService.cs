using MateHR.Domain.Common;

namespace MateHR.Application.Common.Interfaces
{
    public interface IJwtService
    {
        IssuedToken GenerateAccessToken(AuthenticatedUser user);
    }
}