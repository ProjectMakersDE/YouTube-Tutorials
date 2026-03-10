namespace PM.Service
{
    public enum ErrorCode
    {
        // Default Code
        None = 0,
        UnknownError = 1,

        // Success Codes
        Ok = 200,

        // Error Codes
        BadRequest = 400,
        Unauthorized = 401,
        Forbidden = 403,
        NotFound = 404,
        MethodNotAllowed = 405,
        Conflict = 409,
        MessageSizeExceeded = 413,
        RateLimitExceeded = 429,
        SessionInvalid = 430,
        TokenMismatch = 440,

        UserNotFound = 450,
    
        // Health Codes
        InternalServerError = 500,
        NotImplemented = 501,
        ServiceUnavailable = 503,

        // Database Codes
        DatabaseSettingsMissing = 1000,
        DatabaseConnectionError = 1001,
        DatabasePingError = 1002,
        DatabaseCollectionError = 1003,
        DatabaseDocumentError = 1004,
        DatabaseFilterError = 1005,

        // Mail Codes
        MailTemplateMissing = 2000,
        MailSubjectError = 2001,
        MailTextError = 2002,
        WebSocketClose,
        WebSocketTimeout,
        WebSocketError,
        ServerError,
        SerializationError,
        WebSocketSendError,
        AuthenticationError
    }
}