# AquaPrior (ASP.NET Core MVC, .NET 8)

Sistema web para registrar problemas del suministro de agua potable y priorizar interrupciones totales confirmadas en la provincia de Trujillo. Los avisos ciudadanos y los incidentes verificados se gestionan por separado.

## Requisitos
- .NET 8 SDK
- SQL Server. Por defecto usa LocalDB de Windows (`(localdb)\MSSQLLocalDB`). Si usas otro servidor, cambia la cadena `ConnectionStrings:AquaPrior` en `appsettings.json`.

## Base de datos: Code First con migraciones
Las clases de `Models/` definen las tablas. La base se crea y se actualiza con migraciones de EF Core.

Primera vez (genera la carpeta `Migrations/`). En Visual Studio: Herramientas, Administrador de paquetes NuGet, Consola del Administrador de paquetes, con `AquaPrior` como proyecto predeterminado:
```
Add-Migration Inicial
Update-Database
```
O con la CLI, desde la carpeta del proyecto:
```
dotnet tool install --global dotnet-ef
dotnet ef migrations add Inicial
dotnet ef database update
```
Cuando cambies una entidad: `Add-Migration NombreDelCambio` y `Update-Database`. Al iniciar, la aplicación también aplica las migraciones pendientes (`Database.Migrate()`).

## Ejecutar
```
dotnet restore
dotnet run
```
Al iniciar, la aplicación aplica las migraciones y crea los 12 distritos y una cuenta de administrador si no existen. No carga reportes ni ciudadanos de ejemplo.

## Cuenta inicial
| Rol | Correo | Contraseña |
| --- | --- | --- |
| Administrador | admin@aquaprior.pe | Admin123! |

Los ciudadanos se crean desde Registrarse. Cambia la contraseña del administrador en `Data/DbSeeder.cs` antes de entregar.

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
| Maestro-detalle (muchos a muchos) | El reporte es el maestro y su distrito es una línea de detalle en la tabla intermedia `ReporteZona`, con las personas afectadas. El usuario ya no elige el distrito: `DistritoLocator` lo deduce de las coordenadas exactas (punto dentro de polígono sobre el KML) y `ReporteService` crea la línea. Cada aviso queda asociado a su distrito geográfico, y el esquema académico conserva `ReporteZona` como detalle. La agrupación de varios avisos forma un incidente. También `ZonasController.Detalle`: un distrito con sus reportes |
| CRUD independientes | `ZonasController`, `UsuariosController` y `ReportesController`, cada uno con listar, crear, editar y eliminar |
| Validaciones | DataAnnotations en `ViewModels/` y reglas de negocio en `Services/` (ver abajo) |

## Mapa de reportes (mapa de calor)
`MapaService` entrega un punto aproximado por incidente activo (no por cada aviso del mismo corte); nunca publica la dirección residencial ni las coordenadas exactas. La intensidad usa referencias fijas provisionales de población afectada y duración, con penalización del aviso sin verificar; se dibuja con Leaflet.heat.

## Validaciones
- Formularios: campos obligatorios, longitudes, correo válido, contraseña de al menos 8 caracteres; el número estimado de personas afectadas es opcional (si se especifica, mayor que cero). El tipo de problema puede ser corte total, baja presión, calidad del agua u otro.
- Reportes: la fecha de inicio no puede estar en el futuro y la ubicación debe pertenecer a un distrito del KML habilitado en el catálogo. Un ciudadano solo edita o elimina sus reportes mientras estén en estado Recibido.
- Distritos: nombre único y registrado en el KML. No se elimina ni renombra un distrito con reportes vinculados.
- Usuarios: correo único. No se elimina un usuario con reportes, ni la propia cuenta, ni al único administrador, y no se le puede quitar el rol.
- Seguridad: token antifalsificación en todos los POST, autorización por rol y validación de la URL de retorno.

## Casos de uso implementados
| Caso de uso | Dónde |
| --- | --- |
| CUS-01 Registrar reporte | `ReportesController.Crear` |
| CUS-02 Consultar zonas afectadas | `HomeController.Index` |
| CUS-03 Consultar reportes | `ReportesController.Index` y `ZonasController.Detalle` |
| CUS-04 Verificar reporte | `ReportesController.CambiarEstado` (acción explícita del administrador) |
| CUS-05 Información consolidada | `PrioridadService.CalcularAsync` (consolida por zona) |
| CUS-06 Priorizar zonas | `AdminController.Prioridad` |
| CUS-07 Consultar estadísticas | `AdminController.Estadisticas` |
| CUS-08 Iniciar sesión | `CuentaController.Login` |
| CUS-09 Registrarse | `CuentaController.Registro` |

## Pendientes antes de la entrega
- Las referencias absolutas de población (500) y horas (24), los pesos (50 % / 50 %) y los umbrales (alta desde 70, media desde 40) son **provisionales**. Deben acordarse y calibrarse con datos reales (`PrioridadService`). La prioridad solo incluye cortes totales abiertos y verificados.
- Las fechas usan la hora del servidor (`DateTime.Now`).
- Para producción: HTTPS con certificado real, revisión de la Ley N.° 29733 y pruebas del apartado 4.5.

## Reglas de negocio revisadas (noviembre de 2026)

- Tres vecinos que reportan la misma afectación **no la verifican automáticamente**: solo la corroboran. La verificación oficial corresponde al administrador.
- El sistema detecta duplicados del mismo ciudadano. Los reportes de otros ciudadanos no se consideran duplicados.
- La agrupación requiere tipo de problema, distrito y periodos compatibles; todos los reportes de un grupo deben estar próximos entre sí (sin enlaces transitivos).
- Cada ciudadano marca como restablecido únicamente su propio reporte. No se cierra un incidente por mayoría. El administrador puede confirmar el restablecimiento total de uno ya verificado.
- La atención se registra explícitamente, y el administrador puede reabrir reportes cerrados por error. Los incidentes pueden indicar **restablecimiento parcial**.
- La prioridad se basa en referencias fijas y distingue claramente un corte total de una incidencia de presión o calidad.
- Las estadísticas distinguen incidentes activos y resueltos; la duración media considera reportes verificados y cerrados.
- El esquema `ReporteZona` se conserva por requisitos académicos, aunque el formulario asocia cada aviso individual a **un distrito**. El incidente (agrupación de avisos) representa el problema compartido.

### Actualizar una instalación existente

La nueva migración `20261116160000_TipoYAtencionReporte` agrega las columnas `Tipo` y `Atencion` a `Reportes`. Los reportes históricos se interpretan inicialmente como `CorteTotal` y `Pendiente` y pueden reclasificarse manualmente. Haz respaldo de SQL Server antes de aplicarla. En el arranque, `Database.Migrate()` aplica las migraciones pendientes.

### Comprobación manual sugerida

1. Registrar un corte sin estimación de personas: se acepta y queda **Recibido**.
2. Registrar tres cortes cercanos desde ciudadanos distintos: permanecen pendientes hasta que un administrador los verifique.
3. Registrar dos avisos del mismo ciudadano y mismo problema: el segundo se marca duplicado.
4. Verificar un incidente y marcarlo en atención: aparece el estado de seguimiento.
5. Cerrar un aviso de varios: el incidente indica **restablecimiento parcial**, los otros permanecen abiertos.
6. Confirmar el restablecimiento total como administrador y reabrir después: las fechas de cierre se actualizan correctamente.
7. Registrar baja presión o calidad del agua: aparecen en la revisión y mapa, pero no se cuentan como corte total en la prioridad.
8. Comprobar que un visitante solo ve ubicaciones aproximadas y etiquetas distritales en el mapa público.
9. Intentar renombrar un distrito con reportes o crear uno que no existe en KML: se rechaza.
10. Comprobar el ranking con un único corte pequeño: ya no recibe 100 puntos solo por ser el único.

El flujo CI de GitHub Actions compila la aplicación cuando se abre un pull request. Es importante realizar además estas pruebas en una base de datos de desarrollo; compilar no equivale a validar todas las reglas en ejecución.
