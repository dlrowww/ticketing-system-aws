using TicketingSystem.Api.DTOs.Users;

namespace TicketingSystem.Api.Validators.Users;

public interface IUserValidator
{
    CreateUserRequest ValidateAndNormalize(CreateUserRequest req);
    UpdateUserRequest ValidateAndNormalize(UpdateUserRequest req);
}
