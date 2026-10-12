using AutoMapper;
using FluentValidation;
using MateHR.Application.Users.DTOs;
using MateHR.Application.Users.Interfaces;
using MateHR.Domain.Users.Interfaces;

namespace MateHR.Application.Users.Queries
{
    public class GetUsers : IGetUsers
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly IValidator<GetUsersRequest> _validator;

        public GetUsers(IUserRepository userRepository, IMapper mapper, IValidator<GetUsersRequest> validator)
        {
            _userRepository = userRepository;
            _mapper = mapper;
            _validator = validator;
        }

        public async Task<IReadOnlyList<UserResponse>> ExecuteAsync(
            GetUsersRequest request,
            CancellationToken cancellationToken = default)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var users = await _userRepository.GetUsersAsync(request.TenantId, request.Role, cancellationToken);

            return _mapper.Map<List<UserResponse>>(users);
        }
    }
}
