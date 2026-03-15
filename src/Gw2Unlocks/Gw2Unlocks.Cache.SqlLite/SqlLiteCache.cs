// SqlLiteCache.cs
using Gw2Unlocks.Cache.Contract;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Gw2Unlocks.Cache.SqlLite
{
    internal class SqlLiteCache : IGw2Cache, IDisposable
    {
        private readonly SqliteConnection _connection;
        private bool _disposed;

        public SqlLiteCache(string dataSource)
        {
            _connection = new SqliteConnection($"Data Source={dataSource}");
            _connection.Open();
            InitializeTables();
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

            // Optional index for incremental refresh
            cmd.CommandText = @"
                CREATE INDEX IF NOT EXISTS IX_CachedResponses_LastUpdated
                ON CachedResponses(LastUpdated);
            ";
            cmd.ExecuteNonQuery();
        }

        public async Task AddOrUpdateAsync(string endpoint, int id, string jsonContent)
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
            cmd.Parameters.AddWithValue("$content", jsonContent);

            await cmd.ExecuteNonQueryAsync();
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

            // Temp table for safety
            using var createCmd = _connection.CreateCommand();
            createCmd.CommandText = "CREATE TEMP TABLE TempIds(Id INTEGER PRIMARY KEY)";
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
            if (disposing) _connection?.Dispose();
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~SqlLiteCache() => Dispose(false);
    }
}