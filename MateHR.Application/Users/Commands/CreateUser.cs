using AutoMapper;
using FluentValidation;
using MateHR.Application.Common.Interfaces;
using MateHR.Application.Users.DTOs;
using MateHR.Application.Users.Interfaces;
using MateHR.Domain.Exceptions;
using MateHR.Domain.Tenants.Interfaces;
using MateHR.Domain.Users.Entities;
using MateHR.Domain.Users.Interfaces;

namespace MateHR.Application.Users.Commands
{
    public class CreateUser : ICreateUser
    {
        private readonly IUserRepository _userRepository;
        private readonly ITenantRepository _tenantRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IUserSecurityStampValidator _stampValidator;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateUserRequest> _validator;

        public CreateUser(
            IUserRepository userRepository,
            ITenantRepository tenantRepository,
            IPasswordHasher passwordHasher,
            IUserSecurityStampValidator stampValidator,
            IMapper mapper,
            IValidator<CreateUserRequest> validator)
        {
            _userRepository = userRepository;
            _tenantRepository = tenantRepository;
            _passwordHasher = passwordHasher;
            _stampValidator = stampValidator;
            _mapper = mapper;
            _validator = validator;
        }

        public async Task<UserResponse> ExecuteAsync(
            CreateUserRequest request,
            CancellationToken cancellationToken = default)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);

            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var email = request.Email.Trim().ToLowerInvariant();

            if (await _userRepository.ExistsByEmailAsync(email, cancellationToken))
            {
                throw new EmailAlreadyExistsException(email);
            }

            if (request.TenantId.HasValue)
            {
                await _tenantRepository.GetTenantByIdAsync(request.TenantId.Value, cancellationToken);
            }

            var user = new User(
                request.FirstName,
                request.LastName,
                email,
                _passwordHasher.Hash(request.Password),
                request.Role,
                request.TenantId);

            var created = await _userRepository.CreateUserAsync(user, cancellationToken);

            // Por si el mismo email se acaba de crear en otra request concurrente.
            _stampValidator.Invalidate(created.Id);

            return _mapper.Map<UserResponse>(created);
        }
    }
}