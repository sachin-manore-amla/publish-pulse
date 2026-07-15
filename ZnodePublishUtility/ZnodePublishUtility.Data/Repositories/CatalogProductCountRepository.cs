using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using ZnodePublishUtility.Data.Interfaces;
using ZnodePublishUtility.Data.SqlServer;
using ZnodePublishUtility.Models.DTOs;

namespace ZnodePublishUtility.Data.Repositories;

public class CatalogProductCountRepository : ICatalogProductCountRepository
{
    private readonly SqlServerContext _context;

    public CatalogProductCountRepository(SqlServerContext context)
    {
        _context = context;
    }

    public async Task<List<CatalogProductCountDto>> GetCatalogProductCountsAsync()
    {
        using var connection = new SqlConnection(_context.ConnectionString);
        var rows = await connection.QueryAsync("dbo.GetCatalogProductCounts", commandType: CommandType.StoredProcedure);
        return rows
            .Select(row => CatalogProductCountMapper.MapRow((IDictionary<string, object>)row))
            .ToList();
    }
}
