using APIWeb.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIWeb.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Employee")]
public class SuppliersController : ControllerBase
{
    private readonly IProductService _productService;

    public SuppliersController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    public async Task<IActionResult> GetSuppliers()
    {
        var result = await _productService.ObtenerProveedoresAsync();
        return Ok(result.Data);
    }
}
