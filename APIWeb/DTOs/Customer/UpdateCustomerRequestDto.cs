// ============================================================================
// ARCHIVO: DTOs/Customer/UpdateCustomerRequestDto.cs
// PROPÓSITO: DTO de entrada para editar un cliente existente (PUT /api/customers/{id}).
//            No incluye CustomerId porque la llave primaria no se modifica;
//            aquí se actualizan su empresa, contacto y dirección/ciudad/país (destino).
// ============================================================================

using System.ComponentModel.DataAnnotations;

namespace APIWeb.DTOs.Customers;

public class UpdateCustomerRequestDto
{
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
