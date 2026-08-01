using Microsoft.Extensions.Options;
using System.Net;

using TicketingSystem.Api.Common;
using TicketingSystem.Api.DTOs.Tickets;
using TicketingSystem.Api.Enums.Tickets;
using TicketingSystem.Api.Utils;

namespace TicketingSystem.Api.Validators
{
    public interface ITicketValidator
    {
        NormalizedTicketInput ValidateAndNormalize(CreateTicketRequest req);
    }

    public sealed record NormalizedTicketInput(
        string Title,
        string Description,
        int CategoryId,
        TicketPriority Priority
    );

    public sealed class TicketValidator : ITicketValidator
    {
        private readonly TicketOptions _opts;
        public TicketValidator(IOptions<TicketOptions> opts) => _opts = opts.Value;

        public NormalizedTicketInput ValidateAndNormalize(CreateTicketRequest req)
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

            // Title
            var title = (req.Title ?? string.Empty).Trim();
            if (title.Length < _opts.TitleMinLength)
                Add(errors, "Title", ErrorCodes.TicketTitleTooShort);
            if (title.Length > _opts.TitleMaxLength)
                Add(errors, "Title", ErrorCodes.TicketTitleTooLong);

            // Description
            var desc = (req.Description ?? string.Empty).Trim();
            if (desc.Length < _opts.DescriptionMinLength)
                Add(errors, "Description", ErrorCodes.TicketDescriptionTooShort);
            if (desc.Length > _opts.DescriptionMaxLength)
                Add(errors, "Description", ErrorCodes.TicketDescriptionTooLong);

            // Required fields
            if (req.CategoryId is null)
                Add(errors, "CategoryId", ErrorCodes.TicketCategoryRequired);
            if (req.Priority is null)
                Add(errors, "Priority", ErrorCodes.TicketPriorityRequired);

            // Enum validity check for Priority only (CategoryId will be validated against DB)
            // Note: CategoryId validation against existing categories happens in service layer

            if (req.Priority is not null && !Enum.IsDefined(typeof(TicketPriority), req.Priority.Value))
                Add(errors, "Priority", ErrorCodes.TicketPriorityInvalid);

            if (errors.Count > 0)
            {
                var normalized = errors.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Distinct().ToArray());
                throw new AppValidationException(normalized);
            }

            // After validation, these are guaranteed to be present and valid.
            var categoryId = req.CategoryId!.Value;
            var priority = req.Priority!.Value;

            return new NormalizedTicketInput(
                title,
                desc,
                categoryId,
                priority
            );
        }
    }
}