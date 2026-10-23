# AquaPrior (ASP.NET Core MVC, .NET 8)

Sistema web para el registro y la priorización de incidencias por interrupciones del servicio de agua potable en la provincia de Trujillo.

## Requisitos
- .NET 8 SDK
- SQL Server. Por defecto usa LocalDB de Windows (`(localdb)\MSSQLLocalDB`). Si usas otro servidor, cambia la cadena `ConnectionStrings:AquaPrior` en `appsettings.json`.

## Ejecutar
```
dotnet restore
dotnet run
```
Al iniciar, la aplicación crea la base de datos `AquaPrior` (EnsureCreated) y carga datos ficticios de prueba.

## Cuentas de prueba
| Rol | Correo | Contraseña |
| --- | --- | --- |
| Administrador | admin@aquaprior.pe | Admin123! |
| Ciudadano | rosa@correo.pe | Rosa123! |

## Arquitectura (capas + MVC, sección 4.4 del informe)
| Capa | Carpeta | Contenido |
| --- | --- | --- |
| Presentación | `Controllers/`, `Views/`, `ViewModels/`, `wwwroot/` | Controladores MVC, vistas Razor y modelos de vista (formularios y pantallas) |
| Negocio | `Services/` | Reglas de negocio. No depende de la capa de presentación |
| Acceso a datos | `Repositories/`, `Data/` | Interfaces y repositorios con Entity Framework Core, `AquaPriorContext`, `DbSeeder` |
| Dominio | `Models/` | Entidades `Usuario`, `Zona`, `Reporte`, enums y resultados del negocio |

## Requisitos del curso
| Requisito | Dónde está |
| --- | --- |
| Patrón Repository | `Repositories/` (`IReporteRepository`, `IZonaRepository`, `IUsuarioRepository` y sus implementaciones) |
| Segundo patrón: Strategy | `Services/IPrioridadStrategy.cs` y `Estrategias.cs` (SAW, duración, personas) |
| Maestro-detalle | `ZonasController.Detalle` y `Views/Zonas/Detalle.cshtml`: el distrito (maestro) con sus reportes (detalle), donde se agregan, editan, eliminan y verifican reportes |
| CRUD independientes | `ZonasController`, `UsuariosController` y `ReportesController`, cada uno con listar, crear, editar y eliminar |
| Validaciones | DataAnnotations en `ViewModels/` y reglas de negocio en `Services/` (ver abajo) |

## Validaciones
- Formularios: campos obligatorios, longitudes, correo válido, contraseña de al menos 8 caracteres, personas afectadas mayor a 0.
- Reportes: la fecha de inicio no puede estar en el futuro y el distrito debe existir. Un ciudadano solo edita o elimina sus reportes mientras estén en estado Recibido.
- Distritos: nombre único. No se elimina un distrito con reportes.
- Usuarios: correo único. No se elimina un usuario con reportes, ni la propia cuenta, ni al único administrador, y no se le puede quitar el rol.
- Seguridad: token antifalsificación en todos los POST, autorización por rol y validación de la URL de retorno.

## Casos de uso implementados
| Caso de uso | Dónde |
| --- | --- |
| CUS-01 Registrar reporte | `ReportesController.Crear` |
| CUS-02 Consultar zonas afectadas | `HomeController.Index` |
| CUS-03 Consultar reportes | `ReportesController.Index` y `ZonasController.Detalle` |
| CUS-04 Verificar reporte | `ReportesController.CambiarEstado` |
| CUS-05 Información consolidada | `PrioridadService.CalcularAsync` (consolida por zona) |
| CUS-06 Priorizar zonas | `AdminController.Prioridad` |
| CUS-07 Consultar estadísticas | `AdminController.Estadisticas` |
| CUS-08 Iniciar sesión | `CuentaController.Login` |
| CUS-09 Registrarse | `CuentaController.Registro` |

## Pendientes antes de la entrega
- Los pesos (50 % y 50 %) y los umbrales (alta desde 70, media desde 40) son provisionales. El informe indica que el equipo debe acordarlos (`PrioridadService.UmbralAlta` y `UmbralMedia`).
- Las fechas usan la hora del servidor (`DateTime.Now`).
- Para producción: migraciones de EF Core en lugar de `EnsureCreated`, HTTPS con certificado real, revisión de la Ley N.° 29733 y pruebas del apartado 4.5.
