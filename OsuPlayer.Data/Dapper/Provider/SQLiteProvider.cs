using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using Dapper;

namespace Milky.OsuPlayer.Data.Dapper.Provider
{
    // ReSharper disable once InconsistentNaming
    public sealed class SQLiteProvider : DbProviderBase
    {
        public SQLiteProvider()
        {
            DbConnectionStringBuilder = new SQLiteConnectionStringBuilder();
        }

        public override DataBaseType DbType => DataBaseType.Sqlite;

        protected override DbConnection GetNewDbConnection()
        {
            return new SQLiteConnection(ConnectionString);
        }

        protected internal override List<string> InnerGetAllTables(DbConnection dbConnection)
        {
            string sqlStr = null;

            try
            {
                sqlStr = "SELECT name FROM sqlite_master WHERE type='table'";
                var list = dbConnection.Query<string>(sqlStr).ToList();
                return list;
            }
            catch (Exception ex)
            {
                throw new Exception($"从数据库中获取数据出错：\r\n" +
                                    $"数据库执行语句：{sqlStr}", ex);
            }
        }

        protected internal override List<string> InnerGetColumnsByTable(DbConnection dbConnection, string tableName)
        {
            string sqlStr = null;

            try
            {
                sqlStr = $"PRAGMA table_info(`{tableName}`)";
                var list = dbConnection.Query(sqlStr).ToList();

                return list.Select(o => (string)o.name).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"获取数据表{tableName}中字段名出错：\r\n" +
                                    $"数据库执行语句：{sqlStr}", ex);
            }
        }

        protected override string GetSelectCommandTemplate(string table, IReadOnlyCollection<string> columns,
            string orderColumn, string whereStr, int count, bool asc)
        {
            var colStr = "*";
            if (columns != null && columns.Count > 0)
            {
                colStr = string.Join(",", columns.Select(k => $"`{k}`"));
            }

            return $"SELECT {colStr} " +
                   $"FROM `{table}` " +
                   $"WHERE {whereStr} " +
                   (orderColumn == null ? "" : $"ORDER BY `{orderColumn}` {(asc ? "ASC" : "DESC")} ") +
                   (count <= 0 ? "" : $"LIMIT {count} ");
        }

        protected override void GetUpdateCommandTemplate(string table, Dictionary<string, object> updateColumns,
            string whereStr, DynamicParameters whereParams,
            string orderColumn,
            int count,
            bool asc, out string sql, out DynamicParameters kvParams)
        {
            kvParams = new DynamicParameters();
            var assignments = new List<string>(updateColumns.Count);
            var usedParameterNames = whereParams == null
                ? new HashSet<string>()
                : new HashSet<string>(whereParams.ParameterNames);

            foreach (var kvp in updateColumns)
            {
                var parameterName = GetUniqueParameterName("upd_" + kvp.Key, usedParameterNames);
                kvParams.Add(parameterName, kvp.Value);
                assignments.Add($"[{kvp.Key}]=@{parameterName}");
            }

            sql = $"UPDATE {table} SET " +
                  string.Join(",", assignments) + " " +
                  $"WHERE {whereStr} ";
        }

        private static string GetUniqueParameterName(string baseName, ISet<string> usedParameterNames)
        {
            if (usedParameterNames.Add(baseName))
            {
                return baseName;
            }

            var index = 0;
            string candidate;
            do
            {
                candidate = baseName + index++;
            } while (!usedParameterNames.Add(candidate));

            return candidate;
        }

        protected override void GetInsertCommandTemplate(string table, Dictionary<string, object> insertColumns,
            out string sql, out DynamicParameters kvParams)
        {
            kvParams = new DynamicParameters();
            //var eoColl = (ICollection<KeyValuePair<string, object>>)kvParams;
            foreach (var kvp in insertColumns)
            {
                kvParams.Add($"ins_{kvp.Key}", kvp.Value);
            }

            sql = $"INSERT INTO {table} (" +
                  string.Join(",", insertColumns.Keys.Select(k => $"[{k}]")) +
                  $") VALUES (" +
                  string.Join(",", insertColumns.Keys.Select(k => $"@ins_{k}")) +
                  $")";
        }

        protected override string GetDeleteCommandTemplate(string table, string whereStr)
        {
            return $"DELETE FROM {table} " +
                   $"WHERE {whereStr} ";
        }
    }
}