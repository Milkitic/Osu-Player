using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Dynamic;
using System.Linq;
using Dapper;

namespace Milky.OsuPlayer.Common.Data.Dapper.Provider
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
            catch (Exception exc)
            {
                throw new Exception($"从数据库中获取数据出错：\r\n" +
                                    $"数据库执行语句：{sqlStr}", exc);
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
            catch (Exception exc)
            {
                throw new Exception($"获取数据表{tableName}中字段名出错：\r\n" +
                                    $"数据库执行语句：{sqlStr}", exc);
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
            string whereStr, object whereParams,
            string orderColumn,
            int count,
            bool asc, out string sql, out object kvParams)
        {
            kvParams = new ExpandoObject();
            //var s = ((ICollection<KeyValuePair<string, object>>) whereParams).ToDictionary(k => k.Key, k => k.Value);
            var existColumn =
                new HashSet<string>(((ICollection<KeyValuePair<string, object>>)whereParams).Select(k => k.Key));
            var expando = (ICollection<KeyValuePair<string, object>>)kvParams;
            foreach (var kvp in updateColumns)
            {
                if (!existColumn.Contains(kvp.Key))
                {
                    existColumn.Add(kvp.Key);
                    expando.Add(new KeyValuePair<string, object>("upd_" + kvp.Key, kvp.Value));
                }
                else
                {
                    int j = 0;
                    var newStr = kvp.Key + j;
                    while (existColumn.Contains(newStr))
                    {
                        j++;
                        newStr = kvp.Key + j;
                    }

                    expando.Add(new KeyValuePair<string, object>("upd_" + newStr, kvp.Value));
                    existColumn.Add(newStr);
                }
            }

            //var newDic = eoColl.ToDictionary(k => k.Key, k => k.Value);
            var newDic = new Dictionary<string, string>();
            var o1 = updateColumns.Keys.ToList();
            var o2 = expando.Select(k => k.Key).ToList();
            for (int i = 0; i < o1.Count; i++)
            {
                newDic.Add(o1[i], o2[i]);
            }

            sql = $"UPDATE {table} SET " +
                  string.Join(",", newDic.Select(k => $"{k.Key}=@{k.Value}")) + " " +
                  $"WHERE {whereStr} ";
        }

        protected override void GetInsertCommandTemplate(string table, Dictionary<string, object> insertColumns,
            out string sql, out object kvParams)
        {
            kvParams = new ExpandoObject();
            var eoColl = (ICollection<KeyValuePair<string, object>>)kvParams;
            foreach (var kvp in insertColumns)
            {
                eoColl.Add(new KeyValuePair<string, object>($"ins_{kvp.Key}", kvp.Value));
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