using CSharpFunctionalExtensions;
using Epam.ItMarathon.ApiService.Application.Models.Creation;
using FluentValidation.Results;
using MediatR;
using UserEntity = Epam.ItMarathon.ApiService.Domain.Entities.User.User;

namespace Epam.ItMarathon.ApiService.Application.UseCases.User.Commands
{
    /// <summary>
    /// Request adding and creating a User in Room.
    /// </summary>
    /// <param name="User">User <see cref="UserApplication"/> entity.</param>
    /// <param name="RoomCode">Join code of the Room.</param>
    public record CreateUserInRoomRequest(UserApplication User, string RoomCode)
        : IRequest<IResult<UserEntity, ValidationResult>>;

    /// <summary>
    /// Request to delete a user from a room.
    /// </summary>
    /// <param name="UserCode">Unique code of the user to delete.</param>
    /// <param name="AdminCode">Unique code of the admin performing the deletion.</param>
    public record DeleteUserCommand(string UserCode, string AdminCode)
        : IRequest<IResult<ValidationResult>>;
}