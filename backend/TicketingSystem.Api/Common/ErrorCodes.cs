#nullable enable

namespace TicketingSystem.Api.Common
{
    /// <summary>
    /// Stable, machine-readable error codes.
    /// Use these as keys on the FE to localize user messages.
    /// </summary>
    public static class ErrorCodes
    {
        // Generic / platform
        public const string ValidationFailed = "VALIDATION_FAILED";
        public const string AccessDenied = "ACCESS_DENIED";
        public const string ForbiddenOperation = "FORBIDDEN_OPERATION";
        public const string InvalidCredentials = "INVALID_CREDENTIALS";
        public const string NotFound = "NOT_FOUND";
        public const string Conflict = "CONFLICT";
        public const string UnsupportedMediaType = "UNSUPPORTED_MEDIA_TYPE";
        public const string TooLarge = "PAYLOAD_TOO_LARGE";
        public const string InternalError = "INTERNAL_ERROR";
        public const string Unauthenticated = "UNAUTHENTICATED";
        public const string IdentityMappingMissing = "IDENTITY_MAP_MISSING";

        // Domain: Tickets
        public const string TicketTitleTooShort       = "TICKET_TITLE_TOO_SHORT";
        public const string TicketTitleTooLong        = "TICKET_TITLE_TOO_LONG";
        public const string TicketDescriptionTooShort = "TICKET_DESCRIPTION_TOO_SHORT";
        public const string TicketDescriptionTooLong  = "TICKET_DESCRIPTION_TOO_LONG";
        public const string TicketCategoryRequired    = "TICKET_CATEGORY_REQUIRED";
        public const string TicketCategoryInvalid = "TICKET_CATEGORY_INVALID";
        public const string TicketPriorityRequired = "TICKET_PRIORITY_REQUIRED";
        public const string TicketPriorityInvalid = "TICKET_PRIORITY_INVALID";
        public const string TicketStatusInvalid = "TICKET_STATUS_INVALID";
        public const string TicketStatusTransitionInvalid = "TICKET_STATUS_TRANSITION_INVALID";
        public const string TicketNotFound = "TICKET_NOT_FOUND";
        public const string TicketClosed = "TICKET_CLOSED";
        public const string AssignmentInvalid = "ASSIGNMENT_INVALID";
        public const string TicketEditPermissionDenied = "TICKET_EDIT_PERMISSION_DENIED";

        // Domain: Attachments
        public const string FileTooLarge = "FILE_TOO_LARGE";
        public const string FileTypeNotAllowed = "FILE_TYPE_NOT_ALLOWED";
        public const string TooManyFiles = "MAX_FILE_SIZE_EXCEEDED";
        public const string TotalFilesSizeExceeded = "TOTAL_FILES_SIZE_EXCEEDED";
        public const string EmptyFile = "EMPTY_FILE";
        public const string FileNameInvalid = "FILE_NAME_INVALID";
        public const string FileNotFound = "FILE_NOT_FOUND";
        public const string FileContentNotFound = "FILE_CONTENT_NOT_FOUND";
        public const string StorageDeleteFailed = "STORAGE_DELETE_FAILED";
        public const string StorageSaveFailed = "STORAGE_SAVE_FAILED";

        // Domain: Comments
        public const string CommentEmpty = "COMMENT_EMPTY";
        public const string CommentTooLong = "COMMENT_TOO_LONG";
        public const string CommentNotFound = "COMMENT_NOT_FOUND";
        public const string CommentInternalNotAllowed = "COMMENT_INTERNAL_NOT_ALLOWED";

        // Domain: Email/Notifications
        public const string EmailSendFailed = "EMAIL_SEND_FAILED";
        public const string NotificationFailed = "NOTIFICATION_FAILED";

        // Domain: User
        public const string UserNotFound = "USER_NOT_FOUND";
        public const string UserInactive = "USER_INACTIVE";
        public const string UserEmailAlreadyExists = "USER_EMAIL_ALREADY_EXISTS";
        public const string UserCannotDeactivateSelf = "USER_CANNOT_DEACTIVATE_SELF";
        public const string UserNameRequired = "USER_NAME_REQUIRED";
        public const string UserNameTooLong = "USER_NAME_TOO_LONG";
        public const string UserEmailRequired = "USER_EMAIL_REQUIRED";
        public const string UserEmailTooLong = "USER_EMAIL_TOO_LONG";
        public const string UserEmailInvalid = "USER_EMAIL_INVALID";
        public const string UserPasswordRequired = "USER_PASSWORD_REQUIRED";
        public const string UserPasswordTooShort = "USER_PASSWORD_TOO_SHORT";
        public const string UserPasswordTooWeak = "USER_PASSWORD_TOO_WEAK";
        public const string UserRoleInvalid = "USER_ROLE_INVALID";
        public const string UserCategoryInvalid = "USER_CATEGORY_INVALID";
        public const string UserCategoryRequired = "USER_CATEGORY_REQUIRED";
        public const string InvalidAssigneeRole = "INVALID_ASSIGNEE_ROLE";
        public const string AssigneeCategoryMismatch = "ASSIGNEE_CATEGORY_MISMATCH";
        public const string UserHasAssignedTickets = "USER_HAS_ASSIGNED_TICKETS";

        // Domain: Categories
        public const string CategoryNotFound = "CATEGORY_NOT_FOUND";
        public const string CategoryNameRequired = "CATEGORY_NAME_REQUIRED";
        public const string CategoryNameTooShort = "CATEGORY_NAME_TOO_SHORT";
        public const string CategoryNameTooLong = "CATEGORY_NAME_TOO_LONG";
        public const string CategoryNameAlreadyExists = "CATEGORY_NAME_ALREADY_EXISTS";
        public const string CategoryInUse = "CATEGORY_IN_USE";
    }
}