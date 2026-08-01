#nullable enable
using Microsoft.Extensions.Options;

using System.Net;
using System.Text.RegularExpressions;

using TicketingSystem.Api.Common;
using TicketingSystem.Api.Utils;

namespace TicketingSystem.Api.Validators
{
    public interface IAttachmentValidator
    {
        void ValidateFiles(IFormFile[] files);
    }

    public sealed class AttachmentValidator : IAttachmentValidator
    {
        private readonly FileUploadOptions _opts;

        // simple filename guard (no path traversal, control chars)
        private static readonly Regex SafeName = new(@"^[^\\/:*?""<>|\r\n]+$", RegexOptions.Compiled);

        public AttachmentValidator(IOptions<FileUploadOptions> opts)
        {
            _opts = opts.Value;
        }

        public void ValidateFiles(IFormFile[] files)
        {
            var codes = new HashSet<string>();

            if (files.Length > _opts.MaxFiles)
                codes.Add(ErrorCodes.TooManyFiles);

            var total = files.Sum(f => f.Length);
            if (total > _opts.MaxTotalSizeBytes)
                codes.Add(ErrorCodes.TotalFilesSizeExceeded);

            foreach (var f in files)
            {
                if (f.Length <= 0)
                    codes.Add(ErrorCodes.EmptyFile);

                if (f.Length > _opts.MaxFileSizeBytes)
                    codes.Add(ErrorCodes.FileTooLarge);

                if (_opts.AllowedContentTypes?.Length > 0)
                {
                    var ctype = string.IsNullOrWhiteSpace(f.ContentType) ? "application/octet-stream" : f.ContentType;
                    if (!_opts.AllowedContentTypes.Contains(ctype, StringComparer.OrdinalIgnoreCase))
                        codes.Add(ErrorCodes.FileTypeNotAllowed);
                }

                if (string.IsNullOrWhiteSpace(f.FileName) || !SafeName.IsMatch(f.FileName))
                    codes.Add(ErrorCodes.FileNameInvalid);
            }

            if (codes.Count > 0)
            {
                throw new AppValidationException(new Dictionary<string, string[]>
                {
                    ["Files"] = codes.ToArray()
                });
            }
        }
    }
}
