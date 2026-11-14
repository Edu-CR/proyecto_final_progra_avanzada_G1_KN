
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using ProyectoFinalBioquimia_G1.EF;
using ProyectoFinalBioquimia_G1.Services;

namespace ProyectoFinalBioquimia_G1.Controllers
{
    public class GerenteController : Controller
    {
        // Verificar que el usuario tenga rol de gerente
        private bool EsGerente()
        {
            if (Session["IdUsuarioSession"] == null ||
                Session["RolUsuarioSession"] == null)
            {
                return false;
            }

            int idUsuario;
            int idRol;

            return int.TryParse(
                       Session["IdUsuarioSession"].ToString(),
                       out idUsuario)
                   && int.TryParse(
                       Session["RolUsuarioSession"].ToString(),
                       out idRol)
                   && idUsuario > 0
                   && idRol == 2;
        }

        // RF-12: Dashboard del gerente
        [HttpGet]
        public ActionResult Index()
        {
            if (!EsGerente())
            {
                return new HttpStatusCodeResult(
                    403, "Acceso no autorizado");
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
                BitacoraService.Registrar(ex, "Gerente - Index");

                TempData["Error"] =
                    "Ocurrió un error inesperado. Intente de nuevo.";

                return RedirectToAction("Index", "Home");
            }
        }

        // RF-13: Consultar detalle del prototipo
        [HttpGet]
        public ActionResult Detalle(int id)
        {
            if (!EsGerente())
            {
                return new HttpStatusCodeResult(
                    403, "Acceso no autorizado");
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
                BitacoraService.Registrar(ex, "Gerente - Detalle");

                TempData["Error"] =
                    "Ocurrió un error inesperado. Intente de nuevo.";

                return RedirectToAction("Index");
            }
        }

        // RF-13: Evaluar prototipo
        // RF-11: Notificar al químico por correo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Evaluar(
            int id,
            string decision,
            string justificacion)
        {
            if (!EsGerente())
            {
                return new HttpStatusCodeResult(
                    403, "Acceso no autorizado");
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

            // Datos necesarios para enviar la notificación
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

                        // Validar aprobación y devolución
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

                        // Verificar que el gerente exista
                        bool gerenteExiste = db.TB_Usuario
                            .Any(u => u.IdUsuario == idGerente);

                        if (!gerenteExiste)
                        {
                            return new HttpStatusCodeResult(
                                403, "Usuario no válido");
                        }

                        // Obtener el químico responsable
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

                        // Actualizar el prototipo
                        prototipo.IdEstadoPrototipo =
                            nuevoEstado.IdEstadoPrototipo;

                        prototipo.ComentarioGerente =
                            justificacion;

                        prototipo.FechaModificacion =
                            fechaCambio;

                        // Registrar historial
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
                    ex, "Gerente - Evaluar");

                TempData["Error"] =
                    "No se pudo completar la evaluación. Intente nuevamente.";

                return RedirectToAction(
                    "Detalle", new { id });
            }

            // La evaluación ya quedó guardada en SQL Server.
            // Intentar enviar el correo sin revertir la decisión.
            if (!string.IsNullOrWhiteSpace(correoQuimico))
            {
                try
                {
                    var emailService = new EmailService();

                    string asunto =
                        "Bioquimia - Prototipo " + decision;

                    // Codificar datos para evitar insertar HTML
                    // no confiable en el mensaje.
                    string nombreSeguro =
                        HttpUtility.HtmlEncode(nombreQuimico ?? "Químico");

                    string prototipoSeguro =
                        HttpUtility.HtmlEncode(nombrePrototipo ?? "");

                    string decisionSegura =
                        HttpUtility.HtmlEncode(decision);

                    string justificacionSegura =
                        HttpUtility.HtmlEncode(justificacion);

                    string html =
                        "<h2>Actualización de prototipo</h2>" +
                        "<p>Estimado/a " + nombreSeguro + ",</p>" +
                        "<p>Le informamos que el prototipo <strong>" +
                        prototipoSeguro +
                        "</strong> ha cambiado al estado <strong>" +
                        decisionSegura +
                        "</strong>.</p>" +
                        "<p><strong>Justificación del gerente:</strong></p>" +
                        "<p>" + justificacionSegura + "</p>" +
                        "<p>Puede ingresar al sistema Bioquimia " +
                        "para consultar los detalles.</p>" +
                        "<p>Saludos,<br/>Sistema Bioquimia</p>";

                    await emailService.EnviarCorreo(
                        correoQuimico,
                        asunto,
                        html);

                    TempData["Exito"] =
                        "El prototipo fue evaluado correctamente y se envió la notificación al químico.";
                }
                catch (Exception ex)
                {
                    BitacoraService.Registrar(
                        ex, "Gerente - NotificacionCorreo");

                    TempData["Exito"] =
                        "El prototipo fue evaluado correctamente.";

                    TempData["Error"] =
                        "La evaluación se guardó, pero no se pudo enviar el correo de notificación.";
                }
            }
            else
            {
                TempData["Exito"] =
                    "El prototipo fue evaluado correctamente.";

                TempData["Error"] =
                    "No se envió la notificación porque el químico no tiene un correo registrado.";
            }

            return RedirectToAction("Index");
        }
    }
}
