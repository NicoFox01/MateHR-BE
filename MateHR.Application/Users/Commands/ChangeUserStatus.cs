using AutoMapper;
using MateHR.Application.Users.DTOs;
using MateHR.Application.Users.Interfaces;
using MateHR.Domain.Users.Interfaces;

namespace MateHR.Application.Users.Commands
{
    public class ChangeUserStatus : IChangeUserStatus
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserSecurityStampValidator _stampValidator;
        private readonly IMapper _mapper;

        public ChangeUserStatus(
            IUserRepository userRepository,
            IUserSecurityStampValidator stampValidator,
            IMapper mapper)
        {
            _userRepository = userRepository;
            _stampValidator = stampValidator;
            _mapper = mapper;
        }

        public async Task<UserResponse> ExecuteAsync(
            Guid userId,
            ChangeUserStatusDto request,
            CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            var now = DateTimeOffset.UtcNow;

            if (request.IsActive && !user.IsActive)
            {
                user.Activate(now);
            }
            else if (!request.IsActive && user.IsActive)
            {
                user.Deactivate(now);
            }

            var updated = await _userRepository.UpdateUserAsync(user, cancellationToken);

            // Invalida el cache del stamp para que el cambio de estado tenga efecto inmediato.
            _stampValidator.Invalidate(updated.Id);

            return _mapper.Map<UserResponse>(updated);
        }
    }
}
