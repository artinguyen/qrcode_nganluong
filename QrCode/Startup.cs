using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(QLSL.Startup))]
namespace QLSL
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            //ConfigureAuth(app);
            app.MapSignalR();
        }
    }
}
