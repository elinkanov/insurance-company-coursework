using System.ComponentModel.DataAnnotations;

namespace InsuranceApi.Models;

/// <summary>
/// Клиент страховой компании. Используется и для чтения, и для создания/изменения записи.
/// </summary>
public class ClientDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Фамилия обязательна для заполнения")]
    [StringLength(50, ErrorMessage = "Фамилия не должна превышать 50 символов")]
    public string Familiya { get; set; } = string.Empty;

    [Required(ErrorMessage = "Имя обязательно для заполнения")]
    [StringLength(50, ErrorMessage = "Имя не должно превышать 50 символов")]
    public string Imya { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "Отчество не должно превышать 50 символов")]
    public string? Otchestvo { get; set; }

    public DateTime? DataRozhdeniya { get; set; }

    [StringLength(20, ErrorMessage = "Паспорт не должен превышать 20 символов")]
    public string? Pasport { get; set; }

    [StringLength(200, ErrorMessage = "Адрес не должен превышать 200 символов")]
    public string? Adres { get; set; }

    [StringLength(20, ErrorMessage = "Телефон не должен превышать 20 символов")]
    public string? Telefon { get; set; }
}
