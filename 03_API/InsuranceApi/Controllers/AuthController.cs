using InsuranceApi.Models;
using InsuranceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace InsuranceApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly string _connectionString;
    private readonly JwtService _jwtService;

    public AuthController(IConfiguration configuration, JwtService jwtService)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Не задана строка подключения DefaultConnection.");
        _jwtService = jwtService;
    }

    /// <summary>
    /// Проверяет логин и пароль администратора и возвращает JWT-токен.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Login) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { error = "Логин и пароль обязательны для заполнения" });

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT TOP (1) id_admina, familiya, imya, otchestvo
            FROM KURS_Admin
            WHERE login = @login AND parol = @password";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@login", request.Login.Trim());
        command.Parameters.AddWithValue("@password", request.Password);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return Unauthorized(new { error = "Неверный логин или пароль" });

        int adminId = reader.GetInt32(0);
        string familiya = reader.GetString(1);
        string imya = reader.GetString(2);
        string otchestvo = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);
        string fio = $"{familiya} {imya} {otchestvo}".Trim();

        string token = _jwtService.CreateToken(adminId, request.Login.Trim(), fio);

        return Ok(new LoginResponse
        {
            Token = token,
            ExpiresIn = _jwtService.LifetimeSeconds,
            Fio = fio,
            Role = "Admin"
        });
    }
}
