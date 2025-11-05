using CSharpFunctionalExtensions;
using Epam.ItMarathon.ApiService.Application.UseCases.User.Commands;
using Epam.ItMarathon.ApiService.Application.UseCases.User.Handlers;
using Epam.ItMarathon.ApiService.Domain.Abstract;
using Epam.ItMarathon.ApiService.Domain.Aggregate.Room;
using Epam.ItMarathon.ApiService.Domain.Entities.User;
using FluentAssertions;
using FluentValidation.Results;
using NSubstitute;
using Xunit;

namespace Epam.ItMarathon.ApiService.Application.Tests.UserCases.Commands
{
    public class DeleteUserHandlerTests
    {
        private readonly IRoomRepository _roomRepositoryMock;
        private readonly IUserReadOnlyRepository _userRepositoryMock;
        private readonly DeleteUserHandler _handler;

        public DeleteUserHandlerTests()
        {
            _roomRepositoryMock = Substitute.For<IRoomRepository>();
            _userRepositoryMock = Substitute.For<IUserReadOnlyRepository>();
            _handler = new DeleteUserHandler(_roomRepositoryMock, _userRepositoryMock);
        }

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenRoomNotFound()
        {
            // Arrange
            var request = new DeleteUserCommand("user123", "admin123");

            _roomRepositoryMock.GetByUserCodeAsync("admin123", Arg.Any<CancellationToken>())
                .Returns(Result.Failure<Room, ValidationResult>(
                    new ValidationResult([new ValidationFailure("Room", "Room not found")])));

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be("Room not found");
        }

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenAdminNotFoundInRoom()
        {
            // Arrange
            var room = DataFakers.ValidRoomBuilder.Build().Value;

            // Clear users so admin is missing
            typeof(Room).GetProperty("Users", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(room, new List<User>());

            _roomRepositoryMock.GetByUserCodeAsync("admin123", Arg.Any<CancellationToken>())
                .Returns(Result.Success<Room, ValidationResult>(room));

            var request = new DeleteUserCommand("user123", "admin123");

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Contain("Admin not found");
        }

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenAdminIsNotAdmin()
        {
            // Arrange
            var nonAdmin = DataFakers.ValidUserBuilder
                .WithAuthCode("admin123")
                .AsRegularUser()
                .Build();

            var room = DataFakers.ValidRoomBuilder
                .WithUsers([nonAdmin])
                .Build()
                .Value;

            _roomRepositoryMock.GetByUserCodeAsync("admin123", Arg.Any<CancellationToken>())
                .Returns(Result.Success<Room, ValidationResult>(room));

            var request = new DeleteUserCommand("user123", "admin123");

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be("Only admin can delete users.");
        }

        [Fact]
        public async Task Handle_ShouldSucceed_WhenAdminDeletesUserSuccessfully()
        {
            // Arrange
            var admin = DataFakers.ValidUserBuilder
                .WithAuthCode("admin123")
                .AsAdmin()
                .Build();

            var targetUser = DataFakers.ValidUserBuilder
                .WithAuthCode("user123")
                .AsRegularUser()
                .Build();

            var room = DataFakers.ValidRoomBuilder
                .WithUsers([admin, targetUser])
                .Build()
                .Value;

            _roomRepositoryMock.GetByUserCodeAsync("admin123", Arg.Any<CancellationToken>())
                .Returns(Result.Success<Room, ValidationResult>(room));

            _roomRepositoryMock.UpdateAsync(Arg.Any<Room>(), Arg.Any<CancellationToken>())
                .Returns(Result.Success<ValidationResult>(new ValidationResult()));

            var request = new DeleteUserCommand("user123", "admin123");

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenRepositoryUpdateFails()
        {
            // Arrange
            var admin = DataFakers.ValidUserBuilder
                .WithAuthCode("admin123")
                .AsAdmin()
                .Build();

            var targetUser = DataFakers.ValidUserBuilder
                .WithAuthCode("user123")
                .AsRegularUser()
                .Build();

            var room = DataFakers.ValidRoomBuilder
                .WithUsers([admin, targetUser])
                .Build()
                .Value;

            _roomRepositoryMock.GetByUserCodeAsync("admin123", Arg.Any<CancellationToken>())
                .Returns(Result.Success<Room, ValidationResult>(room));

            _roomRepositoryMock.UpdateAsync(Arg.Any<Room>(), Arg.Any<CancellationToken>())
                .Returns(Result.Failure<ValidationResult>("Database error"));

            var request = new DeleteUserCommand("user123", "admin123");

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be("Database error");
        }
    }
}