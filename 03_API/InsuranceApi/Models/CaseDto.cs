using System.ComponentModel.DataAnnotations;

namespace InsuranceApi.Models;

/// <summary>
/// Страховой случай. Поля DogovorNum и ClientName заполняются только при чтении.
/// </summary>
public class CaseDto
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Не указан договор")]
    public int IdDogovora { get; set; }

    public int DogovorNum { get; set; }

    public string? ClientName { get; set; }

    public int IdAdmina { get; set; }

    public DateTime Data { get; set; }

    [Required(ErrorMessage = "Описание страхового случая обязательно")]
    [StringLength(500, ErrorMessage = "Описание не должно превышать 500 символов")]
    public string Opisanie { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335",
        ErrorMessage = "Сумма выплаты должна быть положительной")]
    public decimal SummaVyplaty { get; set; }
}
