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

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeDiscontinued = false)
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var showDiscontinued = includeDiscontinued && (role == "Admin" || role == "Employee");

        var result = await _productService.ObtenerTodosAsync(showDiscontinued);
        return Ok(result.Data);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _productService.ObtenerPorIdAsync(id);
        if (!result.Success)
            return StatusCode(result.StatusCode, new { success = false, message = result.Message, errors = Array.Empty<string>() });

        return Ok(result.Data);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Employee")]
    public async Task<IActionResult> Create([FromBody] CreateProductRequestDto request)
    {
        var result = await _productService.CrearAsync(request);
        if (!result.Success)
            return StatusCode(result.StatusCode, new { success = false, message = result.Message, errors = Array.Empty<string>() });

        return StatusCode(result.StatusCode, result.Data);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Employee")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductRequestDto request)
    {
        var result = await _productService.EditarAsync(id, request);
        if (!result.Success)
            return StatusCode(result.StatusCode, new { success = false, message = result.Message, errors = Array.Empty<string>() });

        return Ok(result.Data);
    }

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
