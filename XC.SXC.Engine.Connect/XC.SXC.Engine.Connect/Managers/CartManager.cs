using Sitecore.Commerce.Services.Carts;

namespace XC.SXC.Engine.Connect.Managers
{
    public class CartManager : BaseCommerceManager
    {
        public CartServiceProvider CartServiceProvider { get; set; }

        public CartManager()
        {
            CartServiceProvider = GetCartServiceProvider();
        }

        public CartServiceProvider GetCartServiceProvider()
        {
            return GetConnectServiceProvider<CartServiceProvider>("cartServiceProvider");
        }
    }
}