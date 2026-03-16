using Gw2Unlocks.Cache.Contract;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

internal class SqlLiteCache : IGw2Cache, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ILogger<SqlLiteCache> logger;
    private readonly SemaphoreSlim _writeSemaphore = new(1, 1); // <-- async lock
    private bool _disposed;

    public SqlLiteCache(string dataSource, ILogger<SqlLiteCache> logger)
    {
        _connection = new SqliteConnection($"Data Source={dataSource}");
        _connection.Open();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode=WAL;";
        cmd.ExecuteNonQuery();

        InitializeTables();
        this.logger = logger;
    }

    private void InitializeTables()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS CachedResponses(
                Endpoint TEXT NOT NULL,
                Id INTEGER NOT NULL,
                Content TEXT NOT NULL,
                LastUpdated TEXT NOT NULL,
                PRIMARY KEY(Endpoint, Id)
            );
        ";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"
            CREATE INDEX IF NOT EXISTS IX_CachedResponses_LastUpdated
            ON CachedResponses(LastUpdated);
        ";
        cmd.ExecuteNonQuery();
    }

    public async Task AddOrUpdateAsync(string endpoint, int id, string jsonContent)
    {
        try
        {
            await _writeSemaphore.WaitAsync();
            try
            {
                using var cmd = _connection.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO CachedResponses (Endpoint, Id, Content, LastUpdated)
                    VALUES ($endpoint, $id, $content, datetime('now'))
                    ON CONFLICT(Endpoint, Id) DO UPDATE SET
                        Content = excluded.Content,
                        LastUpdated = excluded.LastUpdated
                ";
                cmd.Parameters.AddWithValue("$endpoint", endpoint);
                cmd.Parameters.AddWithValue("$id", id);
                cmd.Parameters.AddWithValue("$content", jsonContent ?? "");

                await cmd.ExecuteNonQueryAsync();
            }
            finally
            {
                _writeSemaphore.Release();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AddOrUpdateAsync");
        }
    }

    public async Task<string?> GetCachedAsync(string endpoint, int id)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT Content FROM CachedResponses WHERE Endpoint = $endpoint AND Id = $id";
        cmd.Parameters.AddWithValue("$endpoint", endpoint);
        cmd.Parameters.AddWithValue("$id", id);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return reader.GetString(0);

        return null;
    }

    public async Task<IEnumerable<int>> GetNewKeysAsync(string endpoint, IEnumerable<int> ids)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0) return [];

        await _writeSemaphore.WaitAsync();
        try
        {
            using var dropCmd = _connection.CreateCommand();
            dropCmd.CommandText = "DROP TABLE IF EXISTS TempIds;";
            await dropCmd.ExecuteNonQueryAsync();

            using var createCmd = _connection.CreateCommand();
            createCmd.CommandText = "CREATE TEMP TABLE TempIds(Id INTEGER PRIMARY KEY);";
            await createCmd.ExecuteNonQueryAsync();

            using var insertCmd = _connection.CreateCommand();
            insertCmd.CommandText = "INSERT INTO TempIds(Id) VALUES ($id)";
            var param = insertCmd.CreateParameter();
            param.ParameterName = "$id";
            insertCmd.Parameters.Add(param);

            foreach (var id in idList)
            {
                param.Value = id;
                await insertCmd.ExecuteNonQueryAsync();
            }
        }
        finally
        {
            _writeSemaphore.Release();
        }

        var existingIds = new HashSet<int>();
        using var selectCmd = _connection.CreateCommand();
        selectCmd.CommandText = @"
            SELECT t.Id
            FROM TempIds t
            LEFT JOIN CachedResponses c
                ON c.Endpoint = $endpoint AND c.Id = t.Id
            WHERE c.Id IS NOT NULL
        ";
        selectCmd.Parameters.AddWithValue("$endpoint", endpoint);

        using var reader = await selectCmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            existingIds.Add(reader.GetInt32(0));

        return idList.Where(id => !existingIds.Contains(id));
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            _connection?.Dispose();
            _writeSemaphore.Dispose();
        }
        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~SqlLiteCache() => Dispose(false);
}