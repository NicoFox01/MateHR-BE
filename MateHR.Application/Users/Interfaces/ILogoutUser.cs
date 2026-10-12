namespace MateHR.Application.Users.Interfaces
{
    public interface ILogoutUser
    {
        Task ExecuteAsync(string refreshToken, CancellationToken cancellationToken = default);
    }
}