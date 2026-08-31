using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using QLXDK.Models;

namespace QLXDK
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            System.Web.Http.GlobalConfiguration.Configure(WebApiConfig.Register);
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            using (var context = new qlslContext())
            {
                var user = context.Users.FirstOrDefault(u => false);
            }
        }

        protected void Application_AcquireRequestState(object sender, EventArgs e)
        {
            HttpContext context = HttpContext.Current;

            if (context.Session != null)
            {
                if (context.Request.IsAuthenticated)
                {
                    if (context.Session["UserName"] == null)
                    {
                        System.Web.Security.FormsAuthentication.SignOut();

                        string loginUrl = System.Web.Security.FormsAuthentication.LoginUrl;
                        if (!context.Request.RawUrl.Contains("User/Login"))
                        {
                            context.Response.Redirect(loginUrl);
                        }
                    }
                }
            }
        }

        protected void Application_EndRequest(object sender, EventArgs e)
        {
            if (Context.Response.StatusCode == 302 && new HttpContextWrapper(Context).Request.IsAjaxRequest())
            {
                Context.Response.Clear();
                Context.Response.StatusCode = 401;
            }
        }

        protected void Application_PostAcquireRequestState(object sender, EventArgs e)
        {
            HttpApplication app = (HttpApplication)sender;
            HttpContext context = app.Context;

            if (context.User != null && context.User.Identity.IsAuthenticated)
            {
                if (context.Session != null && context.Session["UserId"] == null)
                {
                    // Hủy luôn Cookie đăng nhập để đồng bộ hoàn toàn
                    System.Web.Security.FormsAuthentication.SignOut();
                    context.Response.Redirect("~/User/Login");
                }
            }
        }
    }
}
