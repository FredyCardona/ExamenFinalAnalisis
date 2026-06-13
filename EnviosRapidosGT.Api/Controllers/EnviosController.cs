using EnviosRapidosGT.Api.Dtos;
using EnviosRapidosGT.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EnviosRapidosGT.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EnviosController : ControllerBase
{
    private readonly EnvioService _service;

    public EnviosController(EnvioService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearEnvioDto dto)
    {
        try
        {
            var envio = await _service.CrearEnvioAsync(dto);
            return CreatedAtAction(nameof(ObtenerPorCodigo), new { codigo = envio.CodigoRastreo }, envio);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerTodos()
    {
        var envios = await _service.ObtenerEnviosAsync();
        return Ok(envios);
    }

    [HttpGet("{codigo}")]
    public async Task<IActionResult> ObtenerPorCodigo(string codigo)
    {
        var envio = await _service.ObtenerPorCodigoAsync(codigo);

        if (envio == null)
            return NotFound(new { mensaje = "Envío no encontrado." });

        return Ok(envio);
    }

    [HttpGet("{codigo}/historial")]
    public async Task<IActionResult> ObtenerHistorial(string codigo)
    {
        var envio = await _service.ObtenerPorCodigoAsync(codigo);

        if (envio == null)
            return NotFound(new { mensaje = "Envío no encontrado." });

        return Ok(envio.Historial.OrderBy(h => h.FechaCambio));
    }

    [HttpPut("{codigo}/estado")]
    public async Task<IActionResult> ActualizarEstado(string codigo, [FromBody] ActualizarEstadoDto dto)
    {
        try
        {
            var envio = await _service.ActualizarEstadoAsync(codigo, dto);
            return Ok(envio);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    [HttpPost("{codigo}/intento-fallido")]
    public async Task<IActionResult> RegistrarIntentoFallido(string codigo, [FromBody] RegistrarIntentoFallidoDto dto)
    {
        try
        {
            var envio = await _service.RegistrarIntentoFallidoAsync(codigo, dto.Ubicacion, dto.Nota);
            return Ok(envio);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    [HttpGet("/api/reportes/eficiencia")]
    public async Task<IActionResult> ReporteEficiencia()
    {
        var reporte = await _service.ReporteEficienciaAsync();
        return Ok(reporte);
    }
}
