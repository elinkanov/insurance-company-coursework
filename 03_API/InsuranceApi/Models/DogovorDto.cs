using System.ComponentModel.DataAnnotations;

namespace InsuranceApi.Models;

/// <summary>
/// Договор страхования. Поля ClientName, VidStrah и AgentName заполняются
/// только при чтении (они получаются соединением таблиц).
/// </summary>
public class DogovorDto
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Не указан клиент")]
    public int IdKlienta { get; set; }

    public string? ClientName { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Не указан вид страхования")]
    public int IdVida { get; set; }

    public string? VidStrah { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Не указан агент")]
    public int IdAgenta { get; set; }

    public string? AgentName { get; set; }

    public DateTime DataZaklucheniya { get; set; }

    [Range(1, 600, ErrorMessage = "Срок действия должен быть от 1 до 600 месяцев")]
    public int Srok { get; set; }

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335",
        ErrorMessage = "Страховая сумма должна быть положительной")]
    public decimal Summa { get; set; }
}
