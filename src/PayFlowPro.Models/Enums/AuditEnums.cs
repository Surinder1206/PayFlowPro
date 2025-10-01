namespace PayFlowPro.Models.Enums;

public enum AuditAction
{
    Create,
    Read,
    Update,
    Delete,
    Login,
    Logout,
    LoginFailed,
    PasswordChanged,
    PasswordReset,
    AccountLocked,
    AccountUnlocked,
    PermissionGranted,
    PermissionDenied,
    Export,
    Import,
    Print,
    Email,
    Backup,
    Restore,
    Configuration,
    SystemStart,
    SystemStop,
    Other
}

public enum AuditSeverity
{
    Info,
    Warning,
    Error,
    Critical,
    Debug
}

public enum AuditCategory
{
    Authentication,
    Authorization,
    DataAccess,
    DataModification,
    SystemOperation,
    UserActivity,
    Security,
    Configuration,
    Export,
    Import,
    Report,
    Payroll,
    Employee,
    Company,
    General
}

public enum SecurityEventType
{
    LoginAttempt,
    LoginSuccess,
    LoginFailure,
    LogoutSuccess,
    PasswordChange,
    PasswordReset,
    AccountLockout,
    SuspiciousActivity,
    UnauthorizedAccess,
    DataBreach,
    SystemIntegrity,
    ConfigurationChange,
    PermissionEscalation,
    BruteForceAttempt,
    SessionHijacking,
    SqlInjectionAttempt,
    XssAttempt,
    CsrfAttempt,
    MaliciousUpload,
    Other
}

public enum SecuritySeverity
{
    Low,
    Medium,
    High,
    Critical
}

public enum ChangeType
{
    Create,
    Update,
    Delete,
    BulkUpdate,
    BulkDelete,
    StatusChange,
    Approval,
    Rejection,
    Archive,
    Restore
}