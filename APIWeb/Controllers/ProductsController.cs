// ============================================================================
// ARCHIVO: Controllers/ProductsController.cs
// PROPÓSITO: Endpoints del Catálogo de Productos (/api/Products).
//            AUTORIZACIÓN DIFERENCIADA POR OPERACIÓN:
//            - GET (consultar catálogo): permitido para Admin, Employee y Customer.
//              (Solo Admin y Employee pueden ver productos descontinuados).
//            - POST, PUT y DELETE (crear, editar y descontinuar): restringido con
//              [Authorize(Roles = "Admin,Employee")].
// ============================================================================

using System.Security.Claims;
using APIWeb.DTOs.Products;
using APIWeb.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIWeb.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Employee,Customer")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    // GET /api/Products -> Lista productos (en POS muestra solo activos; en Catálogo el staff ve también descontinuados)
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeDiscontinued = false)
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var showDiscontinued = includeDiscontinued && (role == "Admin" || role == "Employee");

        var result = await _productService.ObtenerTodosAsync(showDiscontinued);
        return Ok(result.Data);
    }

    // GET /api/Products/{id} -> Obtiene un producto por su ID
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _productService.ObtenerPorIdAsync(id);
        if (!result.Success)
            return StatusCode(result.StatusCode, new { success = false, message = result.Message, errors = Array.Empty<string>() });

        return Ok(result.Data);
    }

    // POST /api/Products -> Crea un nuevo producto (solo Admin y Employee)
    [HttpPost]
    [Authorize(Roles = "Admin,Employee")]
    public async Task<IActionResult> Create([FromBody] CreateProductRequestDto request)
    {
        var result = await _productService.CrearAsync(request);
        if (!result.Success)
            return StatusCode(result.StatusCode, new { success = false, message = result.Message, errors = Array.Empty<string>() });

        return StatusCode(result.StatusCode, result.Data);
    }

    // PUT /api/Products/{id} -> Edita o reactiva un producto existente (solo Admin y Employee)
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Employee")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductRequestDto request)
    {
        var result = await _productService.EditarAsync(id, request);
        if (!result.Success)
            return StatusCode(result.StatusCode, new { success = false, message = result.Message, errors = Array.Empty<string>() });

        return Ok(result.Data);
    }

    // DELETE /api/Products/{id} -> Baja lógica del producto marcando Discontinued = true (solo Admin y Employee)
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,Employee")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _productService.DescontinuarAsync(id);
        if (!result.Success)
            return StatusCode(result.StatusCode, new { success = false, message = result.Message, errors = Array.Empty<string>() });

        return Ok(new { success = true, message = result.Message });
    }
}
