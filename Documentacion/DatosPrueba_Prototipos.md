# Datos de prueba

Los datos de prueba están en la **sección 8 del script `BASE DE DATOS`**. Se cargan al correr el script completo. La parte de prototipos se puede volver a correr sola: primero borra los prototipos cuyo nombre empieza con `[PRUEBA]`.

## Usuarios

| Nombre | Correo | Contraseña | Rol | Estado |
|---|---|---|---|---|
| Laura Quimico Prueba | quimico@bioquimia.com | Quimico123 | Químico (1) | Activo |
| Carlos Gerente Prueba | gerente@bioquimia.com | Gerente123 | Gerente (2) | Activo |
| María Pérez Química | maria.quimico@bioquimia.com | Quimico123 | Químico (1) | Activo |
| Roberto Gómez Gerente | roberto.gerente@bioquimia.com | Gerente123 | Gerente (2) | Activo |
| Usuario Quimico Inactivo | quimico2@bioquimia.com | Quimico123 | Químico (1) | Inactivo |
| Usuario Gerente Inactivo | gerente2@bioquimia.com | Gerente123 | Gerente (2) | Inactivo |

## Catálogos

- **Empresas (RF-14):** Hotel Costa Verde, Supermercados El Ahorro, Clinica San Jose.
- **Ingredientes (RF-23):** Agua desionizada, Lauril eter sulfato de sodio, Cocamidopropil betaina, Cloruro de sodio, Fenoxietanol, Fragancia de lavanda, Acido citrico, Hipoclorito de sodio y Glicerina.
- **Tipos de prueba:** pH, Viscosidad, Densidad y Espuma.

## Prototipos

| # | Prototipo | Químico | Estado | Sale en "En trámite" (RF-10) de Laura |
|---|---|---|---|---|
| 1 | [PRUEBA] Lavaplatos limón | Laura | Borrador | Sí |
| 2 | [PRUEBA] Desengrasante industrial | Laura | Enviado | Sí |
| 3 | [PRUEBA] Jabón antibacterial | Laura | Devuelto (con comentario del gerente) | Sí, de primero |
| 4 | [PRUEBA] Detergente en polvo | Laura | Aprobado (con 2 empresas) | No |
| 5 | [PRUEBA] Abrillantador de pisos | María | Cancelado | No |
| 6 | [PRUEBA] Cloro desinfectante | María | Enviado | No (es de otra química) |

Los 6 prototipos aparecen en el **Histórico (RF-16)**. Cada uno tiene su fórmula, que suma 100 %, y su historial de estados.

## Cómo probar

1. **RF-10:** entrar como `quimico@bioquimia.com`, abrir **En trámite** y ver los prototipos 1, 2 y 3. El devuelto sale de primero, con el comentario del gerente y el botón **Editar**.
2. **RF-16:** entrar con cualquier perfil, abrir **Histórico** y filtrar por estado, categoría, químico o fechas. En el detalle del prototipo 4 se ven sus empresas.
3. **RF-23:** como químico, intentar desactivar *Lauril eter sulfato de sodio*. El sistema no lo permite porque se usa en prototipos en trámite. Como gerente, el catálogo se ve sin botones.
4. **RF-14:** solo como gerente. Si un químico entra a `/Empresa`, lo manda a la página 404.
