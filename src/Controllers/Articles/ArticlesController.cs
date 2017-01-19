using Microsoft.AspNetCore.Mvc;
using api.DataAccess;

namespace api.Controllers.Articles {
    public class ArticlesController : Controller {
        [HttpGet]
        public IActionResult List() {
            using (var db = new DbConnection()) {
                return Json(db.ListArticles());
            }
        }
        [HttpGet]
        public IActionResult UserList() {
            using (var db = new DbConnection()) {
                return Json(new object[0]);
            }
        }
    }
}