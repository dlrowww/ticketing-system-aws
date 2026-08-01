namespace TicketingSystem.Api.Utils
{
    public sealed class FileUploadOptions
    {
        /// <summary>
        /// Configuration section name used in Program.cs when binding.
        /// </summary>
        public const string SectionName = "FileUpload";
        /// <summary>Root folder for binary storage (relative or absolute). E.g. "wwwroot/uploads".</summary>
        public string Root { get; set; } = "wwwroot/uploads";

        /// <summary>Maximum number of files accepted in a single request.</summary>
        public int MaxFiles { get; set; } = 10;

        /// <summary>Max size of a single file in bytes. Default = 20 MB.</summary>
        public long MaxFileSizeBytes { get; set; } = 20L * 1024 * 1024;

        /// <summary>Max total size (sum of files) per request in bytes. Default = 50 MB.</summary>
        public long MaxTotalSizeBytes { get; set; } = 50L * 1024 * 1024;

        /// <summary>Allowed MIME content types. Empty = allow all (not recommended).</summary>
        public string[] AllowedContentTypes { get; set; } =
        [
            "image/png",
            "image/jpeg",
            "application/pdf",
            "text/plain",
            "application/zip",
            "application/x-zip-compressed"
        ];
    }
}