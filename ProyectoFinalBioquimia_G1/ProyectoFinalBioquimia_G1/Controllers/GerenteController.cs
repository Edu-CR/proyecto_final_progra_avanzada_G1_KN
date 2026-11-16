
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using ProyectoFinalBioquimia_G1.EF;
using ProyectoFinalBioquimia_G1.Services;
using System.IO;
using System.Net;
using System.Text;

namespace ProyectoFinalBioquimia_G1.Controllers
{
    public class GerenteController : Controller
    {
        private const string MensajeErrorGenerico ="Ocurrió un error inesperado. Intente de nuevo o contacte al administrador.";

        // Verifica que exista una sesión válida, usuario activo y gerente.
        private bool EsGerente()
        {
            if (Session["IdUsuarioSession"] == null ||
                Session["RolUsuarioSession"] == null ||
                Session["EstadoUsuarioSession"] == null)
            {
                return false;
            }

            int idUsuario;
            int idRol;
            bool estadoUsuario;

            return int.TryParse(Convert.ToString(Session["IdUsuarioSession"]), out idUsuario)
                   && int.TryParse(Convert.ToString(Session["RolUsuarioSession"]), out idRol)
                   && bool.TryParse(Convert.ToString(Session["EstadoUsuarioSession"]), out estadoUsuario)
                   && idUsuario > 0
                   && idRol == 2
                   && estadoUsuario;
        }

        // RF-12: Dashboard del gerente.
        [HttpGet]
        public ActionResult Index()
        {
            if (!EsGerente())
            {
                return RedirectToAction("Error404", "Error");
            }

            try
            {
                using (var db = new BioquimiaDBEntities())
                {
                    var prototipos = db.TB_Prototipo
                        .Include("TB_EstadoPrototipo")
                        .Include("TB_Categoria")
                        .Include("TB_Usuario")
                        .ToList();

                    ViewBag.TotalPrototipos = prototipos.Count;

                    ViewBag.Pendientes = prototipos.Count(p =>
                        p.TB_EstadoPrototipo.NombreEstado == "Enviado");

                    ViewBag.Aprobados = prototipos.Count(p =>
                        p.TB_EstadoPrototipo.NombreEstado == "Aprobado");

                    ViewBag.Devueltos = prototipos.Count(p =>
                        p.TB_EstadoPrototipo.NombreEstado == "Devuelto");

                    ViewBag.Cancelados = prototipos.Count(p =>
                        p.TB_EstadoPrototipo.NombreEstado == "Cancelado");

                    var pendientes = prototipos
                        .Where(p =>
                            p.TB_EstadoPrototipo.NombreEstado == "Enviado")
                        .OrderByDescending(p => p.FechaCreacion)
                        .ToList();

                    ViewBag.TodosPrototipos = prototipos
                        .OrderByDescending(p => p.FechaCreacion)
                        .ToList();

                    return View(pendientes);
                }
            }
            catch (Exception ex)
            {
                BitacoraService.Registrar(
                    ex,
                    "Ocurrió una excepción, GerenteController - HttpGet -> Index()");

                TempData["Error"] = MensajeErrorGenerico;

                return RedirectToAction("Index", "Home");
            }
        }

        // RF-13: Consultar detalle del prototipo.
        [HttpGet]
        public ActionResult Detalle(int id)
        {
            if (!EsGerente())
            {
                return RedirectToAction("Error404", "Error");
            }

            try
            {
                using (var db = new BioquimiaDBEntities())
                {
                    var prototipo = db.TB_Prototipo
                        .Include("TB_Categoria")
                        .Include("TB_EstadoPrototipo")
                        .Include("TB_Usuario")
                        .Include("TB_PrototipoIngrediente.TB_Ingrediente")
                        .FirstOrDefault(p => p.IdPrototipo == id);

                    if (prototipo == null)
                    {
                        return HttpNotFound();
                    }

                    return View(prototipo);
                }
            }
            catch (Exception ex)
            {
                BitacoraService.Registrar(
                    ex,
                    "Ocurrió una excepción, GerenteController - HttpGet -> Detalle()");

                TempData["Error"] = MensajeErrorGenerico;

                return RedirectToAction("Index");
            }
        }

        // RF-13: Evaluar prototipo.
        // RF-11: Notificar al químico por correo.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Evaluar(
            int id,
            string decision,
            string justificacion)
        {
            if (!EsGerente())
            {
                return RedirectToAction("Error404", "Error");
            }

            int idGerente = Convert.ToInt32(
                Session["IdUsuarioSession"]);

            justificacion = (justificacion ?? "").Trim();

            if (justificacion.Length == 0 ||
                justificacion.Length > 500)
            {
                TempData["Error"] =
                    "La justificación es obligatoria y no puede superar 500 caracteres.";

                return RedirectToAction("Detalle", new { id });
            }

            if (decision != "Aprobado" &&
                decision != "Devuelto" &&
                decision != "Cancelado")
            {
                return new HttpStatusCodeResult(
                    400, "Decisión no válida");
            }

            // Datos necesarios para enviar la notificación.
            string correoQuimico = null;
            string nombreQuimico = null;
            string nombrePrototipo = null;

            try
            {
                using (var db = new BioquimiaDBEntities())
                {
                    using (var transaccion =
                        db.Database.BeginTransaction())
                    {
                        var prototipo = db.TB_Prototipo
                            .FirstOrDefault(p =>
                                p.IdPrototipo == id);

                        if (prototipo == null)
                        {
                            return HttpNotFound();
                        }

                        var estadoAnterior = db.TB_EstadoPrototipo
                            .FirstOrDefault(e =>
                                e.IdEstadoPrototipo ==
                                prototipo.IdEstadoPrototipo);

                        if (estadoAnterior == null)
                        {
                            return new HttpStatusCodeResult(
                                400, "Estado actual inválido");
                        }

                        string estadoActual =
                            estadoAnterior.NombreEstado;

                        // Validar aprobación y devolución.
                        if (decision == "Aprobado" ||
                            decision == "Devuelto")
                        {
                            if (estadoActual != "Enviado")
                            {
                                TempData["Error"] =
                                    "Solo se pueden aprobar o devolver prototipos enviados.";

                                return RedirectToAction(
                                    "Detalle", new { id });
                            }
                        }
                        else if (decision == "Cancelado")
                        {
                            bool puedeCancelar =
                                estadoActual == "Borrador" ||
                                estadoActual == "Enviado" ||
                                estadoActual == "Devuelto" ||
                                estadoActual == "Aprobado";

                            if (!puedeCancelar)
                            {
                                TempData["Error"] =
                                    "No se puede cancelar el prototipo en su estado actual.";

                                return RedirectToAction(
                                    "Detalle", new { id });
                            }
                        }

                        var nuevoEstado = db.TB_EstadoPrototipo
                            .FirstOrDefault(e =>
                                e.NombreEstado == decision);

                        if (nuevoEstado == null)
                        {
                            return new HttpStatusCodeResult(
                                400, "Estado destino inválido");
                        }

                        // Verificar que el gerente exista.
                        bool gerenteExiste = db.TB_Usuario
                            .Any(u => u.IdUsuario == idGerente);

                        if (!gerenteExiste)
                        {
                            return new HttpStatusCodeResult(
                                403, "Usuario no válido");
                        }

                        // Obtener el químico responsable.
                        var quimico = db.TB_Usuario
                            .FirstOrDefault(u =>
                                u.IdUsuario == prototipo.IdQuimico);

                        if (quimico != null)
                        {
                            correoQuimico = quimico.CorreoElectronico;
                            nombreQuimico = quimico.NombreCompleto;
                        }

                        nombrePrototipo =
                            prototipo.NombrePrototipo;

                        int idEstadoAnterior =
                            prototipo.IdEstadoPrototipo;

                        DateTime fechaCambio = DateTime.Now;

                        // Actualizar el prototipo.
                        prototipo.IdEstadoPrototipo =
                            nuevoEstado.IdEstadoPrototipo;

                        prototipo.ComentarioGerente =
                            justificacion;

                        prototipo.FechaModificacion =
                            fechaCambio;

                        // Registrar historial.
                        var historial = new TB_HistorialEstado
                        {
                            IdPrototipo = prototipo.IdPrototipo,
                            IdEstadoAnterior = idEstadoAnterior,
                            IdEstadoNuevo = nuevoEstado.IdEstadoPrototipo,
                            IdUsuario = idGerente,
                            Justificacion = justificacion,
                            FechaCambio = fechaCambio
                        };

                        db.TB_HistorialEstado.Add(historial);

                        db.SaveChanges();
                        transaccion.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                BitacoraService.Registrar(
                    ex,
                    "Ocurrió una excepción, GerenteController - HttpPost -> Evaluar()");

                TempData["Error"] = MensajeErrorGenerico;

                return RedirectToAction(
                    "Detalle", new { id });
            }

            // La evaluación ya quedó guardada en SQL Server.
            // El envío del correo es independiente de la transacción.

            if (!string.IsNullOrWhiteSpace(correoQuimico))
            {
                try
                {
                    string colorEstado;
                    string mensajeEstado;

                    switch (decision)
                    {
                        case "Aprobado":
                            colorEstado = "#28a745";
                            mensajeEstado = "Nos complace informarle que su prototipo fue aprobado.";
                            break;
                        case "Devuelto":
                            colorEstado = "#fd7e14";
                            mensajeEstado = "Su prototipo fue devuelto para que realice las correcciones indicadas.";
                            break;
                        default: // Cancelado
                            colorEstado = "#dc3545";
                            mensajeEstado = "Lamentamos informarle que su prototipo fue cancelado.";
                            break;
                    }

                    var ruta = Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "assets", "recursos", "NotificacionPrototipo.html");

                    // La justificacion va de una codificada y luego se respetan los saltos de linea.
                    string justificacionHtml = WebUtility.HtmlEncode(justificacion)
                        .Replace("\r\n", "<br/>")
                        .Replace("\n", "<br/>");

                    var html = System.IO.File.ReadAllText(ruta, Encoding.UTF8)
                        .Replace("{{Nombre}}", WebUtility.HtmlEncode(nombreQuimico ?? "Químico"))
                        .Replace("{{Prototipo}}", WebUtility.HtmlEncode(nombrePrototipo ?? ""))
                        .Replace("{{Estado}}", WebUtility.HtmlEncode(decision))
                        .Replace("{{ColorEstado}}", colorEstado)
                        .Replace("{{Mensaje}}", mensajeEstado)
                        .Replace("{{Justificacion}}", justificacionHtml)
                        .Replace("{{Anio}}", DateTime.Now.Year.ToString());

                    var emailService = new EmailService();

                    await emailService.EnviarCorreo(
                        correoQuimico,
                        "Bioquimia - Prototipo " + decision,
                        html);

                    TempData["Exito"] =
                        "El prototipo fue evaluado correctamente y se envió la notificación al químico.";
                }
                catch (Exception ex)
                {
                    BitacoraService.Registrar(
                        ex,
                        "Ocurrió una excepción, GerenteController - HttpPost -> Evaluar() - envío de correo");

                    TempData["Exito"] = "El prototipo fue evaluado correctamente.";
                    TempData["Error"] =
                        "La evaluación se guardó, pero no se pudo enviar el correo de notificación.";
                }
            }
            else
            {
                TempData["Exito"] = "El prototipo fue evaluado correctamente.";
                TempData["Error"] =
                    "No se envió la notificación porque el químico no tiene un correo registrado.";
            }

            return RedirectToAction("Index");
        }
    }
}
