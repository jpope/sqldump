using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using SQLDump.Configuration;

namespace SQLDump.SqlGeneration
{
    public static class SqlGenerator
    {
        public static string GetInsertStatement(TableInfo table, IDataRecord reader, bool includeIdentityInsert)
        {
            StringBuilder builder = new StringBuilder("");
            builder.Append("insert into [" + table.Name + "] (");
            bool flag = true;
            for (int i = 0; i < reader.FieldCount; i++)
            {
                string name = reader.GetName(i);
                if (includeIdentityInsert || (name != table.IdentityColumn))
                {
                    if (flag)
                    {
                        flag = false;
                    }
                    else
                    {
                        builder.Append(", ");
                    }
                    builder.Append("[" + name + "]");
                }
            }
            builder.Append(") values (");
            flag = true;
            for (int j = 0; j < reader.FieldCount; j++)
            {
                string str2 = reader.GetName(j);
                if (includeIdentityInsert || (str2 != table.IdentityColumn))
                {
                    if (flag)
                    {
                        flag = false;
                    }
                    else
                    {
                        builder.Append(", ");
                    }
                    string str3 = ConvertToSqlLiteral(reader.GetFieldType(j), reader.GetValue(j));
                    builder.Append(str3);
                }
            }
            builder.Append(")");
            return builder.ToString();
        }

        public static string ConvertToSqlLiteral(Type type, object value)
        {
            if (value == DBNull.Value)
            {
                return "null";
            }
            else if (type == typeof(string))
            {
                return "'" + ((string)value).Replace("'", "''") + "'";
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
            var sb = new StringBuilder("'0x");

            foreach (var @byte in value)
            {
                sb.AppendFormat("{0:x2}", @byte);
            }

            sb.Append("'");

            return sb.ToString();
        }
    }
}