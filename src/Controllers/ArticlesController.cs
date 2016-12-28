using Microsoft.AspNetCore.Mvc;
using api.DataAccess;

namespace api.Controllers {
    public class ArticlesController : Controller {
        [HttpGet]
        public IActionResult List() {
            using (var db = new DbConnection()) {
                return Json(db.ListArticles());
            }
        }
    }
}