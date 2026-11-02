using System;
using System.Data.SqlClient;

namespace WebApplication2.DAL
{
    /// <summary>
    /// 点赞数据访问类，封装 Likes 表（Users 与 Posts 的多对多关联）的操作，
    /// 通过唯一约束 UNIQUE(UserId, PostId) 保证同一用户不能重复点赞同一篇文章。
    /// </summary>
    public static class LikeDAL
    {
        /// <summary>判断某用户是否已点赞某篇文章</summary>
        public static bool HasLiked(int userId, int postId)
        {
            object result = DbHelper.ExecuteScalar(
                "SELECT COUNT(*) FROM Likes WHERE UserId = @UserId AND PostId = @PostId",
                new SqlParameter("@UserId", userId),
                new SqlParameter("@PostId", postId));
            return Convert.ToInt32(result) > 0;
        }

        /// <summary>点赞</summary>
        public static void Add(int userId, int postId)
        {
            DbHelper.ExecuteNonQuery(
                "INSERT INTO Likes (UserId, PostId, CreatedAt) VALUES (@UserId, @PostId, GETDATE())",
                new SqlParameter("@UserId", userId),
                new SqlParameter("@PostId", postId));
        }

        /// <summary>取消点赞</summary>
        public static void Remove(int userId, int postId)
        {
            DbHelper.ExecuteNonQuery(
                "DELETE FROM Likes WHERE UserId = @UserId AND PostId = @PostId",
                new SqlParameter("@UserId", userId),
                new SqlParameter("@PostId", postId));
        }
    }
}
