using MateHR.Domain.Common;

namespace MateHR.Application.Common.Interfaces
{
    public interface IRefreshTokenService
    {
        IssuedToken Generate();

        string ComputeHash(string token);
    }
}