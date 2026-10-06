using MateHR.Application.Common.Interfaces;
using MateHR.Application.Users.Interfaces;
using MateHR.Domain.Exceptions;
using MateHR.Domain.Users.Entities;
using MateHR.Domain.Users.Enums;
using MateHR.Domain.Users.Interfaces;

namespace MateHR.Application.Users.Services
{
    public class UserProvisioningService : IUserProvisioningService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;

        public UserProvisioningService(IUserRepository userRepository, IPasswordHasher passwordHasher)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
        }

        public async Task<User> CreateAsync(
            string firstName,
            string lastName,
            string email,
            string plainTextPassword,
            UserRole role,
            Guid? tenantId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(plainTextPassword) || plainTextPassword.Length < 8)
            {
                throw new ArgumentException(
                    "La contrasena debe tener al menos 8 caracteres.",
                    nameof(plainTextPassword));
            }

            var normalized = email.Trim().ToLowerInvariant();

            if (await _userRepository.ExistsByEmailAsync(normalized, cancellationToken))
            {
                throw new EmailAlreadyExistsException(normalized);
            }

            var user = new User(
                firstName,
                lastName,
                normalized,
                _passwordHasher.Hash(plainTextPassword),
                role,
                tenantId);

            return await _userRepository.CreateUserAsync(user, cancellationToken);
        }
    }
}