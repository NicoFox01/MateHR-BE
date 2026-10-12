using FluentAssertions;
using MateHR.Application.Tests.Common;
using MateHR.Application.Users.Queries;
using MateHR.Domain.Users.Entities;
using MateHR.Domain.Users.Enums;
using MateHR.Domain.Users.Interfaces;
using Moq;
using Xunit;

namespace MateHR.Application.Tests.Users.Queries
{
    public class GetCurrentUserTests
    {
        [Fact]
        public async Task ExecuteAsync_ExistingUser_ReturnsMappedResponse()
        {
            var user = new User("Ana", "Pérez", "ana@acme.com", "hash", UserRole.Recruiter, Guid.NewGuid());
            var repository = new Mock<IUserRepository>();
            repository
                .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var response = await new GetCurrentUser(repository.Object, TestMapper.Create())
                .ExecuteAsync(user.Id);

            response.Id.Should().Be(user.Id);
            response.Email.Should().Be("ana@acme.com");
            response.FirstName.Should().Be("Ana");
            response.TenantId.Should().Be(user.TenantId);
        }

        [Fact]
        public async Task ExecuteAsync_RepositoryThrows_PropagatesException()
        {
            var userId = Guid.NewGuid();
            var repository = new Mock<IUserRepository>();
            repository
                .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new KeyNotFoundException());

            Func<Task> act = () => new GetCurrentUser(repository.Object, TestMapper.Create())
                .ExecuteAsync(userId);

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }
    }
}
