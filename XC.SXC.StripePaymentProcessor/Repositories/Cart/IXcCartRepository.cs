using Sitecore.Commerce.Services.Carts;
using Sitecore.Commerce.XA.Foundation.Common.Context;
using Sitecore.Commerce.XA.Foundation.Connect;
using Sitecore.Commerce.XA.Foundation.Connect.Managers;

namespace XC.SXC.StripePaymentProcessor.Repositories.Cart
{
    public interface IXcCartRepository
    {
        ManagerResponse<CartResult, Sitecore.Commerce.Entities.Carts.Cart> GetCurrentCart(IVisitorContext visitorContext, IStorefrontContext storefrontContext, bool recalculateTotals = false);
    }
}
