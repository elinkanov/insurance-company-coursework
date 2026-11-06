using InsuranceApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace InsuranceApi.Controllers;

[ApiController]
[Route("api/dogovors")]
[Authorize(Roles = "Admin")]
public class DogovorsController : ControllerBase
{
    private readonly string _connectionString;

    public DogovorsController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Не задана строка подключения DefaultConnection.");
    }

    /// <summary>
    /// Возвращает список договоров. Можно отфильтровать по клиенту и периоду заключения.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DogovorDto>>> GetDogovors(
        [FromQuery] int? clientId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo)
    {
        var dogovors = new List<DogovorDto>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        string sql = @"
            SELECT d.id_dogovora, d.id_klienta,
                   k.familiya + ' ' + k.imya AS client_name,
                   d.id_vida, v.nazvanie AS vid_name,
                   d.id_agenta, a.familiya + ' ' + a.imya AS agent_name,
                   d.data_zaklucheniya, d.srok, d.summa
            FROM KURS_Dogovor d
            JOIN KURS_Klient k ON d.id_klienta = k.id_klienta
            JOIN KURS_Vid_Strah v ON d.id_vida = v.id_vida
            JOIN KURS_Agent a ON d.id_agenta = a.id_agenta
            WHERE 1 = 1";

        if (clientId.HasValue)
            sql += " AND d.id_klienta = @clientId";
        if (dateFrom.HasValue)
            sql += " AND d.data_zaklucheniya >= @dateFrom";
        if (dateTo.HasValue)
            sql += " AND d.data_zaklucheniya <= @dateTo";

        sql += " ORDER BY d.data_zaklucheniya DESC, d.id_dogovora DESC";

        await using var command = new SqlCommand(sql, connection);
        if (clientId.HasValue)
            command.Parameters.AddWithValue("@clientId", clientId.Value);
        if (dateFrom.HasValue)
            command.Parameters.AddWithValue("@dateFrom", dateFrom.Value.Date);
        if (dateTo.HasValue)
            command.Parameters.AddWithValue("@dateTo", dateTo.Value.Date);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            dogovors.Add(MapDogovor(reader));

        return Ok(dogovors);
    }

    /// <summary>
    /// Возвращает договор по идентификатору.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<DogovorDto>> GetDogovor(int id)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT d.id_dogovora, d.id_klienta,
                   k.familiya + ' ' + k.imya AS client_name,
                   d.id_vida, v.nazvanie AS vid_name,
                   d.id_agenta, a.familiya + ' ' + a.imya AS agent_name,
                   d.data_zaklucheniya, d.srok, d.summa
            FROM KURS_Dogovor d
            JOIN KURS_Klient k ON d.id_klienta = k.id_klienta
            JOIN KURS_Vid_Strah v ON d.id_vida = v.id_vida
            JOIN KURS_Agent a ON d.id_agenta = a.id_agenta
            WHERE d.id_dogovora = @id";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return NotFound(new { error = $"Договор с id={id} не найден" });

        return Ok(MapDogovor(reader));
    }

    /// <summary>
    /// Оформляет новый договор страхования.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<DogovorDto>> CreateDogovor([FromBody] DogovorDto dogovor)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (dogovor.DataZaklucheniya == default)
            return BadRequest(new { error = "Не указана дата заключения договора" });

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        string? referenceError = await CheckReferencesAsync(
            connection, dogovor.IdKlienta, dogovor.IdVida, dogovor.IdAgenta);
        if (referenceError != null)
            return BadRequest(new { error = referenceError });

        const string sql = @"
            INSERT INTO KURS_Dogovor
                (id_klienta, id_vida, id_agenta, data_zaklucheniya, srok, summa)
            OUTPUT INSERTED.id_dogovora
            VALUES
                (@klient, @vid, @agent, @data, @srok, @summa)";

        await using var command = new SqlCommand(sql, connection);
        AddDogovorParameters(command, dogovor);

        int newId = Convert.ToInt32(await command.ExecuteScalarAsync());
        dogovor.Id = newId;

        return CreatedAtAction(nameof(GetDogovor), new { id = newId }, dogovor);
    }

    /// <summary>
    /// Изменяет договор страхования.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateDogovor(int id, [FromBody] DogovorDto dogovor)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (dogovor.DataZaklucheniya == default)
            return BadRequest(new { error = "Не указана дата заключения договора" });

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        string? referenceError = await CheckReferencesAsync(
            connection, dogovor.IdKlienta, dogovor.IdVida, dogovor.IdAgenta);
        if (referenceError != null)
            return BadRequest(new { error = referenceError });

        const string sql = @"
            UPDATE KURS_Dogovor
            SET id_klienta = @klient,
                id_vida = @vid,
                id_agenta = @agent,
                data_zaklucheniya = @data,
                srok = @srok,
                summa = @summa
            WHERE id_dogovora = @id";

        await using var command = new SqlCommand(sql, connection);
        AddDogovorParameters(command, dogovor);
        command.Parameters.AddWithValue("@id", id);

        int affected = await command.ExecuteNonQueryAsync();
        if (affected == 0)
            return NotFound(new { error = $"Договор с id={id} не найден" });

        dogovor.Id = id;
        return Ok(dogovor);
    }

    /// <summary>
    /// Удаляет договор, если по нему нет зарегистрированных страховых случаев.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteDogovor(int id)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        const string checkSql = "SELECT COUNT(1) FROM KURS_Strah_Sluchai WHERE id_dogovora = @id";
        await using var checkCommand = new SqlCommand(checkSql, connection);
        checkCommand.Parameters.AddWithValue("@id", id);

        int cases = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
        if (cases > 0)
            return Conflict(new { error = "Нельзя удалить договор: по нему есть страховые случаи" });

        const string sql = "DELETE FROM KURS_Dogovor WHERE id_dogovora = @id";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);

        int affected = await command.ExecuteNonQueryAsync();
        if (affected == 0)
            return NotFound(new { error = $"Договор с id={id} не найден" });

        return NoContent();
    }

    private static async Task<string?> CheckReferencesAsync(
        SqlConnection connection, int clientId, int vidId, int agentId)
    {
        const string sql = @"
            SELECT
                (SELECT COUNT(1) FROM KURS_Klient WHERE id_klienta = @client) AS clients_count,
                (SELECT COUNT(1) FROM KURS_Vid_Strah WHERE id_vida = @vid) AS vids_count,
                (SELECT COUNT(1) FROM KURS_Agent WHERE id_agenta = @agent) AS agents_count";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@client", clientId);
        command.Parameters.AddWithValue("@vid", vidId);
        command.Parameters.AddWithValue("@agent", agentId);

        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();

        if (reader.GetInt32(0) == 0)
            return "Клиент с указанным идентификатором не найден";
        if (reader.GetInt32(1) == 0)
            return "Вид страхования с указанным идентификатором не найден";
        if (reader.GetInt32(2) == 0)
            return "Агент с указанным идентификатором не найден";

        return null;
    }

    private static void AddDogovorParameters(SqlCommand command, DogovorDto dogovor)
    {
        command.Parameters.AddWithValue("@klient", dogovor.IdKlienta);
        command.Parameters.AddWithValue("@vid", dogovor.IdVida);
        command.Parameters.AddWithValue("@agent", dogovor.IdAgenta);
        command.Parameters.AddWithValue("@data", dogovor.DataZaklucheniya.Date);
        command.Parameters.AddWithValue("@srok", dogovor.Srok);
        command.Parameters.AddWithValue("@summa", dogovor.Summa);
    }

    private static DogovorDto MapDogovor(SqlDataReader reader)
    {
        return new DogovorDto
        {
            Id = reader.GetInt32(0),
            IdKlienta = reader.GetInt32(1),
            ClientName = reader.IsDBNull(2) ? null : reader.GetString(2),
            IdVida = reader.GetInt32(3),
            VidStrah = reader.IsDBNull(4) ? null : reader.GetString(4),
            IdAgenta = reader.GetInt32(5),
            AgentName = reader.IsDBNull(6) ? null : reader.GetString(6),
            DataZaklucheniya = reader.GetDateTime(7),
            Srok = reader.GetInt32(8),
            Summa = reader.GetDecimal(9)
        };
    }
}
