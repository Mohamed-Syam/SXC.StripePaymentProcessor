using Sitecore.Commerce.Engine.Connect;

namespace XC.SXC.Engine.Connect.Helpers
{
    public class CommerceHelper
    {
        public static CommerceEngineConfiguration CeConfig;

        public static CommerceEngineConfiguration CommerceConfiguration
        {
            get
            {
                return CeConfig ?? (CeConfig =
                           (CommerceEngineConfiguration) Sitecore.Configuration.Factory.CreateObject(
                               "commerceEngineConfiguration", true));
            }
        }

        public static string GetDefaultShopName()
        {
            return CommerceConfiguration != null ? CommerceConfiguration.DefaultShopName : string.Empty;
        }
    }
}