using System.Net;

using TicketingSystem.Api.Common;
using TicketingSystem.Api.DTOs.Users;
using TicketingSystem.Api.Enums.Identity;
using TicketingSystem.Api.Enums.Tickets;
using TicketingSystem.Api.Utils;

namespace TicketingSystem.Api.Validators.Users;

public sealed class UserValidator : IUserValidator
{
    private const int MaxNameLength = 100;
    private const int MaxEmailLength = 255;
    private const int MinPasswordLength = 8;

    public CreateUserRequest ValidateAndNormalize(CreateUserRequest req)
    {
        if (req is null)
        {
            throw new AppException(ErrorCodes.ValidationFailed, "Request is required", HttpStatusCode.BadRequest);
        }

        var name = (req.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new AppException(ErrorCodes.UserNameRequired, "Name is required", HttpStatusCode.BadRequest);
        }

        if (name.Length > MaxNameLength)
        {
            throw new AppException(ErrorCodes.UserNameTooLong, "Name is too long", HttpStatusCode.BadRequest);
        }

        var email = (req.Email ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new AppException(ErrorCodes.UserEmailRequired, "Email is required", HttpStatusCode.BadRequest);
        }

        if (email.Length > MaxEmailLength)
        {
            throw new AppException(ErrorCodes.UserEmailTooLong, "Email is too long", HttpStatusCode.BadRequest);
        }

        if (!EmailValidator.IsValid(email))
        {
            throw new AppException(ErrorCodes.UserEmailInvalid, "Email is invalid", HttpStatusCode.BadRequest);
        }

        var password = (req.Password ?? string.Empty).Trim();
        
        // Check if password is empty/whitespace
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new AppException(ErrorCodes.UserPasswordRequired, "Password is required", HttpStatusCode.BadRequest);
        }
        
        // Check minimum length
        if (password.Length < PasswordPolicy.MinLength)
        {
            throw new AppException(ErrorCodes.UserPasswordTooShort, $"Password must be at least {PasswordPolicy.MinLength} characters long", HttpStatusCode.BadRequest);
        }
        
        // Validate password against full security policy (complexity requirements)
        var passwordValidation = PasswordPolicy.Validate(password);
        if (!passwordValidation.IsValid)
        {
            var errorMessage = string.Join("; ", passwordValidation.Errors);
            throw new AppException(ErrorCodes.UserPasswordTooWeak, errorMessage, HttpStatusCode.BadRequest);
        }

        if (!Enum.IsDefined(typeof(UserRole), req.Role))
        {
            throw new AppException(ErrorCodes.UserRoleInvalid, "Role is invalid", HttpStatusCode.BadRequest);
        }

        var role = (UserRole)req.Role;

        int? categoryId = req.CategoryId;
        // Note: CategoryId validation against existing categories happens in service layer

        if ((role == UserRole.Support || role == UserRole.TeamLeader) && categoryId is null)
        {
            throw new AppException(ErrorCodes.UserCategoryRequired, "Category is required for this role", HttpStatusCode.BadRequest);
        }

        return new CreateUserRequest
        {
            Name = name,
            Email = email,
            Password = password,
            Role = req.Role,
            CategoryId = categoryId
        };
    }

    public UpdateUserRequest ValidateAndNormalize(UpdateUserRequest req)
    {
        if (req is null)
        {
            throw new AppException(ErrorCodes.ValidationFailed, "Request is required", HttpStatusCode.BadRequest);
        }

        string? name = req.Name?.Trim();
        if (req.Name is not null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new AppException(ErrorCodes.UserNameRequired, "Name is required", HttpStatusCode.BadRequest);
            }

            if (name.Length > MaxNameLength)
            {
                throw new AppException(ErrorCodes.UserNameTooLong, "Name is too long", HttpStatusCode.BadRequest);
            }
        }

        string? email = req.Email?.Trim().ToLowerInvariant();
        if (req.Email is not null)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new AppException(ErrorCodes.UserEmailRequired, "Email is required", HttpStatusCode.BadRequest);
            }

            if (email.Length > MaxEmailLength)
            {
                throw new AppException(ErrorCodes.UserEmailTooLong, "Email is too long", HttpStatusCode.BadRequest);
            }

            if (!EmailValidator.IsValid(email))
            {
                throw new AppException(ErrorCodes.UserEmailInvalid, "Email is invalid", HttpStatusCode.BadRequest);
            }
        }

        string? password = req.Password?.Trim();
        if (req.Password is not null)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new AppException(ErrorCodes.UserPasswordRequired, "Password is required", HttpStatusCode.BadRequest);
            }

            if (password.Length < MinPasswordLength)
            {
                throw new AppException(ErrorCodes.UserPasswordTooShort, "Password is too short", HttpStatusCode.BadRequest);
            }
        }

        if (req.Role is not null && !Enum.IsDefined(typeof(UserRole), req.Role.Value))
        {
            throw new AppException(ErrorCodes.UserRoleInvalid, "Role is invalid", HttpStatusCode.BadRequest);
        }

        // Note: CategoryId validation against existing categories happens in service layer

        // If role is being changed to Support/TeamLeader, enforce category presence.
        if (req.Role is not null)
        {
            var role = (UserRole)req.Role.Value;
            if ((role == UserRole.Support || role == UserRole.TeamLeader) && req.CategoryId is null)
            {
                throw new AppException(ErrorCodes.UserCategoryRequired, "Category is required for this role", HttpStatusCode.BadRequest);
            }
        }

        return new UpdateUserRequest
        {
            Name = name,
            Email = email,
            Password = password,
            Role = req.Role,
            CategoryId = req.CategoryId,
            IsActive = req.IsActive
        };
    }
}
