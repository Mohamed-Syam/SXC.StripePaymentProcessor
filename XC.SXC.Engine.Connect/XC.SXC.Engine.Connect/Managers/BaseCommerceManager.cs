using Sitecore.Commerce.Services;
using Sitecore.Configuration;
using XC.SXC.Engine.Connect.EngineConnectProxy;
using XC.SXC.Engine.Connect.Helpers;

namespace XC.SXC.Engine.Connect.Managers
{
    public class BaseCommerceManager
    {
        public static XcEngineConnectProxy _proxy;

        public static XcEngineConnectProxy Proxy
        {
            get
            {
                _proxy = null;
                _proxy = new XcEngineConnectProxy(CommerceHelper.GetDefaultShopName(), "", "");
                return _proxy;
            }
        }

        protected virtual TServiceProvider GetConnectServiceProvider<TServiceProvider>(string serviceProviderName) where TServiceProvider : ServiceProvider
        {
            return (TServiceProvider)Factory.CreateObject(serviceProviderName, true);
        }
    }
}