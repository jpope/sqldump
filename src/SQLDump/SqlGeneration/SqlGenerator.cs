using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using SQLDump.Configuration;

namespace SQLDump.SqlGeneration
{
    public static class SqlGenerator
    {
        public static string GetInsertStatement(TableInfo table, IDataRecord reader, bool includeIdentityInsert)
        {
            var builder = new StringBuilder("");
            builder.Append("insert into " + table.SchemaAndTableName + " (");
            var flag = true;
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var fieldName = reader.GetName(i);
                if (includeIdentityInsert || (fieldName != table.IdentityColumn))
                {
                    if (flag)
                    {
                        flag = false;
                    }
                    else
                    {
                        builder.Append(", ");
                    }
                    builder.Append("[" + fieldName + "]");
                }
            }
            builder.Append(") values (");
            flag = true;
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var fieldName = reader.GetName(i);
                if (includeIdentityInsert || (fieldName != table.IdentityColumn))
                {
                    if (flag)
                    {
                        flag = false;
                    }
                    else
                    {
                        builder.Append(", ");
                    }

                    var customColumnFormat = table
                        .OverrideColumns
                        .SingleOrDefault(x => x.ColumnName == reader.GetName(i))?
                        .OverrideSerializationType;

                    var sqlLiteral = ConvertToSqlLiteral(reader.GetFieldType(i), reader.GetValue(i), customColumnFormat);
                    builder.Append(sqlLiteral);
                }
            }
            builder.Append(")");
            return builder.ToString();
        }

        public static string ConvertToSqlLiteral(Type type, object value, string alternateFormatter)
        {
            if (value == DBNull.Value)
            {
                return "null";
            }
            else if (type == typeof(string))
            {
                return "'" + ((string)value).Replace("'", "''") + "'";
            }
            else if (alternateFormatter == "Date" || alternateFormatter == "Date?")
            {
                return "'" + ((DateTime)value).ToString("yyyy-MM-dd") + "'";
            }
            else if (type == typeof(DateTime))
            {
                return "'" + ((DateTime)value).ToString("yyyy-MM-dd hh:mm:ss.fff") + "'";
            }
            else if (type == typeof(DateTimeOffset))
            {
                return "'" + ((DateTimeOffset)value).ToString("yyyy-MM-dd hh:mm:ss.fff") + "'";
            }
            else if (type == typeof(byte[]))
            {
                return GetHexString((byte[])value);
            }
            else if (type == typeof(Guid))
            {
                return "'" + ((Guid)value).ToString("D") + "'";
            }
            else if (type == typeof(bool))
            {
                return ((bool)value) ? "1" : "0";
            }
            else
            {
                return value.ToString();
            }
        }

        private static string GetHexString(IEnumerable<byte> value)
        {
            var sb = new StringBuilder("0x");

            foreach (var @byte in value)
            {
                sb.AppendFormat("{0:x2}", @byte);
            }

            return sb.ToString();
        }
    }
}