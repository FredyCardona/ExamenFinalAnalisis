using System.ComponentModel.DataAnnotations;

namespace EnviosRapidosGT.Api.Models;

public enum EstadoEnvio
{
    Registrado = 1,
    EnTransito = 2,
    EnReparto = 3,
    Entregado = 4,
    Devuelto = 5
}

public class Envio
{
    public int Id { get; set; }

    [Required]
    public string CodigoRastreo { get; set; } = string.Empty;

    [Required]
    public string Remitente { get; set; } = string.Empty;

    [Required]
    public string Destinatario { get; set; } = string.Empty;

    public string? NitRemitente { get; set; }
    public string? NitDestinatario { get; set; }

    public decimal PesoKg { get; set; }
    public decimal TarifaBase { get; set; }
    public decimal Descuento { get; set; }
    public decimal Total { get; set; }

    public EstadoEnvio EstadoActual { get; set; } = EstadoEnvio.Registrado;
    public int IntentosEntrega { get; set; } = 0;

    [Required]
    public string UbicacionActual { get; set; } = string.Empty;

    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    public List<HistorialEnvio> Historial { get; set; } = new();
}
