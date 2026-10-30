/* =====================================================================
   DATOS DE PRUEBA: PROTOTIPOS (para probar RF-10 "En trámite" y RF-16 "Histórico")
   Correr DESPUÉS del script BASE DE DATOS (secciones 1 a 8).
   Se puede correr varias veces: borra solo los prototipos de prueba (los que empiezan con [PRUEBA]).
   ===================================================================== */
USE [BioquimiaDB];
GO

-- Limpieza de una corrida anterior
DELETE h  FROM [dbo].[TB_HistorialEstado]      h  JOIN [dbo].[TB_Prototipo] p ON p.IdPrototipo = h.IdPrototipo  WHERE p.NombrePrototipo LIKE N'[[]PRUEBA]%';
DELETE pi FROM [dbo].[TB_PrototipoIngrediente] pi JOIN [dbo].[TB_Prototipo] p ON p.IdPrototipo = pi.IdPrototipo WHERE p.NombrePrototipo LIKE N'[[]PRUEBA]%';
DELETE pe FROM [dbo].[TB_PrototipoEmpresa]     pe JOIN [dbo].[TB_Prototipo] p ON p.IdPrototipo = pe.IdPrototipo WHERE p.NombrePrototipo LIKE N'[[]PRUEBA]%';
DELETE FROM [dbo].[TB_Prototipo] WHERE NombrePrototipo LIKE N'[[]PRUEBA]%';
GO

DECLARE @Laura   INT = (SELECT IdUsuario FROM TB_Usuario WHERE CorreoElectronico = N'quimico@bioquimia.com');
DECLARE @Maria   INT = (SELECT IdUsuario FROM TB_Usuario WHERE CorreoElectronico = N'maria.quimico@bioquimia.com');
DECLARE @Gerente INT = (SELECT IdUsuario FROM TB_Usuario WHERE CorreoElectronico = N'gerente@bioquimia.com');

DECLARE @Borrador  INT = (SELECT IdEstadoPrototipo FROM TB_EstadoPrototipo WHERE NombreEstado = 'Borrador');
DECLARE @Enviado   INT = (SELECT IdEstadoPrototipo FROM TB_EstadoPrototipo WHERE NombreEstado = 'Enviado');
DECLARE @Devuelto  INT = (SELECT IdEstadoPrototipo FROM TB_EstadoPrototipo WHERE NombreEstado = 'Devuelto');
DECLARE @Aprobado  INT = (SELECT IdEstadoPrototipo FROM TB_EstadoPrototipo WHERE NombreEstado = 'Aprobado');
DECLARE @Cancelado INT = (SELECT IdEstadoPrototipo FROM TB_EstadoPrototipo WHERE NombreEstado = 'Cancelado');

DECLARE @Agua INT = (SELECT IdIngrediente FROM TB_Ingrediente WHERE NombreIngrediente = N'Agua desionizada');
DECLARE @Lauril INT = (SELECT IdIngrediente FROM TB_Ingrediente WHERE NombreIngrediente = N'Lauril eter sulfato de sodio');
DECLARE @Betaina INT = (SELECT IdIngrediente FROM TB_Ingrediente WHERE NombreIngrediente = N'Cocamidopropil betaina');
DECLARE @Cloruro INT = (SELECT IdIngrediente FROM TB_Ingrediente WHERE NombreIngrediente = N'Cloruro de sodio');
DECLARE @Fenoxi INT = (SELECT IdIngrediente FROM TB_Ingrediente WHERE NombreIngrediente = N'Fenoxietanol');
DECLARE @Lavanda INT = (SELECT IdIngrediente FROM TB_Ingrediente WHERE NombreIngrediente = N'Fragancia de lavanda');
DECLARE @Citrico INT = (SELECT IdIngrediente FROM TB_Ingrediente WHERE NombreIngrediente = N'Acido citrico');
DECLARE @Hipo INT = (SELECT IdIngrediente FROM TB_Ingrediente WHERE NombreIngrediente = N'Hipoclorito de sodio');
DECLARE @Glicerina INT = (SELECT IdIngrediente FROM TB_Ingrediente WHERE NombreIngrediente = N'Glicerina');

DECLARE @CatLimp INT = (SELECT IdCategoria FROM TB_Categoria WHERE NombreCategoria = N'Limpiadores');
DECLARE @CatDesi INT = (SELECT IdCategoria FROM TB_Categoria WHERE NombreCategoria = N'Desinfectantes');
DECLARE @CatJab  INT = (SELECT IdCategoria FROM TB_Categoria WHERE NombreCategoria = N'Jabones');
DECLARE @CatDet  INT = (SELECT IdCategoria FROM TB_Categoria WHERE NombreCategoria = N'Detergentes');

DECLARE @P1 INT, @P2 INT, @P3 INT, @P4 INT, @P5 INT, @P6 INT;

-- 1) Borrador (Laura)
INSERT INTO TB_Prototipo (NombrePrototipo, IdCategoria, Descripcion, CaracteristicasEnvase, ImagenRuta, IdQuimico, IdEstadoPrototipo, FechaCreacion)
VALUES (N'[PRUEBA] Lavaplatos limón', @CatDet, N'Lavaplatos líquido concentrado con aroma cítrico.', N'Botella PET de 750 mL con tapa dosificadora.', N'~/assets/img/prototipos/lavaplatos.jpg', @Laura, @Borrador, DATEADD(DAY, -3, GETDATE()));
SET @P1 = SCOPE_IDENTITY();
INSERT INTO TB_PrototipoIngrediente VALUES (@P1, @Agua, 70.00), (@P1, @Lauril, 18.00), (@P1, @Betaina, 7.00), (@P1, @Cloruro, 3.00), (@P1, @Citrico, 1.50), (@P1, @Fenoxi, 0.50);
INSERT INTO TB_HistorialEstado (IdPrototipo, IdEstadoAnterior, IdEstadoNuevo, IdUsuario, Justificacion, FechaCambio)
VALUES (@P1, NULL, @Borrador, @Laura, NULL, DATEADD(DAY, -3, GETDATE()));

-- 2) Enviado (Laura)
INSERT INTO TB_Prototipo (NombrePrototipo, IdCategoria, Descripcion, CaracteristicasEnvase, ImagenRuta, IdQuimico, IdEstadoPrototipo, FechaCreacion, FechaModificacion)
VALUES (N'[PRUEBA] Desengrasante industrial', @CatLimp, N'Desengrasante para cocinas industriales.', N'Galón de 3.78 L con atomizador.', N'~/assets/img/prototipos/desengrasante.jpg', @Laura, @Enviado, DATEADD(DAY, -10, GETDATE()), DATEADD(DAY, -8, GETDATE()));
SET @P2 = SCOPE_IDENTITY();
INSERT INTO TB_PrototipoIngrediente VALUES (@P2, @Agua, 80.00), (@P2, @Lauril, 12.00), (@P2, @Citrico, 5.00), (@P2, @Fenoxi, 3.00);
INSERT INTO TB_HistorialEstado (IdPrototipo, IdEstadoAnterior, IdEstadoNuevo, IdUsuario, Justificacion, FechaCambio)
VALUES (@P2, NULL, @Borrador, @Laura, NULL, DATEADD(DAY, -10, GETDATE())),
       (@P2, @Borrador, @Enviado, @Laura, NULL, DATEADD(DAY, -8, GETDATE()));

-- 3) Devuelto con justificación (Laura)
INSERT INTO TB_Prototipo (NombrePrototipo, IdCategoria, Descripcion, CaracteristicasEnvase, ImagenRuta, IdQuimico, IdEstadoPrototipo, ComentarioGerente, FechaCreacion, FechaModificacion)
VALUES (N'[PRUEBA] Jabón antibacterial', @CatJab, N'Jabón líquido para manos con agente antibacterial.', N'Botella de 500 mL con bomba.', N'~/assets/img/prototipos/antibacterial.jpg', @Laura, @Devuelto, N'El porcentaje de fragancia es muy alto; bajarlo a 0.5 % y volver a enviar.', DATEADD(DAY, -15, GETDATE()), DATEADD(DAY, -2, GETDATE()));
SET @P3 = SCOPE_IDENTITY();
INSERT INTO TB_PrototipoIngrediente VALUES (@P3, @Agua, 75.00), (@P3, @Lauril, 12.00), (@P3, @Glicerina, 8.00), (@P3, @Lavanda, 3.00), (@P3, @Fenoxi, 2.00);
INSERT INTO TB_HistorialEstado (IdPrototipo, IdEstadoAnterior, IdEstadoNuevo, IdUsuario, Justificacion, FechaCambio)
VALUES (@P3, NULL, @Borrador, @Laura, NULL, DATEADD(DAY, -15, GETDATE())),
       (@P3, @Borrador, @Enviado, @Laura, NULL, DATEADD(DAY, -5, GETDATE())),
       (@P3, @Enviado, @Devuelto, @Gerente, N'El porcentaje de fragancia es muy alto; bajarlo a 0.5 % y volver a enviar.', DATEADD(DAY, -2, GETDATE()));

-- 4) Aprobado (Laura) -> NO aparece en "En trámite", sí en el histórico
INSERT INTO TB_Prototipo (NombrePrototipo, IdCategoria, Descripcion, CaracteristicasEnvase, ImagenRuta, IdQuimico, IdEstadoPrototipo, ComentarioGerente, FechaCreacion, FechaModificacion)
VALUES (N'[PRUEBA] Detergente en polvo', @CatDet, N'Detergente para ropa de uso doméstico.', N'Bolsa laminada de 1 kg.', N'~/assets/img/prototipos/detergente.jpg', @Laura, @Aprobado, N'Cumple con la fórmula y el etiquetado.', DATEADD(DAY, -40, GETDATE()), DATEADD(DAY, -30, GETDATE()));
SET @P4 = SCOPE_IDENTITY();
INSERT INTO TB_PrototipoIngrediente VALUES (@P4, @Lauril, 25.00), (@P4, @Cloruro, 60.00), (@P4, @Lavanda, 1.00), (@P4, @Citrico, 14.00);
INSERT INTO TB_HistorialEstado (IdPrototipo, IdEstadoAnterior, IdEstadoNuevo, IdUsuario, Justificacion, FechaCambio)
VALUES (@P4, NULL, @Borrador, @Laura, NULL, DATEADD(DAY, -40, GETDATE())),
       (@P4, @Borrador, @Enviado, @Laura, NULL, DATEADD(DAY, -35, GETDATE())),
       (@P4, @Enviado, @Aprobado, @Gerente, N'Cumple con la fórmula y el etiquetado.', DATEADD(DAY, -30, GETDATE()));

-- 5) Cancelado (María) -> de otra química, solo en el histórico
INSERT INTO TB_Prototipo (NombrePrototipo, IdCategoria, Descripcion, CaracteristicasEnvase, ImagenRuta, IdQuimico, IdEstadoPrototipo, ComentarioGerente, FechaCreacion, FechaModificacion)
VALUES (N'[PRUEBA] Abrillantador de pisos', @CatLimp, N'Abrillantador para pisos de cerámica.', N'Botella de 1 L.', N'~/assets/img/prototipos/abrillantador.jpg', @Maria, @Cancelado, N'El cliente ya no necesita este producto.', DATEADD(DAY, -25, GETDATE()), DATEADD(DAY, -20, GETDATE()));
SET @P5 = SCOPE_IDENTITY();
INSERT INTO TB_PrototipoIngrediente VALUES (@P5, @Agua, 88.00), (@P5, @Glicerina, 10.00), (@P5, @Fenoxi, 2.00);
INSERT INTO TB_HistorialEstado (IdPrototipo, IdEstadoAnterior, IdEstadoNuevo, IdUsuario, Justificacion, FechaCambio)
VALUES (@P5, NULL, @Borrador, @Maria, NULL, DATEADD(DAY, -25, GETDATE())),
       (@P5, @Borrador, @Cancelado, @Gerente, N'El cliente ya no necesita este producto.', DATEADD(DAY, -20, GETDATE()));

-- 6) Enviado (María) -> de otra química: NO aparece en "En trámite" de Laura
INSERT INTO TB_Prototipo (NombrePrototipo, IdCategoria, Descripcion, CaracteristicasEnvase, ImagenRuta, IdQuimico, IdEstadoPrototipo, FechaCreacion, FechaModificacion)
VALUES (N'[PRUEBA] Cloro desinfectante', @CatDesi, N'Desinfectante a base de hipoclorito.', N'Galón de 3.78 L.', N'~/assets/img/prototipos/desengrasante.jpg', @Maria, @Enviado, DATEADD(DAY, -6, GETDATE()), DATEADD(DAY, -4, GETDATE()));
SET @P6 = SCOPE_IDENTITY();
INSERT INTO TB_PrototipoIngrediente VALUES (@P6, @Agua, 94.00), (@P6, @Hipo, 6.00);
INSERT INTO TB_HistorialEstado (IdPrototipo, IdEstadoAnterior, IdEstadoNuevo, IdUsuario, Justificacion, FechaCambio)
VALUES (@P6, NULL, @Borrador, @Maria, NULL, DATEADD(DAY, -6, GETDATE())),
       (@P6, @Borrador, @Enviado, @Maria, NULL, DATEADD(DAY, -4, GETDATE()));

-- Relación con empresas (RF-15, para que el detalle muestre algo)
INSERT INTO TB_PrototipoEmpresa (IdPrototipo, IdEmpresa)
SELECT @P4, IdEmpresa FROM TB_Empresa WHERE NombreEmpresa IN (N'Hotel Costa Verde', N'Supermercados El Ahorro');
GO

-- Verificación: deben salir 6 prototipos
SELECT p.IdPrototipo, p.NombrePrototipo, e.NombreEstado, u.NombreCompleto AS Quimico,
       (SELECT SUM(Porcentaje) FROM TB_PrototipoIngrediente WHERE IdPrototipo = p.IdPrototipo) AS SumaFormula
FROM TB_Prototipo p
JOIN TB_EstadoPrototipo e ON e.IdEstadoPrototipo = p.IdEstadoPrototipo
JOIN TB_Usuario u ON u.IdUsuario = p.IdQuimico
WHERE p.NombrePrototipo LIKE N'[[]PRUEBA]%'
ORDER BY p.IdPrototipo;
GO
