using System.Security.Claims;
using InsuranceApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace InsuranceApi.Controllers;

[ApiController]
[Route("api/cases")]
[Authorize(Roles = "Admin")]
public class CasesController : ControllerBase
{
    private readonly string _connectionString;

    public CasesController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Не задана строка подключения DefaultConnection.");
    }

    /// <summary>
    /// Возвращает список страховых случаев. Можно отфильтровать по договору.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CaseDto>>> GetCases([FromQuery] int? contractId)
    {
        var cases = new List<CaseDto>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        string sql = @"
            SELECT s.id_sluchaya, s.id_dogovora, d.id_dogovora AS dogovor_num,
                   k.familiya + ' ' + k.imya AS client_name,
                   s.id_admina, s.data, s.opisanie, s.summa_viplaty
            FROM KURS_Strah_Sluchai s
            JOIN KURS_Dogovor d ON s.id_dogovora = d.id_dogovora
            JOIN KURS_Klient k ON d.id_klienta = k.id_klienta";

        if (contractId.HasValue)
            sql += " WHERE s.id_dogovora = @contractId";

        sql += " ORDER BY s.data DESC, s.id_sluchaya DESC";

        await using var command = new SqlCommand(sql, connection);
        if (contractId.HasValue)
            command.Parameters.AddWithValue("@contractId", contractId.Value);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            cases.Add(MapCase(reader));

        return Ok(cases);
    }

    /// <summary>
    /// Возвращает страховой случай по идентификатору.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CaseDto>> GetCase(int id)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT s.id_sluchaya, s.id_dogovora, d.id_dogovora AS dogovor_num,
                   k.familiya + ' ' + k.imya AS client_name,
                   s.id_admina, s.data, s.opisanie, s.summa_viplaty
            FROM KURS_Strah_Sluchai s
            JOIN KURS_Dogovor d ON s.id_dogovora = d.id_dogovora
            JOIN KURS_Klient k ON d.id_klienta = k.id_klienta
            WHERE s.id_sluchaya = @id";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return NotFound(new { error = $"Страховой случай с id={id} не найден" });

        return Ok(MapCase(reader));
    }

    /// <summary>
    /// Регистрирует страховой случай. Сумма выплаты не может превышать страховую сумму договора.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CaseDto>> CreateCase([FromBody] CaseDto insuranceCase)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (insuranceCase.Data == default)
            return BadRequest(new { error = "Не указана дата страхового случая" });

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        decimal? contractSum = await GetContractSumAsync(connection, insuranceCase.IdDogovora);
        if (contractSum == null)
            return NotFound(new { error = "Договор с указанным идентификатором не найден" });

        if (insuranceCase.SummaVyplaty > contractSum.Value)
            return Conflict(new { error = "Сумма выплаты не может превышать страховую сумму по договору" });

        int adminId = GetAdminId();

        const string sql = @"
            INSERT INTO KURS_Strah_Sluchai
                (id_dogovora, id_admina, data, opisanie, summa_viplaty)
            OUTPUT INSERTED.id_sluchaya
            VALUES
                (@dogovor, @admin, @data, @opisanie, @summa)";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@dogovor", insuranceCase.IdDogovora);
        command.Parameters.AddWithValue("@admin", adminId);
        command.Parameters.AddWithValue("@data", insuranceCase.Data.Date);
        command.Parameters.AddWithValue("@opisanie", insuranceCase.Opisanie.Trim());
        command.Parameters.AddWithValue("@summa", insuranceCase.SummaVyplaty);

        int newId = Convert.ToInt32(await command.ExecuteScalarAsync());
        insuranceCase.Id = newId;
        insuranceCase.IdAdmina = adminId;

        return CreatedAtAction(nameof(GetCase), new { id = newId }, insuranceCase);
    }

    /// <summary>
    /// Изменяет данные страхового случая.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateCase(int id, [FromBody] CaseDto insuranceCase)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (insuranceCase.Data == default)
            return BadRequest(new { error = "Не указана дата страхового случая" });

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        decimal? contractSum = await GetContractSumAsync(connection, insuranceCase.IdDogovora);
        if (contractSum == null)
            return NotFound(new { error = "Договор с указанным идентификатором не найден" });

        if (insuranceCase.SummaVyplaty > contractSum.Value)
            return Conflict(new { error = "Сумма выплаты не может превышать страховую сумму по договору" });

        const string sql = @"
            UPDATE KURS_Strah_Sluchai
            SET id_dogovora = @dogovor,
                data = @data,
                opisanie = @opisanie,
                summa_viplaty = @summa
            WHERE id_sluchaya = @id";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@dogovor", insuranceCase.IdDogovora);
        command.Parameters.AddWithValue("@data", insuranceCase.Data.Date);
        command.Parameters.AddWithValue("@opisanie", insuranceCase.Opisanie.Trim());
        command.Parameters.AddWithValue("@summa", insuranceCase.SummaVyplaty);
        command.Parameters.AddWithValue("@id", id);

        int affected = await command.ExecuteNonQueryAsync();
        if (affected == 0)
            return NotFound(new { error = $"Страховой случай с id={id} не найден" });

        insuranceCase.Id = id;
        return Ok(insuranceCase);
    }

    /// <summary>
    /// Удаляет страховой случай.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCase(int id)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = "DELETE FROM KURS_Strah_Sluchai WHERE id_sluchaya = @id";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);

        int affected = await command.ExecuteNonQueryAsync();
        if (affected == 0)
            return NotFound(new { error = $"Страховой случай с id={id} не найден" });

        return NoContent();
    }

    private int GetAdminId()
    {
        string? value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out int adminId) ? adminId : 1;
    }

    private static async Task<decimal?> GetContractSumAsync(SqlConnection connection, int contractId)
    {
        const string sql = "SELECT summa FROM KURS_Dogovor WHERE id_dogovora = @id";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", contractId);

        object? value = await command.ExecuteScalarAsync();
        if (value == null || value == DBNull.Value)
            return null;

        return Convert.ToDecimal(value);
    }

    private static CaseDto MapCase(SqlDataReader reader)
    {
        return new CaseDto
        {
            Id = reader.GetInt32(0),
            IdDogovora = reader.GetInt32(1),
            DogovorNum = reader.GetInt32(2),
            ClientName = reader.IsDBNull(3) ? null : reader.GetString(3),
            IdAdmina = reader.GetInt32(4),
            Data = reader.GetDateTime(5),
            Opisanie = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
            SummaVyplaty = reader.GetDecimal(7)
        };
    }
}
