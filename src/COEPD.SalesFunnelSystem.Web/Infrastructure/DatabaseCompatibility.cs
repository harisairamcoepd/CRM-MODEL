using System.Data;
using System.Data.Common;
using COEPD.SalesFunnelSystem.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace COEPD.SalesFunnelSystem.Web.Infrastructure;

public static class DatabaseCompatibility
{
    private const string InitialMigrationId = "20260403072537_InitialCrmFunnelSchema";
    private const string EfProductVersion = "8.0.12";

    public static async Task AdoptLegacyDatabaseAsync(ApplicationDbContext db)
    {
        if (!db.Database.IsSqlite())
        {
            return;
        }

        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync();
        }

        try
        {
            var historyTableExists = await TableExistsAsync(connection, "__EFMigrationsHistory");

            var legacyTables = new[]
            {
                "Leads",
                "DemoBookings",
                "ChatSessions",
                "Users",
                "AppUsers",
                "LeadActivities",
                "LeadActivityLogs"
            };

            var hasLegacySchema = false;
            foreach (var tableName in legacyTables)
            {
                if (await TableExistsAsync(connection, tableName))
                {
                    hasLegacySchema = true;
                    break;
                }
            }

            if (!hasLegacySchema)
            {
                return;
            }

            if (!historyTableExists)
            {
                await ExecuteNonQueryAsync(connection, """
                    CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                        "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                        "ProductVersion" TEXT NOT NULL
                    );
                    """);
            }

            if (!await MigrationExistsAsync(connection, InitialMigrationId))
            {
                await ExecuteNonQueryAsync(
                    connection,
                    $"""INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES ('{InitialMigrationId}', '{EfProductVersion}');""");
            }
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    public static async Task EnsureSchemaCompatibilityAsync(ApplicationDbContext db)
    {
        if (db.Database.IsSqlServer())
        {
            await EnsureSqlServerSchemaCompatibilityAsync(db);
            return;
        }

        if (db.Database.IsSqlite())
        {
            await EnsureSqliteSchemaCompatibilityAsync(db);
        }
    }

    private static async Task EnsureSqlServerSchemaCompatibilityAsync(ApplicationDbContext db)
    {
        const string sql = """
            IF OBJECT_ID('AppUsers', 'U') IS NOT NULL AND OBJECT_ID('Users', 'U') IS NULL
            BEGIN
                EXEC sp_rename 'AppUsers', 'Users';
            END

            IF OBJECT_ID('LeadActivityLogs', 'U') IS NOT NULL AND OBJECT_ID('LeadActivities', 'U') IS NULL
            BEGIN
                EXEC sp_rename 'LeadActivityLogs', 'LeadActivities';
            END

            IF COL_LENGTH('Leads', 'Status') IS NULL
            BEGIN
                ALTER TABLE Leads
                ADD Status NVARCHAR(20) NOT NULL
                CONSTRAINT DF_Leads_Status DEFAULT 'New';
            END

            IF COL_LENGTH('Leads', 'Score') IS NULL
            BEGIN
                ALTER TABLE Leads
                ADD Score NVARCHAR(20) NOT NULL
                CONSTRAINT DF_Leads_Score DEFAULT 'Warm';
            END

            IF COL_LENGTH('Leads', 'Notes') IS NULL
            BEGIN
                ALTER TABLE Leads
                ADD Notes NVARCHAR(2000) NULL;
            END

            IF COL_LENGTH('Leads', 'FunnelStage') IS NULL
            BEGIN
                ALTER TABLE Leads
                ADD FunnelStage NVARCHAR(30) NOT NULL
                CONSTRAINT DF_Leads_FunnelStage DEFAULT 'New';
            END

            IF COL_LENGTH('Leads', 'AssignedStaffId') IS NULL
            BEGIN
                ALTER TABLE Leads
                ADD AssignedStaffId INT NULL;
            END

            IF COL_LENGTH('Leads', 'UpdatedAt') IS NULL
            BEGIN
                ALTER TABLE Leads
                ADD UpdatedAt DATETIME2 NOT NULL
                CONSTRAINT DF_Leads_UpdatedAt DEFAULT SYSUTCDATETIME();
            END

            IF COL_LENGTH('Users', 'FailedLoginAttempts') IS NULL
            BEGIN
                ALTER TABLE Users
                ADD FailedLoginAttempts INT NOT NULL
                CONSTRAINT DF_Users_FailedLoginAttempts DEFAULT 0;
            END

            IF COL_LENGTH('Users', 'LockoutEndUtc') IS NULL
            BEGIN
                ALTER TABLE Users
                ADD LockoutEndUtc DATETIME2 NULL;
            END

            IF COL_LENGTH('Users', 'LastLoginAtUtc') IS NULL
            BEGIN
                ALTER TABLE Users
                ADD LastLoginAtUtc DATETIME2 NULL;
            END

            IF COL_LENGTH('Users', 'UpdatedAt') IS NULL
            BEGIN
                ALTER TABLE Users
                ADD UpdatedAt DATETIME2 NOT NULL
                CONSTRAINT DF_Users_UpdatedAt DEFAULT SYSUTCDATETIME();
            END

            IF OBJECT_ID('LeadActivities', 'U') IS NOT NULL AND COL_LENGTH('LeadActivities', 'UserId') IS NULL
            BEGIN
                ALTER TABLE LeadActivities
                ADD UserId INT NULL;
            END

            IF OBJECT_ID('LeadActivities', 'U') IS NOT NULL AND COL_LENGTH('LeadActivities', 'UpdatedAt') IS NULL
            BEGIN
                ALTER TABLE LeadActivities
                ADD UpdatedAt DATETIME2 NOT NULL
                CONSTRAINT DF_LeadActivities_UpdatedAt DEFAULT SYSUTCDATETIME();
            END

            IF OBJECT_ID('DemoBookings', 'U') IS NOT NULL AND COL_LENGTH('DemoBookings', 'UpdatedAt') IS NULL
            BEGIN
                ALTER TABLE DemoBookings
                ADD UpdatedAt DATETIME2 NOT NULL
                CONSTRAINT DF_DemoBookings_UpdatedAt DEFAULT SYSUTCDATETIME();
            END

            IF OBJECT_ID('FunnelEvents', 'U') IS NOT NULL AND COL_LENGTH('FunnelEvents', 'CreatedAt') IS NULL
            BEGIN
                ALTER TABLE FunnelEvents
                ADD CreatedAt DATETIME2 NOT NULL
                CONSTRAINT DF_FunnelEvents_CreatedAt DEFAULT SYSUTCDATETIME();
            END

            IF OBJECT_ID('FunnelEvents', 'U') IS NOT NULL AND COL_LENGTH('FunnelEvents', 'UpdatedAt') IS NULL
            BEGIN
                ALTER TABLE FunnelEvents
                ADD UpdatedAt DATETIME2 NOT NULL
                CONSTRAINT DF_FunnelEvents_UpdatedAt DEFAULT SYSUTCDATETIME();
            END

            IF OBJECT_ID('LeadFollowUpJobs', 'U') IS NOT NULL AND COL_LENGTH('LeadFollowUpJobs', 'AttemptCount') IS NULL
            BEGIN
                ALTER TABLE LeadFollowUpJobs
                ADD AttemptCount INT NOT NULL
                CONSTRAINT DF_LeadFollowUpJobs_AttemptCount DEFAULT 0;
            END

            IF OBJECT_ID('LeadFollowUpJobs', 'U') IS NOT NULL AND COL_LENGTH('LeadFollowUpJobs', 'ProcessedAt') IS NULL
            BEGIN
                ALTER TABLE LeadFollowUpJobs
                ADD ProcessedAt DATETIME2 NULL;
            END

            IF OBJECT_ID('LeadFollowUpJobs', 'U') IS NOT NULL AND COL_LENGTH('LeadFollowUpJobs', 'CreatedAt') IS NULL
            BEGIN
                ALTER TABLE LeadFollowUpJobs
                ADD CreatedAt DATETIME2 NOT NULL
                CONSTRAINT DF_LeadFollowUpJobs_CreatedAt DEFAULT SYSUTCDATETIME();
            END

            IF OBJECT_ID('LeadFollowUpJobs', 'U') IS NOT NULL AND COL_LENGTH('LeadFollowUpJobs', 'UpdatedAt') IS NULL
            BEGIN
                ALTER TABLE LeadFollowUpJobs
                ADD UpdatedAt DATETIME2 NOT NULL
                CONSTRAINT DF_LeadFollowUpJobs_UpdatedAt DEFAULT SYSUTCDATETIME();
            END

            IF OBJECT_ID('EmailAutomationLogs', 'U') IS NOT NULL AND COL_LENGTH('EmailAutomationLogs', 'CreatedAt') IS NULL
            BEGIN
                ALTER TABLE EmailAutomationLogs
                ADD CreatedAt DATETIME2 NOT NULL
                CONSTRAINT DF_EmailAutomationLogs_CreatedAt DEFAULT SYSUTCDATETIME();
            END

            IF OBJECT_ID('EmailAutomationLogs', 'U') IS NOT NULL AND COL_LENGTH('EmailAutomationLogs', 'UpdatedAt') IS NULL
            BEGIN
                ALTER TABLE EmailAutomationLogs
                ADD UpdatedAt DATETIME2 NOT NULL
                CONSTRAINT DF_EmailAutomationLogs_UpdatedAt DEFAULT SYSUTCDATETIME();
            END

            IF OBJECT_ID('WhatsAppMessageLogs', 'U') IS NOT NULL AND COL_LENGTH('WhatsAppMessageLogs', 'CreatedAt') IS NULL
            BEGIN
                ALTER TABLE WhatsAppMessageLogs
                ADD CreatedAt DATETIME2 NOT NULL
                CONSTRAINT DF_WhatsAppMessageLogs_CreatedAt DEFAULT SYSUTCDATETIME();
            END

            IF OBJECT_ID('WhatsAppMessageLogs', 'U') IS NOT NULL AND COL_LENGTH('WhatsAppMessageLogs', 'UpdatedAt') IS NULL
            BEGIN
                ALTER TABLE WhatsAppMessageLogs
                ADD UpdatedAt DATETIME2 NOT NULL
                CONSTRAINT DF_WhatsAppMessageLogs_UpdatedAt DEFAULT SYSUTCDATETIME();
            END

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_Email' AND object_id = OBJECT_ID('Users'))
            BEGIN
                CREATE UNIQUE INDEX IX_Users_Email ON Users(Email);
            END

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_Role_IsActive' AND object_id = OBJECT_ID('Users'))
            BEGIN
                CREATE INDEX IX_Users_Role_IsActive ON Users(Role, IsActive);
            END

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Leads_Email' AND object_id = OBJECT_ID('Leads'))
            BEGIN
                CREATE UNIQUE INDEX IX_Leads_Email ON Leads(Email);
            END

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Leads_Phone' AND object_id = OBJECT_ID('Leads'))
            BEGIN
                CREATE UNIQUE INDEX IX_Leads_Phone ON Leads(Phone);
            END

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Leads_CreatedAt' AND object_id = OBJECT_ID('Leads'))
            BEGIN
                CREATE INDEX IX_Leads_CreatedAt ON Leads(CreatedAt);
            END

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Leads_Status' AND object_id = OBJECT_ID('Leads'))
            BEGIN
                CREATE INDEX IX_Leads_Status ON Leads(Status);
            END

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Leads_Source' AND object_id = OBJECT_ID('Leads'))
            BEGIN
                CREATE INDEX IX_Leads_Source ON Leads(Source);
            END

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Leads_InterestedDomain' AND object_id = OBJECT_ID('Leads'))
            BEGIN
                CREATE INDEX IX_Leads_InterestedDomain ON Leads(InterestedDomain);
            END

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Leads_Status_CreatedAt' AND object_id = OBJECT_ID('Leads'))
            BEGIN
                CREATE INDEX IX_Leads_Status_CreatedAt ON Leads(Status, CreatedAt);
            END

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Leads_AssignedStaffId' AND object_id = OBJECT_ID('Leads'))
            BEGIN
                CREATE INDEX IX_Leads_AssignedStaffId ON Leads(AssignedStaffId);
            END

            IF OBJECT_ID('LeadActivities', 'U') IS NULL
            BEGIN
                CREATE TABLE LeadActivities (
                    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    LeadId INT NOT NULL,
                    UserId INT NULL,
                    ActivityType NVARCHAR(60) NOT NULL,
                    Message NVARCHAR(500) NOT NULL,
                    Status NVARCHAR(30) NOT NULL,
                    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                    UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
                );

                CREATE INDEX IX_LeadActivities_LeadId ON LeadActivities (LeadId);
                CREATE INDEX IX_LeadActivities_LeadId_CreatedAt ON LeadActivities (LeadId, CreatedAt);
                CREATE INDEX IX_LeadActivities_UserId ON LeadActivities (UserId);
            END

            IF OBJECT_ID('FunnelEvents', 'U') IS NULL
            BEGIN
                CREATE TABLE FunnelEvents (
                    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    LeadId INT NOT NULL,
                    Stage NVARCHAR(20) NOT NULL,
                    Timestamp DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                    UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
                );

                CREATE INDEX IX_FunnelEvents_LeadId ON FunnelEvents (LeadId);
                CREATE INDEX IX_FunnelEvents_Stage ON FunnelEvents (Stage);
            END

            IF OBJECT_ID('LeadFollowUpJobs', 'U') IS NULL
            BEGIN
                CREATE TABLE LeadFollowUpJobs (
                    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    LeadId INT NOT NULL,
                    FollowUpType NVARCHAR(30) NOT NULL,
                    DueAt DATETIME2 NOT NULL,
                    Status NVARCHAR(20) NOT NULL DEFAULT 'Pending',
                    AttemptCount INT NOT NULL DEFAULT 0,
                    ProcessedAt DATETIME2 NULL,
                    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                    UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
                );

                CREATE INDEX IX_LeadFollowUpJobs_DueAt ON LeadFollowUpJobs (DueAt);
                CREATE INDEX IX_LeadFollowUpJobs_Status_DueAt ON LeadFollowUpJobs (Status, DueAt);
            END

            IF EXISTS (SELECT 1 FROM DemoBookings WHERE Status = 'Booked')
            BEGIN
                UPDATE DemoBookings
                SET Status = 'Confirmed'
                WHERE Status = 'Booked';
            END

            UPDATE Leads
            SET Status = CASE FunnelStage
                WHEN 'Contacted' THEN 'Contacted'
                WHEN 'DemoBooked' THEN 'DemoBooked'
                WHEN 'Enrolled' THEN 'Converted'
                ELSE Status
            END
            WHERE (FunnelStage = 'Contacted' AND Status <> 'Contacted')
               OR (FunnelStage = 'DemoBooked' AND Status <> 'DemoBooked')
               OR (FunnelStage = 'Enrolled' AND Status <> 'Converted');

            UPDATE Leads
            SET Status = 'DemoBooked',
                FunnelStage = 'DemoBooked'
            WHERE EXISTS (
                SELECT 1
                FROM DemoBookings db
                WHERE db.LeadId = Leads.Id
                  AND db.Status IN ('Pending', 'Confirmed')
            )
              AND (Status <> 'DemoBooked' OR FunnelStage <> 'DemoBooked');
            """;

        try
        {
            await db.Database.ExecuteSqlRawAsync(sql);
        }
        catch (SqlException)
        {
            // Keep the app available when a database already contains a near-compatible legacy schema.
        }
    }

    private static async Task EnsureSqliteSchemaCompatibilityAsync(ApplicationDbContext db)
    {
        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync();
        }

        try
        {
            if (await TableExistsAsync(connection, "AppUsers") && !await TableExistsAsync(connection, "Users"))
            {
                await ExecuteNonQueryAsync(connection, "ALTER TABLE \"AppUsers\" RENAME TO \"Users\";");
            }

            if (await TableExistsAsync(connection, "LeadActivityLogs") && !await TableExistsAsync(connection, "LeadActivities"))
            {
                await ExecuteNonQueryAsync(connection, "ALTER TABLE \"LeadActivityLogs\" RENAME TO \"LeadActivities\";");
            }

            await ExecuteNonQueryAsync(connection, "CREATE TABLE IF NOT EXISTS \"Users\" (\"Id\" INTEGER NOT NULL CONSTRAINT \"PK_Users\" PRIMARY KEY AUTOINCREMENT, \"FullName\" TEXT NOT NULL, \"Email\" TEXT NOT NULL, \"PasswordHash\" TEXT NOT NULL, \"Role\" TEXT NOT NULL, \"IsActive\" INTEGER NOT NULL DEFAULT 1, \"FailedLoginAttempts\" INTEGER NOT NULL DEFAULT 0, \"LockoutEndUtc\" TEXT NULL, \"LastLoginAtUtc\" TEXT NULL, \"CreatedAt\" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP, \"UpdatedAt\" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP);");
            await ExecuteNonQueryAsync(connection, "CREATE TABLE IF NOT EXISTS \"Leads\" (\"Id\" INTEGER NOT NULL CONSTRAINT \"PK_Leads\" PRIMARY KEY AUTOINCREMENT, \"Name\" TEXT NOT NULL, \"Phone\" TEXT NOT NULL, \"Email\" TEXT NOT NULL, \"Location\" TEXT NOT NULL, \"InterestedDomain\" TEXT NOT NULL, \"Source\" TEXT NOT NULL, \"Status\" TEXT NOT NULL DEFAULT 'New', \"Score\" TEXT NOT NULL DEFAULT 'Warm', \"Notes\" TEXT NULL, \"FunnelStage\" TEXT NOT NULL DEFAULT 'New', \"AssignedStaffId\" INTEGER NULL, \"CreatedAt\" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP, \"UpdatedAt\" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP);");
            await ExecuteNonQueryAsync(connection, "CREATE TABLE IF NOT EXISTS \"DemoBookings\" (\"Id\" INTEGER NOT NULL CONSTRAINT \"PK_DemoBookings\" PRIMARY KEY AUTOINCREMENT, \"LeadId\" INTEGER NOT NULL, \"Day\" TEXT NOT NULL, \"Slot\" TEXT NOT NULL, \"Status\" TEXT NOT NULL DEFAULT 'Pending', \"CreatedAt\" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP, \"UpdatedAt\" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP);");
            await ExecuteNonQueryAsync(connection, "CREATE TABLE IF NOT EXISTS \"LeadActivities\" (\"Id\" INTEGER NOT NULL CONSTRAINT \"PK_LeadActivities\" PRIMARY KEY AUTOINCREMENT, \"LeadId\" INTEGER NOT NULL, \"UserId\" INTEGER NULL, \"ActivityType\" TEXT NOT NULL, \"Message\" TEXT NOT NULL, \"Status\" TEXT NOT NULL, \"CreatedAt\" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP, \"UpdatedAt\" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP);");
            await ExecuteNonQueryAsync(connection, "CREATE TABLE IF NOT EXISTS \"FunnelEvents\" (\"Id\" INTEGER NOT NULL CONSTRAINT \"PK_FunnelEvents\" PRIMARY KEY AUTOINCREMENT, \"LeadId\" INTEGER NOT NULL, \"Stage\" TEXT NOT NULL, \"Timestamp\" TEXT NOT NULL, \"CreatedAt\" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP, \"UpdatedAt\" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP);");
            await ExecuteNonQueryAsync(connection, "CREATE TABLE IF NOT EXISTS \"LeadFollowUpJobs\" (\"Id\" INTEGER NOT NULL CONSTRAINT \"PK_LeadFollowUpJobs\" PRIMARY KEY AUTOINCREMENT, \"LeadId\" INTEGER NOT NULL, \"FollowUpType\" TEXT NOT NULL, \"DueAt\" TEXT NOT NULL, \"Status\" TEXT NOT NULL DEFAULT 'Pending', \"AttemptCount\" INTEGER NOT NULL DEFAULT 0, \"ProcessedAt\" TEXT NULL, \"CreatedAt\" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP, \"UpdatedAt\" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP);");
            await ExecuteNonQueryAsync(connection, "CREATE TABLE IF NOT EXISTS \"ChatSessions\" (\"Id\" INTEGER NOT NULL CONSTRAINT \"PK_ChatSessions\" PRIMARY KEY AUTOINCREMENT, \"SessionId\" TEXT NOT NULL, \"Stage\" TEXT NOT NULL, \"Name\" TEXT NULL, \"Phone\" TEXT NULL, \"Email\" TEXT NULL, \"Location\" TEXT NULL, \"Domain\" TEXT NULL, \"LeadCaptured\" INTEGER NOT NULL DEFAULT 0, \"Source\" TEXT NOT NULL, \"CreatedAt\" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP, \"UpdatedAt\" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP);");
            await ExecuteNonQueryAsync(connection, "CREATE TABLE IF NOT EXISTS \"ChatMessages\" (\"Id\" INTEGER NOT NULL CONSTRAINT \"PK_ChatMessages\" PRIMARY KEY AUTOINCREMENT, \"ChatSessionId\" INTEGER NOT NULL, \"Sender\" TEXT NOT NULL, \"Content\" TEXT NOT NULL, \"CreatedAt\" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP, \"UpdatedAt\" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP);");
            await ExecuteNonQueryAsync(connection, "CREATE TABLE IF NOT EXISTS \"EmailAutomationLogs\" (\"Id\" INTEGER NOT NULL CONSTRAINT \"PK_EmailAutomationLogs\" PRIMARY KEY AUTOINCREMENT, \"LeadId\" INTEGER NOT NULL, \"TemplateKey\" TEXT NOT NULL, \"Subject\" TEXT NOT NULL, \"Status\" TEXT NOT NULL, \"CreatedAt\" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP, \"UpdatedAt\" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP);");
            await ExecuteNonQueryAsync(connection, "CREATE TABLE IF NOT EXISTS \"WhatsAppMessageLogs\" (\"Id\" INTEGER NOT NULL CONSTRAINT \"PK_WhatsAppMessageLogs\" PRIMARY KEY AUTOINCREMENT, \"LeadId\" INTEGER NOT NULL, \"MessageType\" TEXT NOT NULL, \"Phone\" TEXT NOT NULL, \"Status\" TEXT NOT NULL, \"CreatedAt\" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP, \"UpdatedAt\" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP);");

            await EnsureColumnAsync(connection, "Users", "FailedLoginAttempts", "INTEGER NOT NULL DEFAULT 0");
            await EnsureColumnAsync(connection, "Users", "LockoutEndUtc", "TEXT NULL");
            await EnsureColumnAsync(connection, "Users", "LastLoginAtUtc", "TEXT NULL");
            await EnsureColumnAsync(connection, "Users", "CreatedAt", "TEXT NULL");
            await EnsureColumnAsync(connection, "Users", "UpdatedAt", "TEXT NULL");
            await EnsureColumnAsync(connection, "Leads", "Status", "TEXT NOT NULL DEFAULT 'New'");
            await EnsureColumnAsync(connection, "Leads", "Score", "TEXT NOT NULL DEFAULT 'Warm'");
            await EnsureColumnAsync(connection, "Leads", "Notes", "TEXT NULL");
            await EnsureColumnAsync(connection, "Leads", "FunnelStage", "TEXT NOT NULL DEFAULT 'New'");
            await EnsureColumnAsync(connection, "Leads", "AssignedStaffId", "INTEGER NULL");
            await EnsureColumnAsync(connection, "Leads", "CreatedAt", "TEXT NULL");
            await EnsureColumnAsync(connection, "Leads", "UpdatedAt", "TEXT NULL");
            await EnsureColumnAsync(connection, "DemoBookings", "Status", "TEXT NOT NULL DEFAULT 'Pending'");
            await EnsureColumnAsync(connection, "DemoBookings", "CreatedAt", "TEXT NULL");
            await EnsureColumnAsync(connection, "DemoBookings", "UpdatedAt", "TEXT NULL");
            await EnsureColumnAsync(connection, "LeadActivities", "UserId", "INTEGER NULL");
            await EnsureColumnAsync(connection, "LeadActivities", "CreatedAt", "TEXT NULL");
            await EnsureColumnAsync(connection, "LeadActivities", "UpdatedAt", "TEXT NULL");
            await EnsureColumnAsync(connection, "FunnelEvents", "CreatedAt", "TEXT NULL");
            await EnsureColumnAsync(connection, "FunnelEvents", "UpdatedAt", "TEXT NULL");
            await EnsureColumnAsync(connection, "LeadFollowUpJobs", "AttemptCount", "INTEGER NOT NULL DEFAULT 0");
            await EnsureColumnAsync(connection, "LeadFollowUpJobs", "ProcessedAt", "TEXT NULL");
            await EnsureColumnAsync(connection, "LeadFollowUpJobs", "CreatedAt", "TEXT NULL");
            await EnsureColumnAsync(connection, "LeadFollowUpJobs", "UpdatedAt", "TEXT NULL");
            await EnsureColumnAsync(connection, "ChatSessions", "CreatedAt", "TEXT NULL");
            await EnsureColumnAsync(connection, "ChatSessions", "UpdatedAt", "TEXT NULL");
            await EnsureColumnAsync(connection, "ChatMessages", "CreatedAt", "TEXT NULL");
            await EnsureColumnAsync(connection, "ChatMessages", "UpdatedAt", "TEXT NULL");
            await EnsureColumnAsync(connection, "EmailAutomationLogs", "CreatedAt", "TEXT NULL");
            await EnsureColumnAsync(connection, "EmailAutomationLogs", "UpdatedAt", "TEXT NULL");
            await EnsureColumnAsync(connection, "WhatsAppMessageLogs", "CreatedAt", "TEXT NULL");
            await EnsureColumnAsync(connection, "WhatsAppMessageLogs", "UpdatedAt", "TEXT NULL");

            foreach (var auditTable in new[]
            {
                "Users",
                "Leads",
                "DemoBookings",
                "LeadActivities",
                "FunnelEvents",
                "LeadFollowUpJobs",
                "ChatSessions",
                "ChatMessages",
                "EmailAutomationLogs",
                "WhatsAppMessageLogs"
            })
            {
                await BackfillAuditColumnsAsync(connection, auditTable);
            }

            await EnsureIndexAsync(connection, "IX_Users_Email", "CREATE UNIQUE INDEX \"IX_Users_Email\" ON \"Users\" (\"Email\");");
            await EnsureIndexAsync(connection, "IX_Users_Role_IsActive", "CREATE INDEX \"IX_Users_Role_IsActive\" ON \"Users\" (\"Role\", \"IsActive\");");
            await EnsureIndexAsync(connection, "IX_Leads_Email", "CREATE UNIQUE INDEX \"IX_Leads_Email\" ON \"Leads\" (\"Email\");");
            await EnsureIndexAsync(connection, "IX_Leads_Phone", "CREATE UNIQUE INDEX \"IX_Leads_Phone\" ON \"Leads\" (\"Phone\");");
            await EnsureIndexAsync(connection, "IX_Leads_CreatedAt", "CREATE INDEX \"IX_Leads_CreatedAt\" ON \"Leads\" (\"CreatedAt\");");
            await EnsureIndexAsync(connection, "IX_Leads_Status", "CREATE INDEX \"IX_Leads_Status\" ON \"Leads\" (\"Status\");");
            await EnsureIndexAsync(connection, "IX_Leads_Source", "CREATE INDEX \"IX_Leads_Source\" ON \"Leads\" (\"Source\");");
            await EnsureIndexAsync(connection, "IX_Leads_InterestedDomain", "CREATE INDEX \"IX_Leads_InterestedDomain\" ON \"Leads\" (\"InterestedDomain\");");
            await EnsureIndexAsync(connection, "IX_Leads_Status_CreatedAt", "CREATE INDEX \"IX_Leads_Status_CreatedAt\" ON \"Leads\" (\"Status\", \"CreatedAt\");");
            await EnsureIndexAsync(connection, "IX_Leads_AssignedStaffId", "CREATE INDEX \"IX_Leads_AssignedStaffId\" ON \"Leads\" (\"AssignedStaffId\");");
            await EnsureIndexAsync(connection, "IX_DemoBookings_LeadId", "CREATE INDEX \"IX_DemoBookings_LeadId\" ON \"DemoBookings\" (\"LeadId\");");
            await EnsureIndexAsync(connection, "IX_DemoBookings_Day_Slot_Status", "CREATE INDEX \"IX_DemoBookings_Day_Slot_Status\" ON \"DemoBookings\" (\"Day\", \"Slot\", \"Status\");");
            await EnsureIndexAsync(connection, "IX_LeadActivities_LeadId", "CREATE INDEX \"IX_LeadActivities_LeadId\" ON \"LeadActivities\" (\"LeadId\");");
            await EnsureIndexAsync(connection, "IX_LeadActivities_LeadId_CreatedAt", "CREATE INDEX \"IX_LeadActivities_LeadId_CreatedAt\" ON \"LeadActivities\" (\"LeadId\", \"CreatedAt\");");
            await EnsureIndexAsync(connection, "IX_LeadActivities_UserId", "CREATE INDEX \"IX_LeadActivities_UserId\" ON \"LeadActivities\" (\"UserId\");");
            await EnsureIndexAsync(connection, "IX_FunnelEvents_LeadId", "CREATE INDEX \"IX_FunnelEvents_LeadId\" ON \"FunnelEvents\" (\"LeadId\");");
            await EnsureIndexAsync(connection, "IX_FunnelEvents_Stage", "CREATE INDEX \"IX_FunnelEvents_Stage\" ON \"FunnelEvents\" (\"Stage\");");
            await EnsureIndexAsync(connection, "IX_LeadFollowUpJobs_DueAt", "CREATE INDEX \"IX_LeadFollowUpJobs_DueAt\" ON \"LeadFollowUpJobs\" (\"DueAt\");");
            await EnsureIndexAsync(connection, "IX_LeadFollowUpJobs_Status_DueAt", "CREATE INDEX \"IX_LeadFollowUpJobs_Status_DueAt\" ON \"LeadFollowUpJobs\" (\"Status\", \"DueAt\");");
            await EnsureIndexAsync(connection, "IX_ChatSessions_SessionId", "CREATE UNIQUE INDEX \"IX_ChatSessions_SessionId\" ON \"ChatSessions\" (\"SessionId\");");
            await EnsureIndexAsync(connection, "IX_ChatMessages_ChatSessionId", "CREATE INDEX \"IX_ChatMessages_ChatSessionId\" ON \"ChatMessages\" (\"ChatSessionId\");");

            if (await TableExistsAsync(connection, "DemoBookings") && await ColumnExistsAsync(connection, "DemoBookings", "Status"))
            {
                await ExecuteNonQueryAsync(connection, "UPDATE \"DemoBookings\" SET \"Status\" = 'Confirmed' WHERE \"Status\" = 'Booked';");
            }

            if (await TableExistsAsync(connection, "Leads") && await ColumnExistsAsync(connection, "Leads", "FunnelStage") && await ColumnExistsAsync(connection, "Leads", "Status"))
            {
                await ExecuteNonQueryAsync(connection, "UPDATE \"Leads\" SET \"Status\" = CASE \"FunnelStage\" WHEN 'Contacted' THEN 'Contacted' WHEN 'DemoBooked' THEN 'DemoBooked' WHEN 'Enrolled' THEN 'Converted' ELSE \"Status\" END WHERE (\"FunnelStage\" = 'Contacted' AND \"Status\" <> 'Contacted') OR (\"FunnelStage\" = 'DemoBooked' AND \"Status\" <> 'DemoBooked') OR (\"FunnelStage\" = 'Enrolled' AND \"Status\" <> 'Converted');");

                if (await TableExistsAsync(connection, "DemoBookings"))
                {
                    await ExecuteNonQueryAsync(connection, "UPDATE \"Leads\" SET \"Status\" = 'DemoBooked', \"FunnelStage\" = 'DemoBooked' WHERE EXISTS (SELECT 1 FROM \"DemoBookings\" db WHERE db.\"LeadId\" = \"Leads\".\"Id\" AND db.\"Status\" IN ('Pending', 'Confirmed')) AND (\"Status\" <> 'DemoBooked' OR \"FunnelStage\" <> 'DemoBooked');");
                }
            }
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static async Task EnsureColumnAsync(DbConnection connection, string tableName, string columnName, string columnDefinition)
    {
        if (!await TableExistsAsync(connection, tableName) || await ColumnExistsAsync(connection, tableName, columnName))
        {
            return;
        }

        await ExecuteNonQueryAsync(connection, $"ALTER TABLE {QuoteIdentifier(tableName)} ADD COLUMN {QuoteIdentifier(columnName)} {columnDefinition};");
    }

    private static async Task EnsureIndexAsync(DbConnection connection, string indexName, string createSql)
    {
        if (await IndexExistsAsync(connection, indexName))
        {
            return;
        }

        await ExecuteNonQueryAsync(connection, createSql);
    }

    private static async Task BackfillAuditColumnsAsync(DbConnection connection, string tableName)
    {
        if (!await TableExistsAsync(connection, tableName))
        {
            return;
        }

        var hasCreatedAt = await ColumnExistsAsync(connection, tableName, "CreatedAt");
        var hasUpdatedAt = await ColumnExistsAsync(connection, tableName, "UpdatedAt");
        if (!hasCreatedAt && !hasUpdatedAt)
        {
            return;
        }

        var assignments = new List<string>();
        if (hasCreatedAt)
        {
            assignments.Add("\"CreatedAt\" = COALESCE(\"CreatedAt\", CURRENT_TIMESTAMP)");
        }

        if (hasUpdatedAt)
        {
            assignments.Add("\"UpdatedAt\" = COALESCE(\"UpdatedAt\", COALESCE(\"CreatedAt\", CURRENT_TIMESTAMP))");
        }

        if (assignments.Count == 0)
        {
            return;
        }

        await ExecuteNonQueryAsync(
            connection,
            $"""UPDATE {QuoteIdentifier(tableName)} SET {string.Join(", ", assignments)};""");
    }

    private static async Task<bool> TableExistsAsync(DbConnection connection, string tableName)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = $name LIMIT 1;";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "$name";
        parameter.Value = tableName;
        command.Parameters.Add(parameter);

        return await command.ExecuteScalarAsync() is not null;
    }

    private static async Task<bool> ColumnExistsAsync(DbConnection connection, string tableName, string columnName)
    {
        if (!await TableExistsAsync(connection, tableName))
        {
            return false;
        }

        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({QuoteIdentifier(tableName)});";
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            if (string.Equals(reader["name"]?.ToString(), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static async Task<bool> IndexExistsAsync(DbConnection connection, string indexName)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'index' AND name = $name LIMIT 1;";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "$name";
        parameter.Value = indexName;
        command.Parameters.Add(parameter);

        return await command.ExecuteScalarAsync() is not null;
    }

    private static async Task<bool> MigrationExistsAsync(DbConnection connection, string migrationId)
    {
        if (!await TableExistsAsync(connection, "__EFMigrationsHistory"))
        {
            return false;
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = $migrationId LIMIT 1;""";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "$migrationId";
        parameter.Value = migrationId;
        command.Parameters.Add(parameter);

        return await command.ExecuteScalarAsync() is not null;
    }

    private static async Task ExecuteNonQueryAsync(DbConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private static string QuoteIdentifier(string identifier) => $"\"{identifier.Replace("\"", "\"\"")}\"";
}
