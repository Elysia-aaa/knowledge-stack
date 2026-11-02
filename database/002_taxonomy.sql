-- Fixed disciplines and multi-category associations. Safe to run repeatedly.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @firstRun bit=CASE WHEN OBJECT_ID('dbo.PostCategories','U') IS NULL THEN 1 ELSE 0 END;
IF OBJECT_ID('dbo.Categories','U') IS NULL
CREATE TABLE dbo.Categories (
 CategoryId int IDENTITY PRIMARY KEY, Name nvarchar(30) NOT NULL UNIQUE, SortOrder int NOT NULL
);
INSERT dbo.Categories(Name,SortOrder)
SELECT v.Name,v.SortOrder FROM (VALUES
 (N'计算机',1),(N'数学',2),(N'物理',3),(N'化学',4),(N'生命科学',5),(N'医学',6),
 (N'工程技术',7),(N'经济金融',8),(N'管理',9),(N'法学',10),(N'教育',11),(N'文学',12),
 (N'历史',13),(N'哲学',14),(N'农学',15),(N'艺术',16),(N'其他',17)
) v(Name,SortOrder) WHERE NOT EXISTS(SELECT 1 FROM dbo.Categories c WHERE c.Name=v.Name);
IF OBJECT_ID('dbo.PostCategories','U') IS NULL
CREATE TABLE dbo.PostCategories (
 PostId int NOT NULL CONSTRAINT FK_PostCategories_Posts REFERENCES dbo.Posts(PostId) ON DELETE CASCADE,
 CategoryId int NOT NULL CONSTRAINT FK_PostCategories_Categories REFERENCES dbo.Categories(CategoryId),
 CONSTRAINT PK_PostCategories PRIMARY KEY(PostId,CategoryId)
);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.PostCategories') AND name='IX_PostCategories_Category')
CREATE INDEX IX_PostCategories_Category ON dbo.PostCategories(CategoryId,PostId);
ALTER TABLE dbo.Posts ALTER COLUMN Category nvarchar(250) NULL;
ALTER TABLE dbo.Posts ALTER COLUMN Tags nvarchar(250) NULL;
-- Import legacy free text only once; repeat runs must not fill empty draft categories.
IF @firstRun=1
BEGIN
 INSERT dbo.PostCategories(PostId,CategoryId)
 SELECT p.PostId,c.CategoryId FROM dbo.Posts p
 JOIN dbo.Categories c ON c.Name=CASE
  WHEN EXISTS(SELECT 1 FROM dbo.Categories x WHERE x.Name=p.Category) THEN p.Category
  WHEN ISNULL(p.Title,N'')+ISNULL(p.Content,N'') LIKE N'%ASP.NET%'
    OR ISNULL(p.Title,N'')+ISNULL(p.Content,N'') LIKE N'%Markdown%'
    OR ISNULL(p.Category,N'') IN(N'前端',N'数据库',N'ASP.NET') THEN N'计算机'
  ELSE N'其他' END
 WHERE p.Status=N'Published' OR NULLIF(LTRIM(RTRIM(p.Category)),N'') IS NOT NULL;
 UPDATE p SET Category=ISNULL(STUFF((SELECT N'|'+c.Name FROM dbo.PostCategories pc JOIN dbo.Categories c ON c.CategoryId=pc.CategoryId WHERE pc.PostId=p.PostId ORDER BY c.SortOrder,c.CategoryId FOR XML PATH(''),TYPE).value('.','nvarchar(max)'),1,1,N''),N'') FROM dbo.Posts p;
END;
COMMIT TRANSACTION;
