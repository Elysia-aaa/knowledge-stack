-- Idempotent, additive migration. Run against the existing project database.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF COL_LENGTH('dbo.Users','IsAdmin') IS NULL ALTER TABLE dbo.Users ADD IsAdmin bit NOT NULL CONSTRAINT DF_Users_IsAdmin DEFAULT 0;
IF COL_LENGTH('dbo.Posts','Status') IS NULL ALTER TABLE dbo.Posts ADD Status nvarchar(12) NOT NULL CONSTRAINT DF_Posts_Status DEFAULT N'Published';
IF COL_LENGTH('dbo.Posts','DeletedAt') IS NULL ALTER TABLE dbo.Posts ADD DeletedAt datetime NULL;
IF COL_LENGTH('dbo.Posts','IsHidden') IS NULL ALTER TABLE dbo.Posts ADD IsHidden bit NOT NULL CONSTRAINT DF_Posts_IsHidden DEFAULT 0;
IF OBJECT_ID('dbo.Comments','U') IS NULL
CREATE TABLE dbo.Comments (
 CommentId int IDENTITY PRIMARY KEY, PostId int NOT NULL REFERENCES dbo.Posts(PostId),
 UserId int NOT NULL REFERENCES dbo.Users(UserId), ParentId int NULL REFERENCES dbo.Comments(CommentId),
 Content nvarchar(1000) NOT NULL, IsDeleted bit NOT NULL DEFAULT 0, IsHidden bit NOT NULL DEFAULT 0,
 CreatedAt datetime NOT NULL DEFAULT GETDATE()
);
IF OBJECT_ID('dbo.PostImages','U') IS NULL
CREATE TABLE dbo.PostImages (
 ImageId int IDENTITY PRIMARY KEY, UserId int NOT NULL REFERENCES dbo.Users(UserId),
 PostId int NULL REFERENCES dbo.Posts(PostId), IsAvatar bit NOT NULL DEFAULT 0,
 ContentType varchar(30) NOT NULL, ImageData varbinary(max) NOT NULL,
 SizeBytes int NOT NULL, CreatedAt datetime NOT NULL DEFAULT GETDATE()
);
IF OBJECT_ID('dbo.Follows','U') IS NULL
CREATE TABLE dbo.Follows (
 FollowerId int NOT NULL REFERENCES dbo.Users(UserId), FolloweeId int NOT NULL REFERENCES dbo.Users(UserId),
 CreatedAt datetime NOT NULL DEFAULT GETDATE(), PRIMARY KEY(FollowerId,FolloweeId), CHECK(FollowerId<>FolloweeId)
);
IF OBJECT_ID('dbo.Favorites','U') IS NULL
CREATE TABLE dbo.Favorites (
 UserId int NOT NULL REFERENCES dbo.Users(UserId), PostId int NOT NULL REFERENCES dbo.Posts(PostId),
 CreatedAt datetime NOT NULL DEFAULT GETDATE(), PRIMARY KEY(UserId,PostId)
);
IF OBJECT_ID('dbo.Reposts','U') IS NULL
CREATE TABLE dbo.Reposts (
 RepostId int IDENTITY PRIMARY KEY, UserId int NOT NULL REFERENCES dbo.Users(UserId),
 PostId int NULL REFERENCES dbo.Posts(PostId), Note nvarchar(300) NOT NULL DEFAULT N'',
 IsHidden bit NOT NULL DEFAULT 0, CreatedAt datetime NOT NULL DEFAULT GETDATE()
);
IF OBJECT_ID('dbo.Notifications','U') IS NULL
CREATE TABLE dbo.Notifications (
 NotificationId int IDENTITY PRIMARY KEY, UserId int NOT NULL REFERENCES dbo.Users(UserId),
 ActorId int NOT NULL REFERENCES dbo.Users(UserId), Kind varchar(20) NOT NULL,
 PostId int NULL, EventKey varchar(100) NOT NULL, IsRead bit NOT NULL DEFAULT 0,
 CreatedAt datetime NOT NULL DEFAULT GETDATE(), UNIQUE(UserId,EventKey)
);
IF OBJECT_ID('dbo.SearchEvents','U') IS NULL
CREATE TABLE dbo.SearchEvents (
 SearchId int IDENTITY PRIMARY KEY, UserId int NOT NULL REFERENCES dbo.Users(UserId),
 Keyword nvarchar(50) NOT NULL, SearchDate date NOT NULL DEFAULT CAST(GETDATE() AS date),
 CreatedAt datetime NOT NULL DEFAULT GETDATE(), UNIQUE(UserId,Keyword,SearchDate)
);
IF OBJECT_ID('dbo.Reports','U') IS NULL
CREATE TABLE dbo.Reports (
 ReportId int IDENTITY PRIMARY KEY, UserId int NOT NULL REFERENCES dbo.Users(UserId),
 TargetType varchar(12) NOT NULL, TargetId int NOT NULL, Reason nvarchar(500) NOT NULL,
 State varchar(12) NOT NULL DEFAULT 'Pending', Decision nvarchar(500) NULL,
 ReviewerId int NULL REFERENCES dbo.Users(UserId), CreatedAt datetime NOT NULL DEFAULT GETDATE(),
 ReviewedAt datetime NULL
);
IF OBJECT_ID('dbo.PostViews','U') IS NULL
CREATE TABLE dbo.PostViews (
 PostId int NOT NULL REFERENCES dbo.Posts(PostId), ViewerKey varchar(70) NOT NULL,
 ViewDate date NOT NULL DEFAULT CAST(GETDATE() AS date), PRIMARY KEY(PostId,ViewerKey,ViewDate)
);
IF OBJECT_ID('dbo.ModerationActions','U') IS NULL
CREATE TABLE dbo.ModerationActions (
 ActionId int IDENTITY PRIMARY KEY, ReportId int NOT NULL REFERENCES dbo.Reports(ReportId),
 AdminId int NOT NULL REFERENCES dbo.Users(UserId), Verdict varchar(12) NOT NULL,
 Note nvarchar(500) NOT NULL, CreatedAt datetime NOT NULL DEFAULT GETDATE()
);
COMMIT TRANSACTION;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name='IX_Posts_Public' AND object_id=OBJECT_ID('dbo.Posts'))
CREATE INDEX IX_Posts_Public ON dbo.Posts(Status,IsHidden,DeletedAt,CreatedAt DESC,PostId DESC);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name='IX_Comments_Post' AND object_id=OBJECT_ID('dbo.Comments'))
CREATE INDEX IX_Comments_Post ON dbo.Comments(PostId,CreatedAt,CommentId);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name='IX_Notifications_User' AND object_id=OBJECT_ID('dbo.Notifications'))
CREATE INDEX IX_Notifications_User ON dbo.Notifications(UserId,IsRead,CreatedAt DESC);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name='IX_Reposts_Unique' AND object_id=OBJECT_ID('dbo.Reposts'))
CREATE UNIQUE INDEX IX_Reposts_Unique ON dbo.Reposts(UserId,PostId) WHERE PostId IS NOT NULL;
