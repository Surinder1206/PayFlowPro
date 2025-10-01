-- Create Audit Tables for Payslip Management System

-- AuditLogs table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AuditLogs' AND xtype='U')
CREATE TABLE [AuditLogs] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [UserId] nvarchar(100) NOT NULL,
    [UserEmail] nvarchar(200) NOT NULL,
    [Action] nvarchar(50) NOT NULL,
    [EntityType] nvarchar(100) NOT NULL,
    [EntityId] nvarchar(50) NULL,
    [OldValues] nvarchar(max) NULL,
    [NewValues] nvarchar(max) NULL,
    [Description] nvarchar(500) NULL,
    [IpAddress] nvarchar(100) NOT NULL,
    [UserAgent] nvarchar(500) NULL,
    [Severity] nvarchar(50) NOT NULL DEFAULT 'Info',
    [Category] nvarchar(100) NOT NULL DEFAULT 'General',
    [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
    [CorrelationId] nvarchar(100) NULL,
    [IsSuccess] bit NOT NULL DEFAULT 1,
    [ErrorMessage] nvarchar(max) NULL,
    [StackTrace] nvarchar(max) NULL,
    CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
);

-- SecurityEvents table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='SecurityEvents' AND xtype='U')
CREATE TABLE [SecurityEvents] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [EventType] nvarchar(50) NOT NULL,
    [Severity] nvarchar(50) NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [UserId] nvarchar(100) NULL,
    [UserEmail] nvarchar(200) NULL,
    [IpAddress] nvarchar(100) NOT NULL,
    [UserAgent] nvarchar(500) NULL,
    [Resource] nvarchar(100) NULL,
    [HttpMethod] nvarchar(50) NULL,
    [RequestUrl] nvarchar(500) NULL,
    [ResponseCode] int NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
    [CorrelationId] nvarchar(100) NULL,
    [IsResolved] bit NOT NULL DEFAULT 0,
    [ResolvedAt] datetime2 NULL,
    [ResolvedBy] nvarchar(100) NULL,
    [ResolutionNotes] nvarchar(max) NULL,
    [AdditionalData] nvarchar(max) NULL,
    CONSTRAINT [PK_SecurityEvents] PRIMARY KEY ([Id])
);

-- UserSessions table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='UserSessions' AND xtype='U')
CREATE TABLE [UserSessions] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [UserId] nvarchar(100) NOT NULL,
    [SessionId] nvarchar(200) NOT NULL,
    [IpAddress] nvarchar(100) NOT NULL,
    [UserAgent] nvarchar(500) NULL,
    [Location] nvarchar(100) NULL,
    [DeviceType] nvarchar(50) NULL,
    [LoginTime] datetime2 NOT NULL DEFAULT GETUTCDATE(),
    [LogoutTime] datetime2 NULL,
    [LastActivity] datetime2 NOT NULL DEFAULT GETUTCDATE(),
    [IsActive] bit NOT NULL DEFAULT 1,
    [IsBlocked] bit NOT NULL DEFAULT 0,
    [BlockReason] nvarchar(200) NULL,
    [LoginAttempts] int NOT NULL DEFAULT 0,
    [LastLoginAttempt] datetime2 NULL,
    CONSTRAINT [PK_UserSessions] PRIMARY KEY ([Id]),
    CONSTRAINT [UK_UserSessions_SessionId] UNIQUE ([SessionId])
);

-- DataChangeLogs table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='DataChangeLogs' AND xtype='U')
CREATE TABLE [DataChangeLogs] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [EntityType] nvarchar(100) NOT NULL,
    [EntityId] nvarchar(50) NOT NULL,
    [ChangeType] nvarchar(50) NOT NULL,
    [UserId] nvarchar(100) NOT NULL,
    [UserEmail] nvarchar(200) NOT NULL,
    [OldValues] nvarchar(max) NULL,
    [NewValues] nvarchar(max) NULL,
    [ChangedProperties] nvarchar(max) NULL,
    [ChangeTime] datetime2 NOT NULL DEFAULT GETUTCDATE(),
    [IpAddress] nvarchar(100) NOT NULL,
    [Reason] nvarchar(500) NULL,
    [CorrelationId] nvarchar(100) NULL,
    [Version] int NOT NULL DEFAULT 1,
    [ParentEntityType] nvarchar(50) NULL,
    [ParentEntityId] nvarchar(50) NULL,
    CONSTRAINT [PK_DataChangeLogs] PRIMARY KEY ([Id])
);

-- Create indexes for performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_AuditLogs_CreatedAt')
CREATE INDEX [IX_AuditLogs_CreatedAt] ON [AuditLogs] ([CreatedAt]);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_AuditLogs_UserId_CreatedAt')
CREATE INDEX [IX_AuditLogs_UserId_CreatedAt] ON [AuditLogs] ([UserId], [CreatedAt]);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_AuditLogs_EntityType_EntityId')
CREATE INDEX [IX_AuditLogs_EntityType_EntityId] ON [AuditLogs] ([EntityType], [EntityId]);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_SecurityEvents_CreatedAt')
CREATE INDEX [IX_SecurityEvents_CreatedAt] ON [SecurityEvents] ([CreatedAt]);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_SecurityEvents_EventType_CreatedAt')
CREATE INDEX [IX_SecurityEvents_EventType_CreatedAt] ON [SecurityEvents] ([EventType], [CreatedAt]);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_UserSessions_UserId_LoginTime')
CREATE INDEX [IX_UserSessions_UserId_LoginTime] ON [UserSessions] ([UserId], [LoginTime]);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_DataChangeLogs_ChangeTime')
CREATE INDEX [IX_DataChangeLogs_ChangeTime] ON [DataChangeLogs] ([ChangeTime]);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_DataChangeLogs_EntityType_EntityId_ChangeTime')
CREATE INDEX [IX_DataChangeLogs_EntityType_EntityId_ChangeTime] ON [DataChangeLogs] ([EntityType], [EntityId], [ChangeTime]);

-- Add foreign key constraints
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name='FK_AuditLogs_Users_UserId')
ALTER TABLE [AuditLogs] ADD CONSTRAINT [FK_AuditLogs_Users_UserId] 
FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION;

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name='FK_SecurityEvents_Users_UserId')
ALTER TABLE [SecurityEvents] ADD CONSTRAINT [FK_SecurityEvents_Users_UserId] 
FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE SET NULL;

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name='FK_UserSessions_Users_UserId')
ALTER TABLE [UserSessions] ADD CONSTRAINT [FK_UserSessions_Users_UserId] 
FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE;

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name='FK_DataChangeLogs_Users_UserId')
ALTER TABLE [DataChangeLogs] ADD CONSTRAINT [FK_DataChangeLogs_Users_UserId] 
FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION;

PRINT 'Audit tables created successfully!';