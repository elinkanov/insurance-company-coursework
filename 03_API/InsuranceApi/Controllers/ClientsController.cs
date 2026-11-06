using InsuranceApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace InsuranceApi.Controllers;

[ApiController]
[Route("api/clients")]
[Authorize(Roles = "Admin")]
public class ClientsController : ControllerBase
{
    private readonly string _connectionString;

    public ClientsController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Не задана строка подключения DefaultConnection.");
    }

    /// <summary>
    /// Возвращает список клиентов. Параметр search выполняет поиск по ФИО или телефону.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClientDto>>> GetClients([FromQuery] string? search)
    {
        var clients = new List<ClientDto>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        string sql = @"
            SELECT id_klienta, familiya, imya, otchestvo,
                   data_rozhdeniya, pasport, adres, telefon
            FROM KURS_Klient";

        if (!string.IsNullOrWhiteSpace(search))
        {
            sql += @"
                WHERE familiya LIKE @search
                   OR imya LIKE @search
                   OR otchestvo LIKE @search
                   OR telefon LIKE @search";
        }

        sql += " ORDER BY familiya, imya";

        await using var command = new SqlCommand(sql, connection);
        if (!string.IsNullOrWhiteSpace(search))
            command.Parameters.AddWithValue("@search", "%" + search.Trim() + "%");

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            clients.Add(MapClient(reader));

        return Ok(clients);
    }

    /// <summary>
    /// Возвращает одного клиента по идентификатору.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClientDto>> GetClient(int id)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT id_klienta, familiya, imya, otchestvo,
                   data_rozhdeniya, pasport, adres, telefon
            FROM KURS_Klient
            WHERE id_klienta = @id";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return NotFound(new { error = $"Клиент с id={id} не найден" });

        return Ok(MapClient(reader));
    }

    /// <summary>
    /// Добавляет нового клиента.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ClientDto>> CreateClient([FromBody] ClientDto client)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        if (!string.IsNullOrWhiteSpace(client.Pasport))
        {
            const string checkSql = "SELECT COUNT(1) FROM KURS_Klient WHERE pasport = @pasport";
            await using var checkCommand = new SqlCommand(checkSql, connection);
            checkCommand.Parameters.AddWithValue("@pasport", client.Pasport.Trim());

            int exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
            if (exists > 0)
                return Conflict(new { error = "Клиент с таким паспортом уже существует" });
        }

        const string sql = @"
            INSERT INTO KURS_Klient
                (familiya, imya, otchestvo, data_rozhdeniya, pasport, adres, telefon)
            OUTPUT INSERTED.id_klienta
            VALUES
                (@familiya, @imya, @otchestvo, @data, @pasport, @adres, @telefon)";

        await using var command = new SqlCommand(sql, connection);
        AddClientParameters(command, client);

        int newId = Convert.ToInt32(await command.ExecuteScalarAsync());
        client.Id = newId;

        return CreatedAtAction(nameof(GetClient), new { id = newId }, client);
    }

    /// <summary>
    /// Изменяет данные клиента.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateClient(int id, [FromBody] ClientDto client)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        if (!string.IsNullOrWhiteSpace(client.Pasport))
        {
            const string checkSql = @"
                SELECT COUNT(1) FROM KURS_Klient
                WHERE pasport = @pasport AND id_klienta <> @id";

            await using var checkCommand = new SqlCommand(checkSql, connection);
            checkCommand.Parameters.AddWithValue("@pasport", client.Pasport.Trim());
            checkCommand.Parameters.AddWithValue("@id", id);

            int exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
            if (exists > 0)
                return Conflict(new { error = "Клиент с таким паспортом уже существует" });
        }

        const string sql = @"
            UPDATE KURS_Klient
            SET familiya = @familiya,
                imya = @imya,
                otchestvo = @otchestvo,
                data_rozhdeniya = @data,
                pasport = @pasport,
                adres = @adres,
                telefon = @telefon
            WHERE id_klienta = @id";

        await using var command = new SqlCommand(sql, connection);
        AddClientParameters(command, client);
        command.Parameters.AddWithValue("@id", id);

        int affected = await command.ExecuteNonQueryAsync();
        if (affected == 0)
            return NotFound(new { error = $"Клиент с id={id} не найден" });

        client.Id = id;
        return Ok(client);
    }

    /// <summary>
    /// Удаляет клиента, если у него нет договоров.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteClient(int id)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        const string checkSql = "SELECT COUNT(1) FROM KURS_Dogovor WHERE id_klienta = @id";
        await using var checkCommand = new SqlCommand(checkSql, connection);
        checkCommand.Parameters.AddWithValue("@id", id);

        int contracts = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
        if (contracts > 0)
            return Conflict(new { error = "Нельзя удалить клиента: у него есть оформленные договоры" });

        const string sql = "DELETE FROM KURS_Klient WHERE id_klienta = @id";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);

        int affected = await command.ExecuteNonQueryAsync();
        if (affected == 0)
            return NotFound(new { error = $"Клиент с id={id} не найден" });

        return NoContent();
    }

    private static void AddClientParameters(SqlCommand command, ClientDto client)
    {
        command.Parameters.AddWithValue("@familiya", client.Familiya.Trim());
        command.Parameters.AddWithValue("@imya", client.Imya.Trim());
        command.Parameters.AddWithValue("@otchestvo", (object?)client.Otchestvo?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@data", (object?)client.DataRozhdeniya ?? DBNull.Value);
        command.Parameters.AddWithValue("@pasport", (object?)client.Pasport?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@adres", (object?)client.Adres?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@telefon", (object?)client.Telefon?.Trim() ?? DBNull.Value);
    }

    private static ClientDto MapClient(SqlDataReader reader)
    {
        return new ClientDto
        {
            Id = reader.GetInt32(0),
            Familiya = reader.GetString(1),
            Imya = reader.GetString(2),
            Otchestvo = reader.IsDBNull(3) ? null : reader.GetString(3),
            DataRozhdeniya = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
            Pasport = reader.IsDBNull(5) ? null : reader.GetString(5),
            Adres = reader.IsDBNull(6) ? null : reader.GetString(6),
            Telefon = reader.IsDBNull(7) ? null : reader.GetString(7)
        };
    }
}
