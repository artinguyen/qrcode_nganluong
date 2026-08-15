using System.Web;
using System.Web.Optimization;

namespace QLXDK
{
    public class BundleConfig
    {
        // For more information on bundling, visit http://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.validate*"));

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at http://modernizr.com to pick only the tests you need.
            //bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
            //            "~/Scripts/modernizr-*"));

            bundles.Add(new ScriptBundle("~/bundles/customjs").Include(
                      "~/Content/asset/js/popper.min.js",
                      "~/Content/asset/js/simplebar.min.js",
                      "~/Content/asset/js/site.js",
                      //"~/Content/asset/js/fonts/custom-font.js",
                      "~/Content/asset/js/pcoded.js",
                      "~/Content/asset/js/plugins/feather.min.js"
                      ));

            bundles.Add(new Bundle("~/bundles/bootstrap").Include(
                "~/Content/js/bootstrap.min.js"));

            bundles.Add(new StyleBundle("~/Content/css").Include(
                      //"~/Content/bootstrap.css",
                      //"~/Content/site.css",
                      "~/Content/asset/fonts/tabler-icons.min.css",
                      "~/Content/asset/fonts/feather.css",
                      "~/Content/asset/fonts/fontawesome.css",
                      "~/Content/asset/fonts/material.css",
                      "~/Content/asset/css/style.css",
                      "~/Content/asset/css/style-preset.css",
                      "~/Content/Site.css"
                      ));
            bundles.Add(new StyleBundle("~/QrCode/css").Include(
                      "~/Content/Site.css",
                      "~/Content/fontawesome.css"
                      ));
            bundles.Add(new ScriptBundle("~/bundles/qrcode").Include(
                        "~/Scripts/home.js",
                        "~/Scripts/qrcode.min.js"
                        ));
        }
    }
}
