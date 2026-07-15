namespace ZnodePublishUtility.Data.SqlServer;

/// <summary>
/// Holds the SQL Server connection string used by SQL-backed repositories. Connections are
/// opened per call (ADO.NET pools them internally), so this only needs to carry the string —
/// mirrors the role <c>MongoDbContext</c> plays for the Mongo-backed repositories.
/// </summary>
public class SqlServerContext
{
    public string ConnectionString { get; }

    public SqlServerContext(string connectionString)
    {
        ConnectionString = connectionString;
    }
}
