namespace MateHR.Application.Users.Interfaces
{
    public interface IUserSecurityStampValidator
    {
        Task<bool> IsStampCurrentAsync(Guid userId, Guid stamp, CancellationToken cancellationToken = default);

        void Invalidate(Guid userId);
    }
}