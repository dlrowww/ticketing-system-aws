using System.Net;
using Microsoft.Extensions.Options;
using TicketingSystem.Api.Common;
using TicketingSystem.Api.DTOs.Tickets;
using TicketingSystem.Api.Enums.Tickets;
using TicketingSystem.Api.Utils;

namespace TicketingSystem.Api.Validators
{
    public interface ITicketUpdateValidator
    {
        NormalizedTicketUpdate ValidateAndNormalize(UpdateTicketRequest req);
    }

    public sealed record NormalizedTicketUpdate(
        string? Title,
        string? Description,
        bool HasAnyChange,
        bool ClearAssignmentRequested
    )
    {
        // convenience flags; enums & AssignedToUserId are checked in service layer
    }

    public sealed class TicketUpdateValidator : ITicketUpdateValidator
    {
        private readonly TicketOptions _opts;
        public TicketUpdateValidator(IOptions<TicketOptions> opts) => _opts = opts.Value;

        public NormalizedTicketUpdate ValidateAndNormalize(UpdateTicketRequest req)
        {
            var errors = new Dictionary<string, List<string>>();

            static void Add(Dictionary<string, List<string>> dict, string field, string code)
            {
                if (!dict.TryGetValue(field, out var list))
                {
                    list = new List<string>();
                    dict[field] = list;
                }
                list.Add(code);
            }

            // Trim text if present
            var title = req.Title?.Trim();
            var desc  = req.Description?.Trim();

            // Title validation
            if (title is not null)
            {
                if (title.Length < _opts.TitleMinLength)
                    Add(errors, "Title", ErrorCodes.TicketTitleTooShort);
                if (title.Length > _opts.TitleMaxLength)
                    Add(errors, "Title", ErrorCodes.TicketTitleTooLong);
            }

            // Description validation
            if (desc is not null)
            {
                if (desc.Length < _opts.DescriptionMinLength)
                    Add(errors, "Description", ErrorCodes.TicketDescriptionTooShort);
                if (desc.Length > _opts.DescriptionMaxLength)
                    Add(errors, "Description", ErrorCodes.TicketDescriptionTooLong);
            }

            // Assignment conflict check
            if (req.ClearAssignment == true && req.AssignedToUserId is not null)
                Add(errors, "AssignedToUserId", ErrorCodes.ValidationFailed);

            // Priority validation
            if (req.Priority.HasValue && !Enum.IsDefined(typeof(TicketPriority), req.Priority.Value))
                Add(errors, "Priority", ErrorCodes.TicketPriorityInvalid);

            // Status validation
            if (req.Status.HasValue && !Enum.IsDefined(typeof(TicketStatus), req.Status.Value))
                Add(errors, "Status", ErrorCodes.TicketStatusInvalid);

            // At least one field supplied?
            var hasAny =
                title is not null ||
                desc  is not null ||
                req.CategoryId.HasValue ||
                req.Priority.HasValue ||
                req.Status.HasValue ||
                req.AssignedToUserId.HasValue ||
                (req.ClearAssignment ?? false);

            if (!hasAny)
                Add(errors, "", ErrorCodes.ValidationFailed);

            // Throw if there are any validation errors
            if (errors.Count > 0)
            {
                var normalized = errors.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Distinct().ToArray());
                throw new AppValidationException(normalized);
            }

            return new NormalizedTicketUpdate(
                Title: title,
                Description: desc,
                HasAnyChange: hasAny,
                ClearAssignmentRequested: req.ClearAssignment == true
            );
        }
    }
}