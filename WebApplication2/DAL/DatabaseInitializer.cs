using System;
using System.Configuration;
using System.Data.SqlClient;
using WebApplication2.Utils;

namespace WebApplication2.DAL
{
    /// <summary>
    /// 数据库自动初始化：确保 KnowledgeStack 数据库、基础表与社区扩展表（列）就绪。
    /// 幂等，可重复执行，已有数据保留。应用启动（Global.asax）与 Setup.aspx 均调用此方法。
    /// </summary>
    public static class DatabaseInitializer
    {
        /// <summary>确保数据库及所有表/列就绪（保留已有数据）。</summary>
        public static void EnsureInitialized()
        {
            // 1. 先连接 master 创建数据库（若不存在）
            var configured = new SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["KnowledgeStackConn"].ConnectionString);
            string databaseName=configured.InitialCatalog;
            if(String.IsNullOrWhiteSpace(databaseName))throw new InvalidOperationException("请配置数据库名称");
            configured.InitialCatalog="master";string masterConn=configured.ConnectionString;
            using (SqlConnection conn = new SqlConnection(masterConn))
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DECLARE @create nvarchar(max)=N'CREATE DATABASE '+QUOTENAME(@name); IF DB_ID(@name) IS NULL EXEC(@create)";
                    cmd.Parameters.AddWithValue("@name",databaseName);
                    cmd.ExecuteNonQuery();
                }
            }

            // 2. 连接目标库创建基础表
            using (SqlConnection conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = CreateTablesSql;
                    cmd.ExecuteNonQuery();
                }
                SeedData(conn);
            }

            // 3. 社区扩展：补 Status/DeletedAt/IsHidden 等列，建分类、评论、点赞等表
            SchemaMigration.Apply();

            // 4. 保证至少存在一个管理员
            DbHelper.ExecuteNonQuery("UPDATE Users SET IsAdmin=1 WHERE Username=N'demo' AND NOT EXISTS(SELECT 1 FROM Users WHERE IsAdmin=1)");
        }

        /// <summary>插入示例数据（仅当 Users 表为空时）</summary>
        private static void SeedData(SqlConnection conn)
        {
            using (SqlCommand check = conn.CreateCommand())
            {
                check.CommandText = "SELECT COUNT(*) FROM Users";
                if (Convert.ToInt32(check.ExecuteScalar()) > 0)
                {
                    return; // 已有数据，跳过
                }
            }

            // 示例账号 demo / demo123（密码加密存储）
            string salt = SecurityHelper.GenerateSalt();
            string hash = SecurityHelper.HashPassword("demo123", salt);

            int userId;
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"INSERT INTO Users (Username, Email, PasswordHash, Salt, Nickname, Bio)
                                    VALUES (@u, @e, @h, @s, @n, @b);
                                    SELECT CAST(SCOPE_IDENTITY() AS INT);";
                cmd.Parameters.AddWithValue("@u", "demo");
                cmd.Parameters.AddWithValue("@e", "demo@example.com");
                cmd.Parameters.AddWithValue("@h", hash);
                cmd.Parameters.AddWithValue("@s", salt);
                cmd.Parameters.AddWithValue("@n", "示例用户");
                cmd.Parameters.AddWithValue("@b", "这是一个演示账号，密码为 demo123");
                userId = Convert.ToInt32(cmd.ExecuteScalar());
            }

            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"INSERT INTO Posts (UserId, Title, Summary, Content, Category, Tags)
                                    VALUES (@uid, @t, @s, @c, @cat, @tags)";
                cmd.Parameters.AddWithValue("@uid", userId);

                cmd.Parameters.AddWithValue("@t", "欢迎使用「我的知识小栈」");
                cmd.Parameters.AddWithValue("@s", "这是一个 ASP.NET WebForms + SQL Server 的个人博客/笔记系统示例。");
                cmd.Parameters.AddWithValue("@c", "# 欢迎！\n\n这是一个用 **Markdown** 编写的示例文章。\n\n- 支持标题、粗体、代码等语法\n- 点击下方「赞」试试点赞功能\n\n```csharp\nConsole.WriteLine(\"Hello, 我的知识小栈!\");\n```");
                cmd.Parameters.AddWithValue("@cat", "计算机");
                cmd.Parameters.AddWithValue("@tags", "ASP.NET WebForms SQLServer");
                cmd.ExecuteNonQuery();

                cmd.Parameters["@t"].Value = "Markdown 语法快速上手";
                cmd.Parameters["@s"].Value = "几行代码教你掌握 Markdown 常用语法。";
                cmd.Parameters["@c"].Value = "## 常用语法\n\n- `#` 一级标题\n- `##` 二级标题\n- `**粗体**` 加粗\n- `*斜体*` 斜体\n- `` `代码` `` 行内代码\n\n> 这是一段引用。";
                cmd.Parameters["@cat"].Value = "计算机";
                cmd.Parameters["@tags"].Value = "Markdown 教程";
                cmd.ExecuteNonQuery();
            }
        }

        private const string CreateTablesSql = @"
IF OBJECT_ID('dbo.Users', 'U') IS NULL
CREATE TABLE dbo.Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL UNIQUE,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(128) NOT NULL,
    Salt NVARCHAR(64) NOT NULL,
    Nickname NVARCHAR(50) NULL,
    Bio NVARCHAR(500) NULL,
    Avatar NVARCHAR(200) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
);

IF OBJECT_ID('dbo.Posts', 'U') IS NULL
CREATE TABLE dbo.Posts (
    PostId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    Summary NVARCHAR(500) NULL,
    Content NVARCHAR(MAX) NULL,
    Category NVARCHAR(50) NULL,
    Tags NVARCHAR(200) NULL,
    LikeCount INT NOT NULL DEFAULT 0,
    ViewCount INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME NULL,
    CONSTRAINT FK_Posts_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId)
);

IF OBJECT_ID('dbo.Likes', 'U') IS NULL
CREATE TABLE dbo.Likes (
    LikeId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    PostId INT NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Likes_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_Likes_Posts FOREIGN KEY (PostId) REFERENCES dbo.Posts(PostId),
    CONSTRAINT UQ_Likes UNIQUE (UserId, PostId)
);";
    }
}
