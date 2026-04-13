CREATE DATABASE COEPDSalesFunnelDb;
GO

USE COEPDSalesFunnelDb;
GO

CREATE TABLE dbo.Users
(
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
    FullName NVARCHAR(120) NOT NULL,
    Email NVARCHAR(150) NOT NULL,
    PasswordHash NVARCHAR(250) NOT NULL,
    Role NVARCHAR(20) NOT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
    FailedLoginAttempts INT NOT NULL CONSTRAINT DF_Users_FailedLoginAttempts DEFAULT (0),
    LockoutEndUtc DATETIME2 NULL,
    LastLoginAtUtc DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_Users_UpdatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT CK_Users_Role CHECK (Role IN ('Admin', 'Staff'))
);
GO

CREATE TABLE dbo.Leads
(
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Leads PRIMARY KEY,
    Name NVARCHAR(120) NOT NULL,
    Phone NVARCHAR(20) NOT NULL,
    Email NVARCHAR(150) NOT NULL,
    Location NVARCHAR(120) NOT NULL,
    InterestedDomain NVARCHAR(120) NOT NULL,
    Source NVARCHAR(50) NOT NULL,
    Status NVARCHAR(20) NOT NULL CONSTRAINT DF_Leads_Status DEFAULT ('New'),
    Score NVARCHAR(20) NOT NULL CONSTRAINT DF_Leads_Score DEFAULT ('Warm'),
    Notes NVARCHAR(2000) NULL,
    FunnelStage NVARCHAR(30) NOT NULL CONSTRAINT DF_Leads_FunnelStage DEFAULT ('New'),
    AssignedStaffId INT NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Leads_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_Leads_UpdatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT UQ_Leads_Email UNIQUE (Email),
    CONSTRAINT UQ_Leads_Phone UNIQUE (Phone),
    CONSTRAINT FK_Leads_Users_AssignedStaff FOREIGN KEY (AssignedStaffId) REFERENCES dbo.Users(Id) ON DELETE SET NULL,
    CONSTRAINT CK_Leads_Status CHECK (Status IN ('New', 'Contacted', 'DemoBooked', 'Converted')),
    CONSTRAINT CK_Leads_Score CHECK (Score IN ('Cold', 'Warm', 'Hot')),
    CONSTRAINT CK_Leads_FunnelStage CHECK (FunnelStage IN ('New', 'Contacted', 'DemoBooked', 'Enrolled', 'Lost')),
    CONSTRAINT CK_Leads_Source CHECK (Source IN ('Website', 'Chatbot', 'Ads'))
);
GO

CREATE TABLE dbo.DemoBookings
(
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DemoBookings PRIMARY KEY,
    LeadId INT NOT NULL,
    Day NVARCHAR(40) NOT NULL,
    Slot NVARCHAR(40) NOT NULL,
    Status NVARCHAR(20) NOT NULL CONSTRAINT DF_DemoBookings_Status DEFAULT ('Pending'),
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_DemoBookings_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_DemoBookings_UpdatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT FK_DemoBookings_Leads FOREIGN KEY (LeadId) REFERENCES dbo.Leads(Id) ON DELETE CASCADE,
    CONSTRAINT CK_DemoBookings_Status CHECK (Status IN ('Pending', 'Confirmed', 'Cancelled', 'Completed'))
);
GO

CREATE TABLE dbo.LeadActivities
(
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LeadActivities PRIMARY KEY,
    LeadId INT NOT NULL,
    UserId INT NULL,
    ActivityType NVARCHAR(60) NOT NULL,
    Message NVARCHAR(500) NOT NULL,
    Status NVARCHAR(30) NOT NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_LeadActivities_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_LeadActivities_UpdatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT FK_LeadActivities_Leads FOREIGN KEY (LeadId) REFERENCES dbo.Leads(Id) ON DELETE CASCADE,
    CONSTRAINT FK_LeadActivities_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE SET NULL,
    CONSTRAINT CK_LeadActivities_Status CHECK (Status IN ('New', 'Contacted', 'DemoBooked', 'Converted', 'Success', 'Failed'))
);
GO

CREATE TABLE dbo.FunnelEvents
(
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_FunnelEvents PRIMARY KEY,
    LeadId INT NOT NULL,
    Stage NVARCHAR(20) NOT NULL,
    [Timestamp] DATETIME2 NOT NULL CONSTRAINT DF_FunnelEvents_Timestamp DEFAULT (SYSUTCDATETIME()),
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_FunnelEvents_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_FunnelEvents_UpdatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT CK_FunnelEvents_Stage CHECK (Stage IN ('Awareness', 'Interest', 'Desire', 'Action'))
);
GO

CREATE TABLE dbo.LeadFollowUpJobs
(
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LeadFollowUpJobs PRIMARY KEY,
    LeadId INT NOT NULL,
    FollowUpType NVARCHAR(30) NOT NULL,
    DueAt DATETIME2 NOT NULL,
    Status NVARCHAR(20) NOT NULL CONSTRAINT DF_LeadFollowUpJobs_Status DEFAULT ('Pending'),
    AttemptCount INT NOT NULL CONSTRAINT DF_LeadFollowUpJobs_AttemptCount DEFAULT (0),
    ProcessedAt DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_LeadFollowUpJobs_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_LeadFollowUpJobs_UpdatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT CK_LeadFollowUpJobs_Type CHECK (FollowUpType IN ('OneHour', 'OneDay')),
    CONSTRAINT CK_LeadFollowUpJobs_Status CHECK (Status IN ('Pending', 'Completed', 'Failed'))
);
GO

CREATE TABLE dbo.ChatSessions
(
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ChatSessions PRIMARY KEY,
    SessionId NVARCHAR(80) NOT NULL,
    Stage NVARCHAR(50) NOT NULL,
    Name NVARCHAR(120) NULL,
    Phone NVARCHAR(20) NULL,
    Email NVARCHAR(150) NULL,
    Location NVARCHAR(120) NULL,
    Domain NVARCHAR(120) NULL,
    LeadCaptured BIT NOT NULL CONSTRAINT DF_ChatSessions_LeadCaptured DEFAULT (0),
    Source NVARCHAR(50) NOT NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_ChatSessions_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_ChatSessions_UpdatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT UQ_ChatSessions_SessionId UNIQUE (SessionId)
);
GO

CREATE TABLE dbo.ChatMessages
(
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ChatMessages PRIMARY KEY,
    ChatSessionId INT NOT NULL,
    Sender NVARCHAR(20) NOT NULL,
    Content NVARCHAR(2000) NOT NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_ChatMessages_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_ChatMessages_UpdatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT FK_ChatMessages_ChatSessions FOREIGN KEY (ChatSessionId) REFERENCES dbo.ChatSessions(Id) ON DELETE CASCADE
);
GO

CREATE TABLE dbo.EmailAutomationLogs
(
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_EmailAutomationLogs PRIMARY KEY,
    LeadId INT NOT NULL,
    TemplateKey NVARCHAR(120) NOT NULL,
    Subject NVARCHAR(250) NOT NULL,
    Status NVARCHAR(50) NOT NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_EmailAutomationLogs_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_EmailAutomationLogs_UpdatedAt DEFAULT (SYSUTCDATETIME())
);
GO

CREATE TABLE dbo.WhatsAppMessageLogs
(
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_WhatsAppMessageLogs PRIMARY KEY,
    LeadId INT NOT NULL,
    MessageType NVARCHAR(120) NOT NULL,
    Phone NVARCHAR(20) NOT NULL,
    Status NVARCHAR(50) NOT NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_WhatsAppMessageLogs_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_WhatsAppMessageLogs_UpdatedAt DEFAULT (SYSUTCDATETIME())
);
GO

CREATE INDEX IX_Users_Role_IsActive ON dbo.Users(Role, IsActive);
CREATE INDEX IX_Leads_CreatedAt ON dbo.Leads(CreatedAt DESC);
CREATE INDEX IX_Leads_UpdatedAt ON dbo.Leads(UpdatedAt DESC);
CREATE INDEX IX_Leads_Status ON dbo.Leads(Status);
CREATE INDEX IX_Leads_Source ON dbo.Leads(Source);
CREATE INDEX IX_Leads_InterestedDomain ON dbo.Leads(InterestedDomain);
CREATE INDEX IX_Leads_Status_CreatedAt ON dbo.Leads(Status, CreatedAt DESC);
CREATE INDEX IX_Leads_AssignedStaffId ON dbo.Leads(AssignedStaffId);
CREATE INDEX IX_DemoBookings_LeadId ON dbo.DemoBookings(LeadId);
CREATE INDEX IX_DemoBookings_Day_Slot_Status ON dbo.DemoBookings(Day, Slot, Status);
CREATE INDEX IX_LeadActivities_LeadId ON dbo.LeadActivities(LeadId);
CREATE INDEX IX_LeadActivities_LeadId_CreatedAt ON dbo.LeadActivities(LeadId, CreatedAt DESC);
CREATE INDEX IX_LeadActivities_UserId ON dbo.LeadActivities(UserId);
CREATE INDEX IX_FunnelEvents_LeadId ON dbo.FunnelEvents(LeadId);
CREATE INDEX IX_FunnelEvents_Stage ON dbo.FunnelEvents(Stage);
CREATE INDEX IX_LeadFollowUpJobs_DueAt ON dbo.LeadFollowUpJobs(DueAt);
CREATE INDEX IX_LeadFollowUpJobs_Status_DueAt ON dbo.LeadFollowUpJobs(Status, DueAt);
CREATE INDEX IX_ChatMessages_ChatSessionId ON dbo.ChatMessages(ChatSessionId);
GO

CREATE OR ALTER TRIGGER dbo.TR_Users_SetUpdatedAt
ON dbo.Users
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE u
    SET UpdatedAt = SYSUTCDATETIME()
    FROM dbo.Users u
    INNER JOIN inserted i ON i.Id = u.Id;
END;
GO

CREATE OR ALTER TRIGGER dbo.TR_Leads_SetUpdatedAt
ON dbo.Leads
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE l
    SET UpdatedAt = SYSUTCDATETIME()
    FROM dbo.Leads l
    INNER JOIN inserted i ON i.Id = l.Id;
END;
GO

CREATE OR ALTER TRIGGER dbo.TR_DemoBookings_SetUpdatedAt
ON dbo.DemoBookings
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE d
    SET UpdatedAt = SYSUTCDATETIME()
    FROM dbo.DemoBookings d
    INNER JOIN inserted i ON i.Id = d.Id;
END;
GO

CREATE OR ALTER TRIGGER dbo.TR_LeadActivities_SetUpdatedAt
ON dbo.LeadActivities
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE la
    SET UpdatedAt = SYSUTCDATETIME()
    FROM dbo.LeadActivities la
    INNER JOIN inserted i ON i.Id = la.Id;
END;
GO
