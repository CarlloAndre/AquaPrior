namespace AquaPrior.Models;

public enum Rol { Ciudadano = 0, Administrador = 1 }

public enum TipoProblema { CorteTotal = 0, BajaPresion = 1, CalidadAgua = 2, Otro = 3 }

public enum EstadoAtencion { Pendiente = 0, EnAtencion = 1 }

public enum EstadoReporte { Recibido = 0, Verificado = 1, Duplicado = 2, Rechazado = 3 }
