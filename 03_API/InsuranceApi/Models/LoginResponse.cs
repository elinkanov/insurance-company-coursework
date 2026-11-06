namespace InsuranceApi.Models;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }
    public string Fio { get; set; } = string.Empty;
    public string Role { get; set; } = "Admin";
}
