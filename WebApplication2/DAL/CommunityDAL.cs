using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace WebApplication2.DAL
{
    // Custom database session. Related writes share one transaction, with bound parameters.
    public sealed class CommunityDAL : IDisposable
    {
        private readonly SqlConnection connection;
        private readonly SqlTransaction transaction;
        public CommunityDAL(bool write = false)
        {
            connection = DbHelper.GetConnection(); connection.Open();
            using(var options=new SqlCommand("SET ARITHABORT ON; SET QUOTED_IDENTIFIER ON; SET NUMERIC_ROUNDABORT OFF;",connection))options.ExecuteNonQuery();
            if (write) transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        }
        private SqlCommand Command(string sql, object[] values)
        {
            var command = new SqlCommand(sql, connection, transaction);
            for (int i = 0; i < values.Length; i++) command.Parameters.AddWithValue("@p" + i, values[i] ?? DBNull.Value);
            return command;
        }
        public List<Dictionary<string, object>> Rows(string sql, params object[] values)
        {
            using (var command = Command(sql, values))
            using (var reader = command.ExecuteReader())
            {
                var rows = new List<Dictionary<string, object>>();
                while (reader.Read())
                {
                    var row = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++) row[reader.GetName(i)] = reader.IsDBNull(i) ? null : (reader.GetValue(i) is DateTime ? ((DateTime)reader.GetValue(i)).ToString("yyyy-MM-dd HH:mm:ss") : reader.GetValue(i));
                    rows.Add(row);
                }
                return rows;
            }
        }
        public object Scalar(string sql, params object[] values) { using (var command = Command(sql, values)) return command.ExecuteScalar(); }
        public int Execute(string sql, params object[] values) { using (var command = Command(sql, values)) return command.ExecuteNonQuery(); }
        public void Commit() { if (transaction != null) transaction.Commit(); }
        public void Dispose() { if (transaction != null) transaction.Dispose(); connection.Dispose(); }
    }
}
