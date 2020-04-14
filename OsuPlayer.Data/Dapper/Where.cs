using System;
using System.Data;

namespace Milky.OsuPlayer.Data.Dapper
{
    public class Where
    {
        public Where(string columnName, object value)
        {
            ColumnName = columnName;
            Value = value;
        }

        public WhereType WhereType { get; set; } = WhereType.Equal;
        public string ColumnName { get; set; }
        public object Value { get; set; }
        public DbType? ForceKeyType { get; set; }

        public static implicit operator Where((string columnName, object value) tuple)
        {
            return new Where(tuple.columnName, tuple.value);
        }

        public static implicit operator Where((string columnName, object value, string type) tuple)
        {
            return new Where(tuple.columnName, tuple.value)
            {
                WhereType = SwitchType(tuple.type)
            };
        }

        public static implicit operator Where((string columnName, object value, WhereType type) tuple)
        {
            return new Where(tuple.columnName, tuple.value)
            {
                WhereType = tuple.type
            };
        }

        public static WhereType SwitchType(string symbol)
        {
            switch (symbol)
            {
                case "=":
                case "==":
                    return WhereType.Equal;
                case "<>":
                case "!=":
                    return WhereType.Unequal;
                case "<":
                    return WhereType.Less;
                case "<=":
                    return WhereType.LessOrEqual;
                case ">":
                    return WhereType.Greater;
                case ">=":
                    return WhereType.GreaterOrEqual;
                default:
                    return WhereType.Equal;
            }
        }

        public static string GetTypeSymbol(WhereType whereType)
        {
            switch (whereType)
            {
                case WhereType.Equal:
                    return "=";
                case WhereType.Unequal:
                    return "<>";
                case WhereType.Less:
                    return "<";
                case WhereType.LessOrEqual:
                    return "<=";
                case WhereType.GreaterOrEqual:
                    return ">=";
                case WhereType.Greater:
                    return ">";
                default:
                    throw new ArgumentOutOfRangeException(nameof(whereType),
                        whereType, null);
            }
        }
    }
}