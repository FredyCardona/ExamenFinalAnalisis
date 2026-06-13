using EnviosRapidosGT.Api.Models;

namespace EnviosRapidosGT.Api.Dtos;

public class CrearEnvioDto
{
    public string Remitente { get; set; } = string.Empty;
    public string Destinatario { get; set; } = string.Empty;
    public string? NitRemitente { get; set; }
    public string? NitDestinatario { get; set; }
    public decimal PesoKg { get; set; }
    public string UbicacionInicial { get; set; } = string.Empty;
}

public class ActualizarEstadoDto
{
    public EstadoEnvio NuevoEstado { get; set; }
    public string Ubicacion { get; set; } = string.Empty;
    public string? Nota { get; set; }
}

public class RegistrarIntentoFallidoDto
{
    public string Ubicacion { get; set; } = string.Empty;
    public string? Nota { get; set; }
}
