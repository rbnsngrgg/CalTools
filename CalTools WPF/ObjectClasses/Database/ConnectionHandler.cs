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
        public void CreateTable(string schema);
        public void DropTable(string tableName);
        public void RenameTable(string oldName, string newName);
        public void SetVersion(string version);
        public bool DatabaseReady();
        public int GetDatabaseVersion();
        public int InsertIntoTable(string tableName, Dictionary<string, string> colValues, bool orIgnore = true);
        public void UpdateTable(string tableName, Dictionary<string, string> colValues, Dictionary<string, string> whereValues);
        public void RemoveFromTable(string tableName, Dictionary<string, string> whereValues);
        public List<Dictionary<string, string>> SelectAllFromTable(string tableName);
        public List<Dictionary<string, string>> SelectFromTableWhere(string tableName, Dictionary<string, string> whereValues);
        public List<Dictionary<string, string>> SelectStandardEquipmentWhere(Dictionary<string, string> whereValues);
    }

    internal class SqliteConnectionHandler : IConnectionHandler
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
            "standard_equipment",
            "old_items",
            "old_tasks",
            "old_data"
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
                    $"SQLiteConnectionHandler.ExecuteQuery: {query}",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return reader;
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
        private int LastInsertRowId()
        {
            SqliteDataReader reader = ExecuteQuery("SELECT last_insert_rowid()");
            reader.Read();
            int id = reader.GetInt32(0);
            Disconnect();
            return id;
        }

        public virtual void CreateTable(string schema)
        {
            ExecuteQuery(schema);
            Disconnect();
        }
        public virtual void DropTable(string tableName)
        {
            string query = $"DROP TABLE IF EXISTS {tableName}";
            ExecuteQuery(query);
            Disconnect();
        }
        public virtual void RenameTable(string oldName, string newName)
        {
            ExecuteQuery($"ALTER TABLE {oldName} RENAME TO {newName}");
            Disconnect();
        }
        public virtual void SetVersion(string version)
        {
            ExecuteQuery($"PRAGMA user_version = {version}");
            Disconnect();
        }
        public virtual bool DatabaseReady()
        {
            return Connect() && Disconnect();
        }
        public virtual int GetDatabaseVersion()
        {
            string query = "PRAGMA user_version";
            SqliteDataReader reader = ExecuteQuery(query);
            int version = reader.Read() ? reader.GetInt32(0) : 0;
            Disconnect();
            return version;
        }
        public virtual int InsertIntoTable(string tableName, Dictionary<string,string> colValues, bool orIgnore = true)
        {
            if (IsTableValid(tableName))
            {
                string queryCols = "";
                string queryValues = "";
                foreach(string col in colValues.Keys)
                {
                    if (queryCols != "")
                    {
                        queryCols += ",";
                        queryValues += ",";
                    }
                    queryCols += $"{col}";
                    queryValues += $"'{colValues[col].Replace("'","''")}'";
                }
                string query = $"INSERT {(orIgnore ? "OR IGNORE" : "")} INTO {tableName}({queryCols}) VALUES({queryValues}) ";
                ExecuteQuery(query);
                int lastId = LastInsertRowId();
                Disconnect();
                return lastId;
            }
            else
            {
                Disconnect();
                MessageBox.Show(
                    $"Invalid Table Name",
                    $"SQLiteConnectionHandler.InsertIntoTable: {tableName}",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return -1;
            }
        }
        public virtual void UpdateTable(string tableName, Dictionary<string, string> colValues, Dictionary<string, string> whereValues)
        {
            if (IsTableValid(tableName))
            {
                string querySet = "";
                string queryWhere = "";
                foreach (string col in colValues.Keys)
                {
                    if (querySet != "")
                    {
                        querySet += $",";
                    }
                    querySet += $"{col}='{colValues[col].Replace("'", "''")}'";
                }
                foreach (string col in whereValues.Keys)
                {
                    if (queryWhere != "")
                    {
                        queryWhere += $"AND ";
                    }
                    queryWhere += $"{col}='{whereValues[col].Replace("'", "''")}'";
                }
                string query = $"UPDATE {tableName} SET {querySet} WHERE {queryWhere}";
                ExecuteQuery(query);
                Disconnect();
            }
            else
            {
                Disconnect();
                MessageBox.Show(
                    $"Invalid Table Name",
                    $"SQLiteConnectionHandler.UpdateTable: {tableName}",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        public virtual void RemoveFromTable(string tableName, Dictionary<string, string> whereValues)
        {
            if (IsTableValid(tableName))
            {
                
                string queryWhere = "";
                foreach (string key in whereValues.Keys)
                {
                    if (queryWhere != "") { queryWhere += " AND"; }
                    queryWhere += $" {key}='{whereValues[key]}'";
                }
                string query = $"DELETE * FROM {tableName} WHERE{queryWhere}";
                ExecuteQuery(query);
                Disconnect();
            }
        }
        public virtual List<Dictionary<string, string>> SelectAllFromTable(string tableName)
        {
            List<Dictionary<string, string>> results = new();
            if (IsTableValid(tableName))
            {
                results = GetQueryResults(
                    ExecuteQuery($"SELECT * FROM {tableName}"
                    ));
            }
            Disconnect();
            return results;
        }
        public virtual List<Dictionary<string, string>> SelectFromTableWhere(string tableName, Dictionary<string,string> whereValues)
        {
            List<Dictionary<string, string>> results = new();
            if (IsTableValid(tableName))
            {
                string queryWhere = "";
                foreach(string col in whereValues.Keys)
                {
                    if(queryWhere != "") { queryWhere += " AND"; }
                    queryWhere += $" {col}='{whereValues[col]}'";
                }
                string query = $"SELECT * FROM {tableName} WHERE {queryWhere}";
                results = GetQueryResults(
                    ExecuteQuery(query
                    ));
            }
            Disconnect();
            return results;
        }
        public virtual List<Dictionary<string, string>> SelectStandardEquipmentWhere(Dictionary<string, string> whereValues)
        {
            List<Dictionary<string, string>> results = new();
            string queryWhere = "";
            foreach (string col in whereValues.Keys)
            {
                if (queryWhere != "") { queryWhere += " AND"; }
                queryWhere += $" {col}='{whereValues[col]}'";
            }
            string query = $"SELECT * FROM standard_equipment " +
                    $"WHERE standard_equipment.id IN " +
                    $"(SELECT standard_equipment_id FROM data_standard_equipment WHERE {queryWhere})";
            results = GetQueryResults(
                ExecuteQuery(query
                ));
            Disconnect();
            return results;
        }
    }
}
