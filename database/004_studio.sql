-- Additive studio migration. Existing content and counters are preserved.
SET XACT_ABORT ON;
IF OBJECT_ID('dbo.UserAppearance','U') IS NULL
CREATE TABLE dbo.UserAppearance(UserId int PRIMARY KEY REFERENCES dbo.Users(UserId) ON DELETE CASCADE,ThemeKey varchar(16) NOT NULL DEFAULT 'light',CustomBase varchar(16) NOT NULL DEFAULT 'light',CustomJson nvarchar(max) NOT NULL DEFAULT N'{}',UpdatedAt datetime2 NOT NULL DEFAULT GETDATE());
IF OBJECT_ID('dbo.AnalyticsState','U') IS NULL
CREATE TABLE dbo.AnalyticsState(Id int PRIMARY KEY CHECK(Id=1),StartedAt datetime2 NOT NULL);
IF NOT EXISTS(SELECT 1 FROM dbo.AnalyticsState WHERE Id=1)
INSERT dbo.AnalyticsState VALUES(1,DATEADD(hour,8,GETUTCDATE()));
IF OBJECT_ID('dbo.ReadingDaily','U') IS NULL
CREATE TABLE dbo.ReadingDaily(PostId int NOT NULL REFERENCES dbo.Posts(PostId) ON DELETE CASCADE,ReaderKey varchar(70) NOT NULL,ReadingDate date NOT NULL,ActiveSeconds int NOT NULL DEFAULT 0 CHECK(ActiveSeconds BETWEEN 0 AND 86400),Completed bit NOT NULL DEFAULT 0,LastHeartbeatAt datetime2 NOT NULL DEFAULT DATEADD(hour,8,GETUTCDATE()),PRIMARY KEY(PostId,ReaderKey,ReadingDate));
IF OBJECT_ID('dbo.CreatorEvents','U') IS NULL
CREATE TABLE dbo.CreatorEvents(EventId bigint IDENTITY PRIMARY KEY,OwnerId int NOT NULL REFERENCES dbo.Users(UserId) ON DELETE CASCADE,PostId int NULL REFERENCES dbo.Posts(PostId) ON DELETE CASCADE,ActorId int NOT NULL,Kind varchar(16) NOT NULL,Delta int NOT NULL CHECK(Delta IN(-1,1)),OccurredAt datetime2 NOT NULL DEFAULT DATEADD(hour,8,GETUTCDATE()));
IF OBJECT_ID('dbo.PostHeatDaily','U') IS NULL
CREATE TABLE dbo.PostHeatDaily(PostId int NOT NULL REFERENCES dbo.Posts(PostId) ON DELETE CASCADE,SnapshotDate date NOT NULL,Heat int NOT NULL,PRIMARY KEY(PostId,SnapshotDate));
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.CreatorEvents') AND name='IX_CreatorEvents_OwnerDate')
CREATE INDEX IX_CreatorEvents_OwnerDate ON dbo.CreatorEvents(OwnerId,OccurredAt) INCLUDE(PostId,Kind,Delta,ActorId);
-- Establish a real current baseline, never synthetic historical points.
INSERT PostHeatDaily(PostId,SnapshotDate,Heat)
SELECT p.PostId,CAST(DATEADD(hour,8,GETUTCDATE()) AS date),
 (SELECT COUNT(*) FROM Likes WHERE PostId=p.PostId)*3+(SELECT COUNT(*) FROM Favorites WHERE PostId=p.PostId)*4+(SELECT COUNT(*) FROM Comments WHERE PostId=p.PostId AND IsDeleted=0 AND IsHidden=0)*2
FROM Posts p WHERE NOT EXISTS(SELECT 1 FROM PostHeatDaily h WHERE h.PostId=p.PostId AND h.SnapshotDate=CAST(DATEADD(hour,8,GETUTCDATE()) AS date));

GO
-- All existing write paths (including LikeDAL) are captured by database triggers.
CREATE OR ALTER TRIGGER dbo.TR_Studio_Likes ON dbo.Likes AFTER INSERT,DELETE AS
BEGIN
 SET NOCOUNT ON;
 INSERT dbo.CreatorEvents(OwnerId,PostId,ActorId,Kind,Delta)
 SELECT p.UserId,i.PostId,i.UserId,'like',1 FROM inserted i JOIN dbo.Posts p ON p.PostId=i.PostId
 UNION ALL SELECT p.UserId,d.PostId,d.UserId,'like',-1 FROM deleted d JOIN dbo.Posts p ON p.PostId=d.PostId;
 DECLARE @day date=CAST(DATEADD(hour,8,GETUTCDATE()) AS date);
 ;WITH changed AS(SELECT PostId FROM inserted UNION SELECT PostId FROM deleted), scores AS(
 SELECT p.PostId,(SELECT COUNT(*) FROM Likes WHERE PostId=p.PostId)*3+(SELECT COUNT(*) FROM Favorites WHERE PostId=p.PostId)*4+(SELECT COUNT(*) FROM Comments WHERE PostId=p.PostId AND IsDeleted=0 AND IsHidden=0)*2 AS Heat FROM Posts p JOIN changed c ON c.PostId=p.PostId)
 MERGE PostHeatDaily WITH(HOLDLOCK) AS t USING scores s ON t.PostId=s.PostId AND t.SnapshotDate=@day WHEN MATCHED THEN UPDATE SET Heat=s.Heat WHEN NOT MATCHED THEN INSERT VALUES(s.PostId,@day,s.Heat);
END;
GO
CREATE OR ALTER TRIGGER dbo.TR_Studio_Favorites ON dbo.Favorites AFTER INSERT,DELETE AS
BEGIN
 SET NOCOUNT ON;
 INSERT CreatorEvents(OwnerId,PostId,ActorId,Kind,Delta)
 SELECT p.UserId,i.PostId,i.UserId,'favorite',1 FROM inserted i JOIN Posts p ON p.PostId=i.PostId
 UNION ALL SELECT p.UserId,d.PostId,d.UserId,'favorite',-1 FROM deleted d JOIN Posts p ON p.PostId=d.PostId;
 DECLARE @day date=CAST(DATEADD(hour,8,GETUTCDATE()) AS date);
 ;WITH changed AS(SELECT PostId FROM inserted UNION SELECT PostId FROM deleted), scores AS(
 SELECT p.PostId,(SELECT COUNT(*) FROM Likes WHERE PostId=p.PostId)*3+(SELECT COUNT(*) FROM Favorites WHERE PostId=p.PostId)*4+(SELECT COUNT(*) FROM Comments WHERE PostId=p.PostId AND IsDeleted=0 AND IsHidden=0)*2 AS Heat FROM Posts p JOIN changed c ON c.PostId=p.PostId)
 MERGE PostHeatDaily WITH(HOLDLOCK) AS t USING scores s ON t.PostId=s.PostId AND t.SnapshotDate=@day WHEN MATCHED THEN UPDATE SET Heat=s.Heat WHEN NOT MATCHED THEN INSERT VALUES(s.PostId,@day,s.Heat);
END;
GO
CREATE OR ALTER TRIGGER dbo.TR_Studio_Comments ON dbo.Comments AFTER INSERT,UPDATE,DELETE AS
BEGIN
 SET NOCOUNT ON;
 INSERT CreatorEvents(OwnerId,PostId,ActorId,Kind,Delta)
 SELECT p.UserId,x.PostId,x.UserId,'comment',x.Delta FROM(
 SELECT i.PostId,i.UserId,1 Delta FROM inserted i LEFT JOIN deleted d ON i.CommentId=d.CommentId WHERE i.IsDeleted=0 AND i.IsHidden=0 AND (d.CommentId IS NULL OR d.IsDeleted=1 OR d.IsHidden=1)
 UNION ALL SELECT d.PostId,d.UserId,-1 FROM deleted d LEFT JOIN inserted i ON d.CommentId=i.CommentId WHERE d.IsDeleted=0 AND d.IsHidden=0 AND (i.CommentId IS NULL OR i.IsDeleted=1 OR i.IsHidden=1))x JOIN Posts p ON x.PostId=p.PostId;
 DECLARE @day date=CAST(DATEADD(hour,8,GETUTCDATE()) AS date);
 ;WITH changed AS(SELECT PostId FROM inserted UNION SELECT PostId FROM deleted), scores AS(
 SELECT p.PostId,(SELECT COUNT(*) FROM Likes WHERE PostId=p.PostId)*3+(SELECT COUNT(*) FROM Favorites WHERE PostId=p.PostId)*4+(SELECT COUNT(*) FROM Comments WHERE PostId=p.PostId AND IsDeleted=0 AND IsHidden=0)*2 AS Heat FROM Posts p JOIN changed c ON c.PostId=p.PostId)
 MERGE PostHeatDaily WITH(HOLDLOCK) AS t USING scores s ON t.PostId=s.PostId AND t.SnapshotDate=@day WHEN MATCHED THEN UPDATE SET Heat=s.Heat WHEN NOT MATCHED THEN INSERT VALUES(s.PostId,@day,s.Heat);
END;
GO
CREATE OR ALTER TRIGGER dbo.TR_Studio_Reposts ON dbo.Reposts AFTER INSERT,UPDATE,DELETE AS
BEGIN
 SET NOCOUNT ON;
 INSERT CreatorEvents(OwnerId,PostId,ActorId,Kind,Delta)
 SELECT p.UserId,x.PostId,x.UserId,'repost',x.Delta FROM(
 SELECT i.PostId,i.UserId,1 Delta FROM inserted i LEFT JOIN deleted d ON i.RepostId=d.RepostId WHERE i.PostId IS NOT NULL AND i.IsHidden=0 AND (d.RepostId IS NULL OR d.IsHidden=1)
 UNION ALL SELECT d.PostId,d.UserId,-1 FROM deleted d LEFT JOIN inserted i ON i.RepostId=d.RepostId WHERE d.PostId IS NOT NULL AND d.IsHidden=0 AND (i.RepostId IS NULL OR i.PostId IS NULL OR i.IsHidden=1))x JOIN Posts p ON p.PostId=x.PostId;
END;
GO
CREATE OR ALTER TRIGGER dbo.TR_Studio_Follows ON dbo.Follows AFTER INSERT,DELETE AS
BEGIN
 SET NOCOUNT ON;
 INSERT CreatorEvents(OwnerId,PostId,ActorId,Kind,Delta)
 SELECT FolloweeId,NULL,FollowerId,'follow',1 FROM inserted
 UNION ALL SELECT FolloweeId,NULL,FollowerId,'follow',-1 FROM deleted;
END;
