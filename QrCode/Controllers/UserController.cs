using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using QLXDK.Models;
using PagedList;
using BCrypt.Net;
using System.Web.Security;
namespace QLXDK.Controllers
{
    [Authorize(Users = "admin")]
    public class UserController : Controller
    {
        private qlslContext _db = new qlslContext();
        // GET: User
        public ActionResult Index(int? page)
        {
            int pageSize = 10;

            int pageNumber = (page ?? 1);
            ViewBag.CurrentPage = pageNumber;
            var data = _db.Users
                 .OrderByDescending(u => u.ID)
                 .ToPagedList(pageNumber, pageSize);
            return View(data);
        }

        // GET: User/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }
        [AllowAnonymous]
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            // "Sign out"
            Session.Clear();
            Session.Abandon();

            if (Request.Cookies[FormsAuthentication.FormsCookieName] != null)
            {
                var cookie = new HttpCookie(FormsAuthentication.FormsCookieName);
                cookie.Expires = DateTime.Now.AddDays(-1); // Ép cookie hết hạn ngay lập tức
                Response.Cookies.Add(cookie);
            }

            return RedirectToAction("Login", "User");
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(QLXDK.Models.Views.LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = _db.Users.FirstOrDefault(u => u.UserName == model.UserName);
            if (user == null)
            {
                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng");
                return View(model);
            }

            bool ok = BCrypt.Net.BCrypt.Verify(model.Password, user.Password);
            if (!ok)
            {
                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng");
                return View(model);
            }
            FormsAuthentication.SetAuthCookie(user.UserName, false);
            Session["UserId"] = user.ID;
            Session["Username"] = user.UserName;
            Session["FullName"] = user.FullName;
            Session["Type"] = user.Type;
            //return Redirect(returnUrl);
            return RedirectToAction("Index", "Home");
        }

        // GET: User/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: User/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Models.Views.UserVM model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                        var newUser = new Models.Entities.User
                        {
                            FullName = model.FullName,
                            UserName = model.UserName,
                            Password = BCrypt.Net.BCrypt.HashPassword(model.Password),
                            Type = 1,
                            Active = 1,
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        };

                        _db.Users.Add(newUser);
                        _db.SaveChanges();

                        TempData["Message"] = "Thêm người dùng thành công!";
                        return RedirectToAction("Create");
                    //}
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra: " + ex.Message);
                }
            }

            return View(model);
        }

        // GET: User/Edit/5
        public ActionResult Edit(int id)
        {
           
            var user = _db.Users.Find(id);

            var model = new Models.Views.UserVM
            {
                ID = user.ID,
                FullName = user.FullName,
                UserName = user.UserName,
                Type = user.Type,
                Active = user.Active
            };

            return View(model);

        }

        public ActionResult EditPassword(int id)
        {

            var user = _db.Users.Find(id);

            var model = new Models.Views.UserVM
            {
                ID = user.ID,
                FullName = user.FullName,
                UserName = user.UserName,
                Type = user.Type,
                Active = user.Active
            };

            return View(model);

        }

        // POST: User/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int ID, Models.Views.EditViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return View(model);

                var user = _db.Users.SingleOrDefault(p => p.ID == ID);
                if (user == null) return HttpNotFound();
                user.FullName = model.FullName;
                user.Active = model.Active;
                user.Type = model.Type;

                _db.SaveChanges();
                TempData["Message"] = "Cập nhật thành công!";
                return RedirectToAction("Edit", new { ID = user.ID });
            }
            catch
            {
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditPassword(int ID, Models.Views.UserVM model)
        {
            try
            {
                /*
                if (!ModelState.IsValid)
                    return View(model);
                   */
                var user = _db.Users.SingleOrDefault(p => p.ID == ID);
                if (user == null) return HttpNotFound();
                user.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);
                _db.SaveChanges();
                TempData["Message"] = "Cập nhật thành công!";
                return RedirectToAction("EditPassword", new { ID = user.ID });
            }
            catch
            {
                return View();
            }
        }

        // POST: User/Delete/5
        //[HttpPost]
        public ActionResult Delete(int id)
        {
            try
            {
                // TODO: Add delete logic here
                var item = _db.Users.Find(id);
                _db.Users.Remove(item);
                _db.SaveChanges();
                TempData["Message"] = "Xoá thành công!";
                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }
    }
}
