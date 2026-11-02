-- Idempotent comment/reply likes; emoji already uses the NVARCHAR content fields.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID('dbo.CommentLikes','U') IS NULL
CREATE TABLE dbo.CommentLikes (
 CommentId int NOT NULL CONSTRAINT FK_CommentLikes_Comments REFERENCES dbo.Comments(CommentId) ON DELETE CASCADE,
 UserId int NOT NULL CONSTRAINT FK_CommentLikes_Users REFERENCES dbo.Users(UserId),
 CreatedAt datetime NOT NULL CONSTRAINT DF_CommentLikes_CreatedAt DEFAULT GETDATE(),
 CONSTRAINT PK_CommentLikes PRIMARY KEY(CommentId,UserId)
);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.CommentLikes') AND name='IX_CommentLikes_User')
CREATE INDEX IX_CommentLikes_User ON dbo.CommentLikes(UserId,CommentId);
COMMIT TRANSACTION;
