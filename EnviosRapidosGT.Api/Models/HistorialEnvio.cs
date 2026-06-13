using System.ComponentModel.DataAnnotations;

namespace EnviosRapidosGT.Api.Models;

public class HistorialEnvio
{
    public int Id { get; set; }

    public int EnvioId { get; set; }

    public EstadoEnvio Estado { get; set; }

    [Required]
    public string Ubicacion { get; set; } = string.Empty;

    public string? Nota { get; set; }

    public DateTime FechaCambio { get; set; } = DateTime.UtcNow;

    public Envio? Envio { get; set; }
}
