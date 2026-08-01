using System.Net;
using Microsoft.Extensions.Options;
using TicketingSystem.Api.Common;
using TicketingSystem.Api.DTOs.Comments;
using TicketingSystem.Api.Utils;

namespace TicketingSystem.Api.Validators
{
    public interface ICommentValidator
    {
        string ValidateAndNormalize(AddCommentRequest req);
    }

    public sealed class CommentValidator : ICommentValidator
    {
        private readonly CommentOptions _opts;
        public CommentValidator(IOptions<CommentOptions> opts) => _opts = opts.Value;

        public string ValidateAndNormalize(AddCommentRequest req)
        {
            var content = req.Content ?? string.Empty;
            content = content.Trim();

            if (content.Length < _opts.MinLength)
                throw new AppException(ErrorCodes.CommentEmpty,
                    "Comment content is required.", HttpStatusCode.BadRequest);

            if (content.Length > _opts.MaxLength)
                throw new AppException(ErrorCodes.CommentTooLong,
                    $"Comment content exceeds max length {_opts.MaxLength}.", HttpStatusCode.BadRequest);

            return content;
        }
    }
}
