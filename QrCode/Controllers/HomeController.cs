using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using QLXDK.Models;

namespace QLXDK.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private qlslContext _db = new qlslContext();
        public ActionResult Index()
        {
            int userId = Convert.ToInt32(Session["UserId"]);
            var orders = _db.Orders
                            .Where(p => p.UserID == userId)    
                            .OrderByDescending(p => p.CreatedDate)
                            .Take(20)
                            .ToList();
            return View(orders);
        }

    }
}