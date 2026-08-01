using System.Net;
using TicketingSystem.Api.Common;
using TicketingSystem.Api.DTOs.Categories;

namespace TicketingSystem.Api.Validators.Categories;

public interface ICategoryValidator
{
    CreateCategoryRequest ValidateAndNormalize(CreateCategoryRequest req);
    UpdateCategoryRequest ValidateAndNormalize(UpdateCategoryRequest req);
}

public sealed class CategoryValidator : ICategoryValidator
{
    private const int MaxNameLength = 100;
    private const int MinNameLength = 2;

    public CreateCategoryRequest ValidateAndNormalize(CreateCategoryRequest req)
    {
        if (req is null)
        {
            throw new AppException(ErrorCodes.ValidationFailed, "Request is required", HttpStatusCode.BadRequest);
        }

        var namePl = (req.NamePl ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(namePl))
        {
            throw new AppException(ErrorCodes.CategoryNameRequired, "Polish name is required", HttpStatusCode.BadRequest);
        }

        if (namePl.Length < MinNameLength)
        {
            throw new AppException(ErrorCodes.CategoryNameTooShort, "Polish name is too short", HttpStatusCode.BadRequest);
        }

        if (namePl.Length > MaxNameLength)
        {
            throw new AppException(ErrorCodes.CategoryNameTooLong, "Polish name is too long", HttpStatusCode.BadRequest);
        }

        var nameEn = (req.NameEn ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(nameEn))
        {
            throw new AppException(ErrorCodes.CategoryNameRequired, "English name is required", HttpStatusCode.BadRequest);
        }

        if (nameEn.Length < MinNameLength)
        {
            throw new AppException(ErrorCodes.CategoryNameTooShort, "English name is too short", HttpStatusCode.BadRequest);
        }

        if (nameEn.Length > MaxNameLength)
        {
            throw new AppException(ErrorCodes.CategoryNameTooLong, "English name is too long", HttpStatusCode.BadRequest);
        }

        return new CreateCategoryRequest
        {
            NamePl = namePl,
            NameEn = nameEn
        };
    }

    public UpdateCategoryRequest ValidateAndNormalize(UpdateCategoryRequest req)
    {
        if (req is null)
        {
            throw new AppException(ErrorCodes.ValidationFailed, "Request is required", HttpStatusCode.BadRequest);
        }

        string? namePl = req.NamePl?.Trim();
        if (req.NamePl is not null)
        {
            if (string.IsNullOrWhiteSpace(namePl))
            {
                throw new AppException(ErrorCodes.CategoryNameRequired, "Polish name cannot be empty", HttpStatusCode.BadRequest);
            }

            if (namePl.Length < MinNameLength)
            {
                throw new AppException(ErrorCodes.CategoryNameTooShort, "Polish name is too short", HttpStatusCode.BadRequest);
            }

            if (namePl.Length > MaxNameLength)
            {
                throw new AppException(ErrorCodes.CategoryNameTooLong, "Polish name is too long", HttpStatusCode.BadRequest);
            }
        }

        string? nameEn = req.NameEn?.Trim();
        if (req.NameEn is not null)
        {
            if (string.IsNullOrWhiteSpace(nameEn))
            {
                throw new AppException(ErrorCodes.CategoryNameRequired, "English name cannot be empty", HttpStatusCode.BadRequest);
            }

            if (nameEn.Length < MinNameLength)
            {
                throw new AppException(ErrorCodes.CategoryNameTooShort, "English name is too short", HttpStatusCode.BadRequest);
            }

            if (nameEn.Length > MaxNameLength)
            {
                throw new AppException(ErrorCodes.CategoryNameTooLong, "English name is too long", HttpStatusCode.BadRequest);
            }
        }

        // At least one field must be provided
        if (namePl is null && nameEn is null && req.IsActive is null)
        {
            throw new AppException(ErrorCodes.ValidationFailed, "No fields to update", HttpStatusCode.BadRequest);
        }

        return new UpdateCategoryRequest
        {
            NamePl = namePl,
            NameEn = nameEn,
            IsActive = req.IsActive
        };
    }
}
