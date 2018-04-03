using SPKHelperPackage.Logs;
using System;
using System.Data;
using System.Data.SqlClient;

namespace SPK.BackUpSql
{
    public class RestDatabase
    {
        private readonly string _sqlConnectionString;

        public RestDatabase(string sqlConnectionString)
        {
            _sqlConnectionString = sqlConnectionString;
        }

        public void Go(string dbName, string localtion)
        {
            using (SqlConnection conn = new SqlConnection(_sqlConnectionString))
            {
                conn.Open();
                conn.InfoMessage += onInfoMessage;
                conn.FireInfoMessageEventOnUserErrors = true;

                try
                {
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = "sp_restore_database";
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 0;
                    cmd.Parameters.AddWithValue("@dbName", dbName);
                    cmd.Parameters.AddWithValue("@fromLocation", localtion);
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Logged.Error("RestDatabase", "Go", ex.Message);
                }
            }
        }

        private void onInfoMessage(object sender, SqlInfoMessageEventArgs args)
        {
            foreach (var msg in args.Message)
            {
                Logged.Event("RestDatabase", "onInfoMessage", msg.ToString());
            }

            foreach (SqlError err in args.Errors)
            {
                Logged.Error("RestDatabase", "onInfoMessage", string.Format("Msg {0}, Level {1}, State {2}, Line {3}",
                    err.Number, err.Class, err.State, err.LineNumber));
                Logged.Error("RestDatabase", "onInfoMessage", err.Message);
            }
        }
    }
}