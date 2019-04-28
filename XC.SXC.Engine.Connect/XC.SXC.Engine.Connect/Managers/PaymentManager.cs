using Sitecore.Commerce.Engine.Connect.Services.Carts;
using Sitecore.Commerce.Services.Carts;
using XC.Commerce.Engine.Plugin.Stripe.Pipelines.Arguments;

namespace XC.SXC.Engine.Connect.Managers
{
    public class PaymentManager : BaseCommerceManager
    {
        public CartServiceProvider CartServiceProvider { get; set; }

        public PaymentManager()
        {
            CartServiceProvider = GetCartServiceProvider();
        }

        public CartServiceProvider GetCartServiceProvider()
        {
            return GetConnectServiceProvider<CartServiceProvider>("cartServiceProvider");
        }

        public bool StorePaymentDetails(StorePaymentDetailsArgument storePaymentDetailsArgument)
        {
            return Proxy.StorePaymentDetails(storePaymentDetailsArgument);
        }

        public bool CreateStripeCustomer(CreateStripeCustomerArgument createStripeCustomerArgument)
        {
            return Proxy.CreateStripeCustomer(createStripeCustomerArgument);
        }
    }
}