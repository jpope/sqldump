namespace SQLDump.SqlGeneration
{
    public class ConnectionStringBuilder
    {
        public string BuildConnectionString(string server, string database, bool useSqlServerAuthenication, string username, string password)
        {
            if (useSqlServerAuthenication)
            {
                return string.Format("Data Source={0};Initial Catalog={1};User Id={2};Password={3};", server, database, username, password);
            }
            else
            {
                return string.Format("Data Source={0};Initial Catalog={1};Integrated Security=SSPI;Trusted_Connection=yes;", server, database);
            }
        }
    }
}