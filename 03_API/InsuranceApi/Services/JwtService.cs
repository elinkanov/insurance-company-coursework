using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace InsuranceApi.Services;

/// <summary>
/// Формирует JWT-токен для администратора после успешной проверки логина и пароля.
/// </summary>
public class JwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public int LifetimeSeconds =>
        (int.TryParse(_configuration["Jwt:LifetimeMinutes"], out int minutes) ? minutes : 60) * 60;

    public string CreateToken(int adminId, string login, string fio)
    {
        string key = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("В конфигурации не задан ключ Jwt:Key.");
        string issuer = _configuration["Jwt:Issuer"] ?? "InsuranceApi";
        string audience = _configuration["Jwt:Audience"] ?? "InsuranceClients";

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, adminId.ToString()),
            new(ClaimTypes.NameIdentifier, adminId.ToString()),
            new(ClaimTypes.Name, login),
            new(ClaimTypes.Role, "Admin"),
            new("fio", fio)
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddSeconds(LifetimeSeconds),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
