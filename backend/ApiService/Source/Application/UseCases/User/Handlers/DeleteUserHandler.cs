using CSharpFunctionalExtensions;
using Epam.ItMarathon.ApiService.Application.UseCases.User.Commands;
using Epam.ItMarathon.ApiService.Domain.Abstract;
using Epam.ItMarathon.ApiService.Domain.Shared.ValidationErrors;
using FluentValidation.Results;
using MediatR;

namespace Epam.ItMarathon.ApiService.Application.UseCases.User.Handlers
{
    /// <summary>
    /// Handler for deleting a user from a room.
    /// </summary>
    public class DeleteUserHandler(IRoomRepository roomRepository, IUserReadOnlyRepository userRepository)
        : IRequestHandler<DeleteUserCommand, IResult<ValidationResult>>
    {
        public async Task<IResult<ValidationResult>> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            var adminCode = request.AdminCode;
            var userCode = request.UserCode;

            var roomFindResult = await roomRepository.GetByUserCodeAsync(adminCode, cancellationToken);
            if (roomFindResult.IsFailure)
            {
                var error = new ValidationResult([
                    new ValidationFailure(nameof(adminCode), roomFindResult.Error.ToString())
                ]);
                return (IResult<ValidationResult>)Result.Failure<ValidationResult>(error.ToString());
            }

            var room = roomFindResult.Value;

            var adminUser = room.Users.FirstOrDefault(u => u.AuthCode == adminCode);
            if (adminUser is null)
            {
                return (IResult<ValidationResult>)Result.Failure<ValidationResult>(
                    new ValidationResult([new ValidationFailure(nameof(adminCode), "Admin not found in this room.")]).ToString()
                );
            }

            if (!adminUser.IsAdmin)
            {
                return (IResult<ValidationResult>)Result.Failure<ValidationResult>(
                    new ValidationResult([new ValidationFailure(nameof(adminCode), "Only admin can delete users.")]).ToString()
                );
            }

            var removeResult = room.RemoveUser(userCode);
            if (removeResult.IsFailure)
            {
                var error = new ValidationResult([
                    new ValidationFailure(nameof(userCode), removeResult.Error.ToString())
                ]);
                return (IResult<ValidationResult>)Result.Failure<ValidationResult>(error.ToString());
            }

            var updateResult = await roomRepository.UpdateAsync(removeResult.Value, cancellationToken);
            if (updateResult.IsFailure)
            {
                var error = new ValidationResult([
                    new ValidationFailure(nameof(room), updateResult.Error)
                ]);
                return (IResult<ValidationResult>)Result.Failure<ValidationResult>(error.ToString());
            }

            return (IResult<ValidationResult>)Result.Success<ValidationResult>(new ValidationResult());
        }
    }
}