using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using MongoDB.Bson;
using MongoDB.Driver;
using MySqlConnector;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SqlParameter = BotSharp.Plugin.SqlDriver.Models.SqlParameter;

namespace BotSharp.Plugin.SqlDriver.Services
{
    public class SqlExecuteService
    {
        public async Task<IEnumerable<dynamic>> RunQueryInMySql(string connectionString, string sqlText, SqlParameter[] parameters = null)
        {
            using var connection = new MySqlConnection(connectionString);
            var dictionary = new Dictionary<string, object>();
            foreach (var p in parameters ?? Array.Empty<SqlParameter>())
            {
                dictionary["@" + p.Name] = p.Value;
            }
            return await connection.QueryAsync(sqlText, dictionary);
        }

        public async Task<IEnumerable<dynamic>> RunQueryInSqlServer(string connectionString, string sqlText, SqlParameter[] parameters = null)
        {
            using var connection = new SqlConnection(connectionString);
            var dictionary = new Dictionary<string, object>();
            foreach (var p in parameters ?? Array.Empty<SqlParameter>())
            {
                dictionary["@" + p.Name] = p.Value;
            }
            return await connection.QueryAsync(sqlText, dictionary);
        }

        public async Task<IEnumerable<dynamic>> RunQueryInRedshift(string connectionString, string sqlText, SqlParameter[] parameters = null)
        {
            using var connection = new NpgsqlConnection(connectionString);
            var dictionary = new Dictionary<string, object>();
            foreach (var p in parameters ?? Array.Empty<SqlParameter>())
            {
                dictionary["@" + p.Name] = p.Value;
            }
            return await connection.QueryAsync(sqlText, dictionary);
        }

        public async Task<IEnumerable<dynamic>> RunQueryInSqlite(string connectionString, string sqlText, SqlParameter[] parameters = null)
        {
            using var connection = new SqliteConnection(connectionString);
            var dictionary = new Dictionary<string, object>();
            foreach (var p in parameters ?? Array.Empty<SqlParameter>())
            {
                dictionary["@" + p.Name] = p.Value;
            }
            return await connection.QueryAsync(sqlText, dictionary);
        }

        public async Task<IEnumerable<dynamic>> RunQueryInMongoDb(string connectionString, string sqlText, SqlParameter[] parameters = null)
        {
            var client = new MongoClient(connectionString);

            // Normalize multi-line query to single line
            var statement = Regex.Replace(sqlText.Trim(), @"\s+", " ");

            // Parse MongoDB query: database.collection.find({query}).projection({}).sort({}).limit(100)
            var match = Regex.Match(statement,
                @"^([^.]+)\.([^.]+)\.find\s*\((.*?)\)(.*)?$",
                RegexOptions.Singleline);

            if (!match.Success)
                return ["Invalid MongoDB query format. Expected: database.collection.find({query})"];

            var queryJson = BuildMongoQuery(match.Groups[3].Value.Trim(), parameters ?? Array.Empty<SqlParameter>());

            try
            {
                var database = client.GetDatabase(match.Groups[1].Value);
                var collection = database.GetCollection<BsonDocument>(match.Groups[2].Value);

                var filter = string.IsNullOrWhiteSpace(queryJson) || queryJson == "{}"
                    ? Builders<BsonDocument>.Filter.Empty
                    : BsonDocument.Parse(queryJson);

                var findFluent = collection.Find(filter);
                findFluent = BuildMongoFluent(findFluent, match.Groups[4].Value);

                var results = await findFluent.ToListAsync();
                return results.Select(doc => BsonTypeMapper.MapToDotNetValue(doc));
            }
            catch (Exception ex)
            {
                return [$"Invalid MongoDB query: {ex.Message}"];
            }
        }

        private string BuildMongoQuery(string query, Models.SqlParameter[] parameters)
        {
            foreach (var p in parameters)
                query = query.Replace($"@{p.Name}", p.Value?.ToString() ?? "null");
            return query;
        }

        private IFindFluent<BsonDocument, BsonDocument> BuildMongoFluent(
            IFindFluent<BsonDocument, BsonDocument> findFluent, string chainedOps)
        {
            if (string.IsNullOrWhiteSpace(chainedOps)) return findFluent;

            // Apply projection
            var projMatch = Regex.Match(chainedOps, @"\.projection\s*\((.*?)\)", RegexOptions.Singleline);
            if (projMatch.Success)
                findFluent = findFluent.Project<BsonDocument>(BsonDocument.Parse(projMatch.Groups[1].Value.Trim()));

            // Apply sort
            var sortMatch = Regex.Match(chainedOps, @"\.sort\s*\((.*?)\)", RegexOptions.Singleline);
            if (sortMatch.Success)
                findFluent = findFluent.Sort(BsonDocument.Parse(sortMatch.Groups[1].Value.Trim()));

            // Apply limit
            var limitMatch = Regex.Match(chainedOps, @"\.limit\s*\((\d+)\)");
            if (limitMatch.Success && int.TryParse(limitMatch.Groups[1].Value, out var limit))
            {
                findFluent = findFluent.Limit(limit);
            }
            else
            {
                findFluent = findFluent.Limit(10);
            }

            return findFluent;
        }

        public async Task<IEnumerable<dynamic>> RunQueryInMySql(string connectionString, string[] sqlTexts)
        {
            return await RunQueryInMySql(connectionString, string.Join(";\r\n", sqlTexts), []);
        }

        public async Task<IEnumerable<dynamic>> RunQueryInSqlServer(string connectionString, string[] sqlTexts)
        {
            return await RunQueryInSqlServer(connectionString, string.Join("\r\n", sqlTexts), []);
        }

        public async Task<IEnumerable<dynamic>> RunQueryInRedshift(string connectionString, string[] sqlTexts)
        {
            return await RunQueryInRedshift(connectionString, string.Join("\r\n", sqlTexts), []);
        }

        public async Task<IEnumerable<dynamic>> RunQueryInSqlite(string connectionString, string[] sqlTexts)
        {
            return await RunQueryInSqlite(connectionString, string.Join("\r\n", sqlTexts), []);
        }
    }
}
