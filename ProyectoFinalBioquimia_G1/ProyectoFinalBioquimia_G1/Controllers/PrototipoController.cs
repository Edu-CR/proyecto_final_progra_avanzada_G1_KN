using ProyectoFinalBioquimia_G1.EF; // de aqui vienen los modelos y la conexion a la BD
using ProyectoFinalBioquimia_G1.Services;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ProyectoFinalBioquimia_G1.Controllers
{
    public class PrototipoController : Controller
    {
        // GET: Prototipo
        public ActionResult Index()
        {
            return View();
        }
    }
}