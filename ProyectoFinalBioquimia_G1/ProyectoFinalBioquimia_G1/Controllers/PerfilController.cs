using ProyectoFinalBioquimia_G1.EF;
using ProyectoFinalBioquimia_G1.Services;
using System;

using System.Linq;

using System.Web.Mvc;



namespace ProyectoFinalBioquimia_G1.Controllers
{
    public class PerfilController : Controller
    {

        private const string MensajeErrorGenerico = "Ocurrió un error inesperado. Intente de nuevo o contacte al administrador.";

        private BioquimiaDBEntities context = new BioquimiaDBEntities(); //conexion mejor asi para no andar iniciandola a cada rato.

        [HttpGet]
        public ActionResult VerPerfil()
        {
            try
            {
                if (Session["IdUsuarioSession"] == null || (bool?)Session["EstadoUsuarioSession"] != true)//solo si no hay sesion o si usuario desactivado, porque cualquier usuario puede ver su perfil sea el rol q sea
                {
                    return RedirectToAction("Error404", "Error");

                } else { 
                    
                
                }

                int idUsuarioConvertido = Convert.ToInt32(Session["IdUsuarioSession"]);

                var response = (from u in context.TB_Usuario
                                join r in context.TB_Rol on u.IdRol equals r.IdRol
                                where u.IdUsuario == idUsuarioConvertido && u.Estado == true
                                select new
                                {
                                    u.Identificacion,
                                    u.NombreCompleto,
                                    u.CorreoElectronico,
                                    r.NombreRol,
                                    u.FechaCreacion
                                }).FirstOrDefault();

                if (response != null)
                {
                    ViewBag.Identificacion = response.Identificacion;
                    ViewBag.NombreCompleto = response.NombreCompleto;
                    ViewBag.CorreoElectronico = response.CorreoElectronico;
                    ViewBag.NombreRol = response.NombreRol;
                    ViewBag.FechaCreacion = response.FechaCreacion;

                    return View();
                }
                else {
                    Session.Clear();
                    Session.Abandon();
                    return RedirectToAction("Login", "Auth"); //si se hace una consulta pero no existe el usuario
                }

       
            }
            catch (Exception ex)
            {
                BitacoraService.Registrar(ex, "Ocurrio una excepción, PerfilController - HttpGet -> VerPerfil()");
                TempData["Message"] = "Se presentó un error, consulte con el administrador.";
                // TO DO: Ver a donde se envia al usuario dependiendo del rol
            }


            return RedirectToAction("Error404", "Error");
        }















        #region Dispose
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (context != null)
                {
                    context.Dispose();
                }
            }
            base.Dispose(disposing);
        }
        #endregion



    }
}
