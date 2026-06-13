using EnviosRapidosGT.Api.Data;
using EnviosRapidosGT.Api.Dtos;
using EnviosRapidosGT.Api.Models;
using EnviosRapidosGT.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace EnviosRapidosGT.Tests;

public class EnvioServiceTests
{
    private EnvioService CrearServicio(out AppDbContext context)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        context = new AppDbContext(options);
        return new EnvioService(context);
    }

    [Theory]
    [InlineData(0.5, 25)]
    [InlineData(2, 45)]
    [InlineData(7, 75)]
    [InlineData(12, 100)]
    public void CalcularTarifa_SegunPeso_RetornaTarifaCorrecta(decimal peso, decimal esperado)
    {
        var service = CrearServicio(out _);

        var resultado = service.CalcularTarifa(peso);

        Assert.Equal(esperado, resultado);
    }

    [Fact]
    public void CalcularTarifa_ConPesoInvalido_LanzaError()
    {
        var service = CrearServicio(out _);

        Assert.Throws<ArgumentException>(() => service.CalcularTarifa(0));
    }

    [Fact]
    public void CalcularDescuento_ConNitValido_AplicaCincoPorCiento()
    {
        var service = CrearServicio(out _);

        var descuento = service.CalcularDescuento(100, "1234567", "");

        Assert.Equal(5, descuento);
    }

    [Fact]
    public void CalcularDescuento_SinNitValido_NoAplicaDescuento()
    {
        var service = CrearServicio(out _);

        var descuento = service.CalcularDescuento(100, "", "");

        Assert.Equal(0, descuento);
    }

    [Fact]
    public async Task CrearEnvio_GeneraCodigoRastreoYEstadoRegistrado()
    {
        var service = CrearServicio(out _);

        var envio = await service.CrearEnvioAsync(new CrearEnvioDto
        {
            Remitente = "Juan Perez",
            Destinatario = "Maria Lopez",
            NitRemitente = "1234567",
            PesoKg = 4.5m,
            UbicacionInicial = "Guatemala"
        });

        Assert.Matches(@"^ENV-\d{8}-\d{4}$", envio.CodigoRastreo);
        Assert.Equal(EstadoEnvio.Registrado, envio.EstadoActual);
        Assert.Equal(45, envio.TarifaBase);
        Assert.Equal(2.25m, envio.Descuento);
        Assert.Equal(42.75m, envio.Total);
        Assert.Single(envio.Historial);
    }

    [Fact]
    public async Task CrearEnvio_ConPesoCero_LanzaError()
    {
        var service = CrearServicio(out _);

        await Assert.ThrowsAsync<ArgumentException>(() => service.CrearEnvioAsync(new CrearEnvioDto
        {
            Remitente = "Juan Perez",
            Destinatario = "Maria Lopez",
            PesoKg = 0,
            UbicacionInicial = "Guatemala"
        }));
    }

    [Fact]
    public void TransicionValida_NoPermiteSaltarDeRegistradoAEntregado()
    {
        var service = CrearServicio(out _);

        var resultado = service.TransicionValida(EstadoEnvio.Registrado, EstadoEnvio.Entregado);

        Assert.False(resultado);
    }

    [Fact]
    public async Task ActualizarEstado_EnOrden_GuardaHistorial()
    {
        var service = CrearServicio(out _);

        var envio = await service.CrearEnvioAsync(new CrearEnvioDto
        {
            Remitente = "Juan Perez",
            Destinatario = "Maria Lopez",
            PesoKg = 2,
            UbicacionInicial = "Guatemala"
        });

        var actualizado = await service.ActualizarEstadoAsync(envio.CodigoRastreo, new ActualizarEstadoDto
        {
            NuevoEstado = EstadoEnvio.EnTransito,
            Ubicacion = "Centro de distribucion Guatemala",
            Nota = "Paquete salio de oficina central"
        });

        Assert.Equal(EstadoEnvio.EnTransito, actualizado.EstadoActual);
        Assert.Equal(2, actualizado.Historial.Count);
    }

    [Fact]
    public async Task TercerIntentoFallido_CambiaEstadoADevuelto()
    {
        var service = CrearServicio(out _);

        var envio = await service.CrearEnvioAsync(new CrearEnvioDto
        {
            Remitente = "Juan Perez",
            Destinatario = "Maria Lopez",
            PesoKg = 3,
            UbicacionInicial = "Guatemala"
        });

        await service.ActualizarEstadoAsync(envio.CodigoRastreo, new ActualizarEstadoDto
        {
            NuevoEstado = EstadoEnvio.EnTransito,
            Ubicacion = "Centro de distribucion Guatemala"
        });

        await service.ActualizarEstadoAsync(envio.CodigoRastreo, new ActualizarEstadoDto
        {
            NuevoEstado = EstadoEnvio.EnReparto,
            Ubicacion = "Sanarate"
        });

        await service.RegistrarIntentoFallidoAsync(envio.CodigoRastreo, "Sanarate", "No contestaron");
        await service.RegistrarIntentoFallidoAsync(envio.CodigoRastreo, "Sanarate", "Casa cerrada");
        var resultado = await service.RegistrarIntentoFallidoAsync(envio.CodigoRastreo, "Sanarate", "Tercer intento fallido");

        Assert.Equal(3, resultado.IntentosEntrega);
        Assert.Equal(EstadoEnvio.Devuelto, resultado.EstadoActual);
    }
}
