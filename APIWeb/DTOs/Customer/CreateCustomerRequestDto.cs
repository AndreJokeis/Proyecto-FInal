using System.ComponentModel.DataAnnotations;

namespace APIWeb.DTOs.Customers;

public class CreateCustomerRequestDto
{
    [Required(ErrorMessage = "El ID del cliente es obligatorio.")]
    [StringLength(5, MinimumLength = 3, ErrorMessage = "El CustomerId debe tener entre 3 y 5 caracteres.")]
    [RegularExpression("^[A-Za-z0-9]+$", ErrorMessage = "El CustomerId solo puede contener letras y números.")]
    public string CustomerId { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre de la empresa es obligatorio.")]
    [StringLength(40, ErrorMessage = "Máximo 40 caracteres.")]
    public string CompanyName { get; set; } = string.Empty;

    [StringLength(30)]
    public string? ContactName { get; set; }

    [StringLength(30)]
    public string? ContactTitle { get; set; }

    [StringLength(60)]
    public string? Address { get; set; }

    [StringLength(15)]
    public string? City { get; set; }

    [StringLength(15)]
    public string? Region { get; set; }

    [StringLength(10)]
    public string? PostalCode { get; set; }

    [StringLength(15)]
    public string? Country { get; set; }

    [StringLength(24)]
    public string? Phone { get; set; }
}