using System.Web.Mvc;
using System.Web.Routing;
using Sitecore.Pipelines;

namespace XC.SXC.StripePaymentProcessor.Pipelines.Initialize
{
    public class RegisterControllerRoutes
    {
        public virtual void Process(PipelineArgs args)
        {
            this.Register();
        }

        protected virtual void Register()
        {
            RouteTable.Routes.MapRoute("XCApiRoutes", "{virtualFolder}/api/cxa/{controller}/{action}");
            RouteTable.Routes.MapRoute("XCRootApiRoutes", "api/cxa/{controller}/{action}");
        }
    }
}