namespace CoJ.ESB.FX.ASM
{
    #region Using Directives
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Data;
    using System.Data.SqlClient;
    using System.Text;
    using System.Xml;
    #endregion

    [Serializable]
    public class Helper
    {
        #region Public Methods
        public static int InsertMessage(string connectionString, string messageType, XmlDocument message)
        {
            int recordId = 0;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = @"INSERT INTO [Messaging].[Audit] VALUES ('" + messageType + "','GETDATE()',null,null,0,'" + message.OuterXml + "'); SELECT @@IDENTITY";

                SqlCommand command = new SqlCommand();
                command.CommandText = query;
                command.CommandType = CommandType.Text;
                command.Connection = connection;

                connection.Open();
                SqlDataReader reader = command.ExecuteReader();

                if(reader.HasRows)
                {
                    while(reader.Read())
                    {
                        recordId = Convert.ToInt32(reader.GetValue(0).ToString());
                    }
                }

                command.Dispose();
            }

            return recordId;
        }

        public static void UpdateMessage(string connectionString, int recordId, int status)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = @"UPDATE [Messaging].[Audit] SET Status = '" + status + "' WHERE pkRecordId = '" + recordId + "'";

                SqlCommand command = new SqlCommand();
                command.CommandText = query;
                command.CommandType = CommandType.Text;
                command.Connection = connection;

                connection.Open();
                command.ExecuteNonQuery();
                command.Dispose();
            }
        }

        public static void UpdateMessageException(string connectionString, string exception, int recordId)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = @"UPDATE [Messaging].[Audit] SET Status = '3', ExceptionMessage = '" + exception + "', ExceptionDate = GETDATE() WHERE pkRecordId = '" + recordId + "'";

                SqlCommand command = new SqlCommand();
                command.CommandText = query;
                command.CommandType = CommandType.Text;
                command.Connection = connection;

                connection.Open();
                command.ExecuteNonQuery();
                command.Dispose();
            }
        }
        #endregion
    }
}
