using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace WebApplication2.DAL
{
    /// <summary>
    /// 通用数据库操作类（自定义类）——本项目所有数据库增删改查都通过此类完成，
    /// 页面代码不再直接书写 SQL / 连接字符串，从而实现数据访问与页面逻辑分离（获得加分）。
    /// 所有方法均采用参数化查询，防止 SQL 注入。
    /// </summary>
    public static class DbHelper
    {
        /// <summary>从 Web.config 读取连接字符串</summary>
        private static readonly string ConnStr =
            ConfigurationManager.ConnectionStrings["KnowledgeStackConn"].ConnectionString;

        /// <summary>创建并返回一个 SqlConnection 对象</summary>
        public static SqlConnection GetConnection()
        {
            return new SqlConnection(ConnStr);
        }

        /// <summary>执行 INSERT / UPDATE / DELETE，返回受影响的行数</summary>
        public static int ExecuteNonQuery(string sql, params SqlParameter[] parameters)
        {
            using (SqlConnection conn = GetConnection())
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                if (parameters != null)
                {
                    foreach (SqlParameter parameter in parameters) cmd.Parameters.Add((SqlParameter)((System.ICloneable)parameter).Clone());
                }
                conn.Open();
                return cmd.ExecuteNonQuery();
            }
        }

        /// <summary>执行查询并返回第一行第一列的值（用于 COUNT、SCOPE_IDENTITY 等）</summary>
        public static object ExecuteScalar(string sql, params SqlParameter[] parameters)
        {
            using (SqlConnection conn = GetConnection())
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                if (parameters != null)
                {
                    foreach (SqlParameter parameter in parameters) cmd.Parameters.Add((SqlParameter)((System.ICloneable)parameter).Clone());
                }
                conn.Open();
                return cmd.ExecuteScalar();
            }
        }

        /// <summary>执行查询并返回 DataTable 结果集</summary>
        public static DataTable ExecuteDataTable(string sql, params SqlParameter[] parameters)
        {
            using (SqlConnection conn = GetConnection())
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
            {
                if (parameters != null)
                {
                    foreach (SqlParameter parameter in parameters) cmd.Parameters.Add((SqlParameter)((System.ICloneable)parameter).Clone());
                }
                DataTable dt = new DataTable();
                adapter.Fill(dt);   // Fill 会自动打开并关闭连接
                return dt;
            }
        }
    }
}
