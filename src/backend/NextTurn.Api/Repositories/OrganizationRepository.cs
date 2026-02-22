using System.Data;
using Microsoft.Data.SqlClient;
using NextTurn.Api.Models;

namespace NextTurn.Api.Repositories
{
    public class OrganizationRepository
    {
        private readonly IConfiguration _config;

        public OrganizationRepository(IConfiguration config)
        {
            _config = config;
        }

        private string ConnStr =>
            _config.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing connection string: ConnectionStrings:Default");

        public async Task<List<Organization>> GetAllAsync()
        {
            var results = new List<Organization>();

            using var conn = new SqlConnection(ConnStr);
            using var cmd = new SqlCommand(
                @"SELECT OrganizationId, Name, CreatedAt
                  FROM Organizations
                  ORDER BY OrganizationId DESC;", conn);

            await conn.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new Organization
                {
                    OrganizationId = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    CreatedAt = reader.GetDateTime(2)
                });
            }

            return results;
        }

        public async Task<Organization?> GetByIdAsync(int id)
        {
            using var conn = new SqlConnection(ConnStr);
            using var cmd = new SqlCommand(
                @"SELECT OrganizationId, Name, CreatedAt
                  FROM Organizations
                  WHERE OrganizationId = @id;", conn);

            cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;

            await conn.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            return new Organization
            {
                OrganizationId = reader.GetInt32(0),
                Name = reader.GetString(1),
                CreatedAt = reader.GetDateTime(2)
            };
        }

        public async Task<int> CreateAsync(string name)
        {
            using var conn = new SqlConnection(ConnStr);
            using var cmd = new SqlCommand(
                @"INSERT INTO Organizations (Name)
                  OUTPUT INSERTED.OrganizationId
                  VALUES (@name);", conn);

            cmd.Parameters.Add("@name", SqlDbType.NVarChar, 200).Value = name;

            await conn.OpenAsync();

            var result = await cmd.ExecuteScalarAsync();
            if (result == null || result == DBNull.Value)
                throw new InvalidOperationException("Insert failed: no ID returned.");

            return Convert.ToInt32(result);
        }

        public async Task<bool> UpdateAsync(int id, string name)
        {
            using var conn = new SqlConnection(ConnStr);
            using var cmd = new SqlCommand(
                @"UPDATE Organizations
                  SET Name = @name
                  WHERE OrganizationId = @id;", conn);

            cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
            cmd.Parameters.Add("@name", SqlDbType.NVarChar, 200).Value = name;

            await conn.OpenAsync();

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = new SqlConnection(ConnStr);
            using var cmd = new SqlCommand(
                @"DELETE FROM Organizations
                  WHERE OrganizationId = @id;", conn);

            cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;

            await conn.OpenAsync();

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }
    }
}