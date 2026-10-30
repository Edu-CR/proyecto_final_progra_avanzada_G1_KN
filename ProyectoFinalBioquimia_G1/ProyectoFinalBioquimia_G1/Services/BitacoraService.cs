using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Web;
using ProyectoFinalBioquimia_G1.EF;

namespace ProyectoFinalBioquimia_G1.Services
{
    public static class BitacoraService
    {

        public static void Registrar(Exception ex, string modulo)
        {
            try
            {
                // Obtenemos el ID de usuario directamente de la sesión actual de forma segura
                int? idUsuario = null;
                if (HttpContext.Current?.Session != null && HttpContext.Current.Session["IdUsuarioSession"] != null)
                {
                    if (int.TryParse(HttpContext.Current.Session["IdUsuarioSession"].ToString(), out int idUsuarioSessionParseado))
                    {
                        idUsuario = idUsuarioSessionParseado;
                    }
                }

                
                using (var db = new BioquimiaDBEntities())
                {
                    var error = new TB_BitacoraError
                    {
                        FechaError = DateTime.Now,
                        Modulo = Cortar(modulo, 100),
                        Mensaje = Cortar(ex.Message, 1000),
                        Detalle = ex.ToString(),   
                        IdUsuario = idUsuario      
                    };

                    db.TB_BitacoraError.Add(error);
                    db.SaveChanges();
                }
            }
            catch (Exception exBitacora)
            {
                // Si de verdad falla todo todo todo escribir txt
                try
                {
                    string ruta = HttpContext.Current.Server.MapPath("~/App_Data/errores.txt");
                    File.AppendAllText(ruta,
                        DateTime.Now + " | " + modulo + " | " + ex.Message + " | " + exBitacora.Message + Environment.NewLine);
                }
                catch { }
            }
        }

        private static string Cortar(string texto, int max)
        {
            if (string.IsNullOrEmpty(texto)) return "";
            return texto.Length <= max ? texto : texto.Substring(0, max);
        }
    }
}