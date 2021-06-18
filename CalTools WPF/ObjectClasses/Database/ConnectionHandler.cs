using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;


namespace CalTools_WPF.ObjectClasses.Database
{
    public interface IConnectionHandler
    {

    }

    class SqliteConnectionHandler : IConnectionHandler
    {
        private readonly SqliteConnection connection;
        private SqliteDataReader reader;

        public readonly string[] TableNames = {
            "items",
            "tasks",
            "task_data",
            "task_data_files",
            "data_standard_equipment",
            "findings",
            "standard_equipment"
        };
        public string DbPath { get; set; }
        public SqliteConnectionHandler(string dbPath)
        {
            DbPath = dbPath;
            connection = new SqliteConnection($"Data Source={DbPath}");
        }
        private bool Connect()
        {
            try
            {
                if (!IsConnectionOpen())
                {
                    connection.Open();
                }
                return true;
            }
            catch(Exception ex)
            {
                Disconnect();
                MessageBox.Show(
                    $"Error connecting to database: {ex.Message}",
                    "SQLiteConnectionHandler.Connect",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }
        }

        private bool Disconnect() //True if disconnected successfully, false if error
        {
            try
            {
                if (IsConnectionOpen())
                {
                    connection.Close();
                }
                return true;
            }
            catch (Exception ex)
            {
                Disconnect();
                MessageBox.Show(
                    $"Error disconnecting from database: {ex.Message}",
                    "SQLiteConnectionHandler.Disconnect",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }
        }

        private bool IsConnectionOpen()
        {
            return connection.State == System.Data.ConnectionState.Open;
        }

        private bool IsTableValid(string tableName)
        {
            return TableNames.Contains(tableName);
        }

        private bool DatabaseReady() //Check for successful connect and disconnect
        {
            return Connect() && Disconnect();
        }

        private SqliteDataReader ExecuteQuery(string query)
        {
            try
            {   if(Connect())
                {
                    SqliteCommand command = new SqliteCommand(query, connection);
                    reader = command.ExecuteReader();
                    return reader;
                }
                throw new SqliteException("Error connecting to database to execute query.", 1);
            }
            catch (Exception ex)
            {
                Disconnect();
                MessageBox.Show(
                    $"Error executing query: {ex.Message}",
                    "SQLiteConnectionHandler.ExecuteQuery",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                SqliteCommand blankCommand = new();
                return blankCommand.ExecuteReader();
            }
        }

        private List<Dictionary<string,string>> GetQueryResults(SqliteDataReader dataReader)
        {
            List<Dictionary<string, string>> queryResults = new();
            try
            {
                if(dataReader.HasRows)
                {
                    int fieldCount = dataReader.FieldCount;
                    while (dataReader.Read())
                    {
                        Dictionary<string, string> rowItem = new();
                        for (int col = 0; col < fieldCount; col++)
                        {
                            string colName = dataReader.GetName(col);
                            rowItem[colName] = reader.GetString(col);
                        }
                        queryResults.Add(rowItem);
                    }
                }
                Disconnect();
                return queryResults;
            }
            catch(Exception ex)
            {
                Disconnect();
                queryResults.Clear();
                MessageBox.Show(
                    $"Error parsing query results: {ex.Message}",
                    "SQLiteConnectionHandler.GetQueryResults",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                queryResults = new();
                return queryResults;
            }
            
        }

        public List<Dictionary<string, string>> SelectAllFromTable(string tableName)
        {
            List<Dictionary<string, string>> results = new();
            if (IsTableValid(tableName))
            {
                results = GetQueryResults(
                    ExecuteQuery($"SELECT * FROM {tableName}"
                    ));
            }
            return results;
        }
        public List<Dictionary<string, string>> SelectFromTableWhere(string tableName, string[] whereCols, string[] whereValues)
        {
            List<Dictionary<string, string>> results = new();
            if (IsTableValid(tableName))
            {
                string query = $"SELECT * FROM {tableName} WHERE ";
                for(int i = 0; i < whereCols.Length; i++)
                {
                    if(i > 0) { query += " AND"; }
                    query += $" {whereCols[i]}='{whereValues[i]}'";
                }
                results = GetQueryResults(
                    ExecuteQuery(query
                    ));
            }
            return results;
        }
    }
}
