using System;
using System.Data;
using System.Data.SqlClient;
using WebApplication2.Models;

namespace WebApplication2.DAL
{
    /// <summary>
    /// 用户数据访问类，封装 Users 表的所有增删查操作。
    /// </summary>
    public static class UserDAL
    {
        /// <summary>把 DataRow 转换成 User 实体对象</summary>
        private static User RowToUser(DataRow row)
        {
            return new User
            {
                UserId = Convert.ToInt32(row["UserId"]),
                Username = row["Username"].ToString(),
                Email = row["Email"].ToString(),
                PasswordHash = row["PasswordHash"].ToString(),
                Salt = row["Salt"].ToString(),
                Nickname = row["Nickname"] == DBNull.Value ? "" : row["Nickname"].ToString(),
                Bio = row["Bio"] == DBNull.Value ? "" : row["Bio"].ToString(),
                Avatar = row["Avatar"] == DBNull.Value ? "" : row["Avatar"].ToString(),
                CreatedAt = Convert.ToDateTime(row["CreatedAt"])
            };
        }

        /// <summary>按用户名查询用户</summary>
        public static User GetByUsername(string username)
        {
            DataTable dt = DbHelper.ExecuteDataTable(
                "SELECT * FROM Users WHERE Username = @Username",
                new SqlParameter("@Username", username));
            return dt.Rows.Count > 0 ? RowToUser(dt.Rows[0]) : null;
        }

        /// <summary>按邮箱查询用户</summary>
        public static User GetByEmail(string email)
        {
            DataTable dt = DbHelper.ExecuteDataTable(
                "SELECT * FROM Users WHERE Email = @Email",
                new SqlParameter("@Email", email));
            return dt.Rows.Count > 0 ? RowToUser(dt.Rows[0]) : null;
        }

        /// <summary>按主键 ID 查询用户（首页展示账号详细信息时使用）</summary>
        public static User GetById(int userId)
        {
            DataTable dt = DbHelper.ExecuteDataTable(
                "SELECT * FROM Users WHERE UserId = @UserId",
                new SqlParameter("@UserId", userId));
            return dt.Rows.Count > 0 ? RowToUser(dt.Rows[0]) : null;
        }

        /// <summary>判断用户名是否已被注册</summary>
        public static bool UsernameExists(string username)
        {
            object result = DbHelper.ExecuteScalar(
                "SELECT COUNT(*) FROM Users WHERE Username = @Username",
                new SqlParameter("@Username", username));
            return Convert.ToInt32(result) > 0;
        }

        /// <summary>判断邮箱是否已被注册</summary>
        public static bool EmailExists(string email)
        {
            object result = DbHelper.ExecuteScalar(
                "SELECT COUNT(*) FROM Users WHERE Email = @Email",
                new SqlParameter("@Email", email));
            return Convert.ToInt32(result) > 0;
        }

        /// <summary>注册新用户，返回新用户的 UserId</summary>
        public static int Create(User user)
        {
            string sql = @"INSERT INTO Users (Username, Email, PasswordHash, Salt, Nickname, Bio)
                           VALUES (@Username, @Email, @PasswordHash, @Salt, @Nickname, @Bio);
                           SELECT CAST(SCOPE_IDENTITY() AS INT);";
            object result = DbHelper.ExecuteScalar(sql,
                new SqlParameter("@Username", user.Username),
                new SqlParameter("@Email", user.Email),
                new SqlParameter("@PasswordHash", user.PasswordHash),
                new SqlParameter("@Salt", user.Salt),
                new SqlParameter("@Nickname", (object)user.Nickname ?? DBNull.Value),
                new SqlParameter("@Bio", (object)user.Bio ?? DBNull.Value));
            return Convert.ToInt32(result);
        }
    }
}
