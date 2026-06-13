using EnviosRapidosGT.Api.Data;
using EnviosRapidosGT.Api.Dtos;
using EnviosRapidosGT.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EnviosRapidosGT.Api.Services;

public class EnvioService
{
    private readonly AppDbContext _context;

    public EnvioService(AppDbContext context)
    {
        _context = context;
    }

    public decimal CalcularTarifa(decimal pesoKg)
    {
        if (pesoKg <= 0)
            throw new ArgumentException("El peso debe ser mayor a 0.");

        if (pesoKg < 1)
            return 25;

        if (pesoKg <= 5)
            return 45;

        if (pesoKg <= 10)
            return 75;

        return 100;
    }

    public bool NitValido(string? nit)
    {
        return !string.IsNullOrWhiteSpace(nit) && nit.Trim().Length >= 5;
    }

    public decimal CalcularDescuento(decimal tarifaBase, string? nitRemitente, string? nitDestinatario)
    {
        if (NitValido(nitRemitente) || NitValido(nitDestinatario))
            return Math.Round(tarifaBase * 0.05m, 2);

        return 0;
    }

    public string GenerarCodigoRastreo()
    {
        var fecha = DateTime.UtcNow.ToString("yyyyMMdd");
        var random = Random.Shared.Next(1000, 9999);
        return $"ENV-{fecha}-{random}";
    }

    public bool TransicionValida(EstadoEnvio actual, EstadoEnvio nuevo)
    {
        return actual switch
        {
            EstadoEnvio.Registrado => nuevo == EstadoEnvio.EnTransito,
            EstadoEnvio.EnTransito => nuevo == EstadoEnvio.EnReparto || nuevo == EstadoEnvio.Devuelto,
            EstadoEnvio.EnReparto => nuevo == EstadoEnvio.Entregado || nuevo == EstadoEnvio.Devuelto,
            _ => false
        };
    }

    public async Task<Envio> CrearEnvioAsync(CrearEnvioDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Remitente))
            throw new ArgumentException("El remitente es obligatorio.");

        if (string.IsNullOrWhiteSpace(dto.Destinatario))
            throw new ArgumentException("El destinatario es obligatorio.");

        if (string.IsNullOrWhiteSpace(dto.UbicacionInicial))
            throw new ArgumentException("La ubicación inicial es obligatoria.");

        var tarifa = CalcularTarifa(dto.PesoKg);
        var descuento = CalcularDescuento(tarifa, dto.NitRemitente, dto.NitDestinatario);

        string codigo;
        do
        {
            codigo = GenerarCodigoRastreo();
        } while (await _context.Envios.AnyAsync(e => e.CodigoRastreo == codigo));

        var envio = new Envio
        {
            CodigoRastreo = codigo,
            Remitente = dto.Remitente,
            Destinatario = dto.Destinatario,
            NitRemitente = dto.NitRemitente,
            NitDestinatario = dto.NitDestinatario,
            PesoKg = dto.PesoKg,
            TarifaBase = tarifa,
            Descuento = descuento,
            Total = tarifa - descuento,
            EstadoActual = EstadoEnvio.Registrado,
            IntentosEntrega = 0,
            UbicacionActual = dto.UbicacionInicial,
            FechaRegistro = DateTime.UtcNow
        };

        envio.Historial.Add(new HistorialEnvio
        {
            Estado = EstadoEnvio.Registrado,
            Ubicacion = dto.UbicacionInicial,
            Nota = "Envío registrado",
            FechaCambio = DateTime.UtcNow
        });

        _context.Envios.Add(envio);
        await _context.SaveChangesAsync();

        return envio;
    }

    public async Task<List<Envio>> ObtenerEnviosAsync()
    {
        return await _context.Envios
            .Include(e => e.Historial)
            .OrderByDescending(e => e.FechaRegistro)
            .ToListAsync();
    }

    public async Task<Envio?> ObtenerPorCodigoAsync(string codigo)
    {
        return await _context.Envios
            .Include(e => e.Historial)
            .FirstOrDefaultAsync(e => e.CodigoRastreo == codigo);
    }

    public async Task<Envio> ActualizarEstadoAsync(string codigo, ActualizarEstadoDto dto)
    {
        var envio = await ObtenerPorCodigoAsync(codigo);

        if (envio == null)
            throw new KeyNotFoundException("Envío no encontrado.");

        if (string.IsNullOrWhiteSpace(dto.Ubicacion))
            throw new ArgumentException("La ubicación es obligatoria.");

        if (!TransicionValida(envio.EstadoActual, dto.NuevoEstado))
            throw new InvalidOperationException($"No se puede cambiar de {envio.EstadoActual} a {dto.NuevoEstado}.");

        envio.EstadoActual = dto.NuevoEstado;
        envio.UbicacionActual = dto.Ubicacion;

        envio.Historial.Add(new HistorialEnvio
        {
            EnvioId = envio.Id,
            Estado = dto.NuevoEstado,
            Ubicacion = dto.Ubicacion,
            Nota = dto.Nota,
            FechaCambio = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return envio;
    }

    public async Task<Envio> RegistrarIntentoFallidoAsync(string codigo, string ubicacion, string? nota)
    {
        var envio = await ObtenerPorCodigoAsync(codigo);

        if (envio == null)
            throw new KeyNotFoundException("Envío no encontrado.");

        if (string.IsNullOrWhiteSpace(ubicacion))
            throw new ArgumentException("La ubicación es obligatoria.");

        if (envio.EstadoActual != EstadoEnvio.EnReparto)
            throw new InvalidOperationException("Los intentos fallidos solo se registran cuando el envío está EnReparto.");

        envio.IntentosEntrega++;

        if (envio.IntentosEntrega >= 3)
        {
            envio.EstadoActual = EstadoEnvio.Devuelto;
            envio.UbicacionActual = ubicacion;

            envio.Historial.Add(new HistorialEnvio
            {
                EnvioId = envio.Id,
                Estado = EstadoEnvio.Devuelto,
                Ubicacion = ubicacion,
                Nota = "Devuelto automáticamente por tercer intento fallido. " + nota,
                FechaCambio = DateTime.UtcNow
            });
        }
        else
        {
            envio.Historial.Add(new HistorialEnvio
            {
                EnvioId = envio.Id,
                Estado = envio.EstadoActual,
                Ubicacion = ubicacion,
                Nota = $"Intento fallido #{envio.IntentosEntrega}. {nota}",
                FechaCambio = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        return envio;
    }

    public async Task<object> ReporteEficienciaAsync()
    {
        var total = await _context.Envios.CountAsync();
        var entregados = await _context.Envios.CountAsync(e => e.EstadoActual == EstadoEnvio.Entregado);
        var devueltos = await _context.Envios.CountAsync(e => e.EstadoActual == EstadoEnvio.Devuelto);

        var eficiencia = total == 0 ? 0 : Math.Round((decimal)entregados / total * 100, 2);

        return new
        {
            totalEnvios = total,
            entregados,
            devueltos,
            eficienciaPorcentaje = eficiencia
        };
    }
}
