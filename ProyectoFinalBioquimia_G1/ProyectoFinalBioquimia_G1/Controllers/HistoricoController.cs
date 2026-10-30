using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using ProyectoFinalBioquimia_G1.EF;
using ProyectoFinalBioquimia_G1.Models;

namespace ProyectoFinalBioquimia_G1.Controllers
{
    // RF-16: Histórico de prototipos con búsqueda y filtros.
    // Químico y gerente consultan todos los prototipos y el detalle de cada uno.
    // TODO: roles. Cuando el login esté listo, validar que haya sesión (cualquier perfil).
    public class HistoricoController : Controller
    {
        private const string MensajeErrorGenerico =
            "Ocurrió un error inesperado. Intente de nuevo o contacte al administrador.";

        [HttpGet]
        public ActionResult Index(HistoricoFiltroModel filtro)
        {
            filtro = filtro ?? new HistoricoFiltroModel();

            try
            {
                using (var db = new BioquimiaDBEntities())
                {
                    CargarListas(db, filtro);

                    if (filtro.Desde.HasValue && filtro.Hasta.HasValue && filtro.Desde > filtro.Hasta)
                    {
                        ViewBag.Advertencia = "La fecha \"Desde\" no puede ser mayor que la fecha \"Hasta\".";
                        return View(new List<PrototipoListaModel>());
                    }

                    var consulta = db.TB_Prototipo.AsQueryable();

                    if (!string.IsNullOrWhiteSpace(filtro.Buscar))
                    {
                        string texto = filtro.Buscar.Trim();
                        consulta = consulta.Where(p => p.NombrePrototipo.Contains(texto)
                                                    || p.Descripcion.Contains(texto));
                    }
                    if (filtro.IdEstado.HasValue)
                    {
                        int idEstado = filtro.IdEstado.Value;
                        consulta = consulta.Where(p => p.IdEstadoPrototipo == idEstado);
                    }
                    if (filtro.IdCategoria.HasValue)
                    {
                        int idCategoria = filtro.IdCategoria.Value;
                        consulta = consulta.Where(p => p.IdCategoria == idCategoria);
                    }
                    if (filtro.IdQuimico.HasValue)
                    {
                        int idQuimico = filtro.IdQuimico.Value;
                        consulta = consulta.Where(p => p.IdQuimico == idQuimico);
                    }
                    if (filtro.Desde.HasValue)
                    {
                        DateTime desde = filtro.Desde.Value.Date;
                        consulta = consulta.Where(p => p.FechaCreacion >= desde);
                    }
                    if (filtro.Hasta.HasValue)
                    {
                        // Incluye todo el día "Hasta".
                        DateTime hastaExclusivo = filtro.Hasta.Value.Date.AddDays(1);
                        consulta = consulta.Where(p => p.FechaCreacion < hastaExclusivo);
                    }

                    var lista = consulta
                        .OrderByDescending(p => p.FechaCreacion)
                        .Select(p => new PrototipoListaModel
                        {
                            IdPrototipo = p.IdPrototipo,
                            NombrePrototipo = p.NombrePrototipo,
                            Categoria = p.TB_Categoria.NombreCategoria,
                            Quimico = p.TB_Usuario.NombreCompleto,
                            Estado = p.TB_EstadoPrototipo.NombreEstado,
                            ComentarioGerente = p.ComentarioGerente,
                            FechaCreacion = p.FechaCreacion,
                            FechaModificacion = p.FechaModificacion
                        })
                        .ToList();

                    return View(lista);
                }
            }
            catch (Exception)
            {
                // TODO: bitácora
                TempData["Error"] = MensajeErrorGenerico;
                ViewBag.Filtro = filtro;
                ViewBag.Estados = new SelectList(new List<SelectListItem>());
                ViewBag.Categorias = new SelectList(new List<SelectListItem>());
                ViewBag.Quimicos = new SelectList(new List<SelectListItem>());
                return View(new List<PrototipoListaModel>());
            }
        }

        [HttpGet]
        public ActionResult Detalle(int id)
        {
            try
            {
                using (var db = new BioquimiaDBEntities())
                {
                    var p = db.TB_Prototipo.Find(id);
                    if (p == null)
                    {
                        TempData["Error"] = "El prototipo no existe.";
                        return RedirectToAction("Index");
                    }

                    var detalle = new PrototipoDetalleModel
                    {
                        IdPrototipo = p.IdPrototipo,
                        NombrePrototipo = p.NombrePrototipo,
                        Categoria = p.TB_Categoria.NombreCategoria,
                        Quimico = p.TB_Usuario.NombreCompleto,
                        Estado = p.TB_EstadoPrototipo.NombreEstado,
                        ComentarioGerente = p.ComentarioGerente,
                        FechaCreacion = p.FechaCreacion,
                        FechaModificacion = p.FechaModificacion,
                        Descripcion = p.Descripcion,
                        CaracteristicasEnvase = p.CaracteristicasEnvase,
                        ImagenRuta = p.ImagenRuta
                    };

                    detalle.Formula = db.TB_PrototipoIngrediente
                        .Where(f => f.IdPrototipo == id)
                        .OrderByDescending(f => f.Porcentaje)
                        .Select(f => new IngredienteFormulaModel
                        {
                            NombreIngrediente = f.TB_Ingrediente.NombreIngrediente,
                            Funcion = f.TB_Ingrediente.Funcion,
                            UnidadBase = f.TB_Ingrediente.UnidadBase,
                            Porcentaje = f.Porcentaje
                        })
                        .ToList();

                    detalle.Empresas = p.TB_Empresa
                        .OrderBy(e => e.NombreEmpresa)
                        .Select(e => e.NombreEmpresa)
                        .ToList();

                    // Se une por Id para no depender de los nombres que EF le pone a las dos
                    // relaciones con TB_EstadoPrototipo (estado anterior y estado nuevo).
                    detalle.Historial = (from h in db.TB_HistorialEstado
                                         where h.IdPrototipo == id
                                         join nuevo in db.TB_EstadoPrototipo on h.IdEstadoNuevo equals nuevo.IdEstadoPrototipo
                                         join anterior in db.TB_EstadoPrototipo on h.IdEstadoAnterior equals (int?)anterior.IdEstadoPrototipo into anteriores
                                         from anterior in anteriores.DefaultIfEmpty()
                                         orderby h.FechaCambio
                                         select new CambioEstadoModel
                                         {
                                             FechaCambio = h.FechaCambio,
                                             EstadoAnterior = anterior == null ? null : anterior.NombreEstado,
                                             EstadoNuevo = nuevo.NombreEstado,
                                             Usuario = h.TB_Usuario.NombreCompleto,
                                             Justificacion = h.Justificacion
                                         })
                                        .ToList();

                    return View(detalle);
                }
            }
            catch (Exception)
            {
                // TODO: bitácora
                TempData["Error"] = MensajeErrorGenerico;
                return RedirectToAction("Index");
            }
        }

        private void CargarListas(BioquimiaDBEntities db, HistoricoFiltroModel filtro)
        {
            ViewBag.Filtro = filtro;

            ViewBag.Estados = new SelectList(
                db.TB_EstadoPrototipo.OrderBy(e => e.IdEstadoPrototipo).ToList(),
                "IdEstadoPrototipo", "NombreEstado", filtro.IdEstado);

            ViewBag.Categorias = new SelectList(
                db.TB_Categoria.OrderBy(c => c.NombreCategoria).ToList(),
                "IdCategoria", "NombreCategoria", filtro.IdCategoria);

            ViewBag.Quimicos = new SelectList(
                db.TB_Usuario.Where(u => u.TB_Rol.NombreRol == "Quimico")
                             .OrderBy(u => u.NombreCompleto).ToList(),
                "IdUsuario", "NombreCompleto", filtro.IdQuimico);
        }
    }
}
