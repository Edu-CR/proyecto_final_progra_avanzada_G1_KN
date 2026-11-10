
using System.Web.Mvc;



namespace ProyectoFinalBioquimia_G1.Controllers
{
    public class ErrorController : Controller
    {
        [HttpGet]
        public ActionResult Error404()
        {
            return View();
        }
    }
}
