using Sitecore.Commerce.Services;
using Sitecore.Commerce.Services.Carts;
using Sitecore.Commerce.XA.Feature.Cart.Repositories;
using Sitecore.Commerce.XA.Foundation.Common.Context;
using Sitecore.Commerce.XA.Foundation.Common.Models;
using Sitecore.Commerce.XA.Foundation.Common.Providers;
using Sitecore.Commerce.XA.Foundation.Connect;
using Sitecore.Commerce.XA.Foundation.Connect.Managers;
using Sitecore.Diagnostics;

namespace XC.SXC.StripePaymentProcessor.Repositories.Cart
{
    public class XcCartRepository : BaseCartRepository, IXcCartRepository
    {
        #region Declarations

        public IContext Context { get; }

        #endregion

        #region Constructors

        public XcCartRepository(IModelProvider modelProvider,
            ICartManager cartManager,
            IPaymentManager paymentManager,
            IGiftCardManager giftCardManager,
            IShippingManager shippingManager,
            ISiteContext siteContext,
            IContext context,
            ICheckoutStepProvider checkoutStepProvider) : base(modelProvider, cartManager, siteContext)
        {
            Assert.ArgumentNotNull((object)paymentManager, nameof(paymentManager));
            Assert.ArgumentNotNull((object)giftCardManager, nameof(giftCardManager));
            Assert.ArgumentNotNull((object)shippingManager, nameof(shippingManager));

            Context = context;
        }

        #endregion

        #region Implementations

        public ManagerResponse<CartResult, Sitecore.Commerce.Entities.Carts.Cart> GetCurrentCart(IVisitorContext visitorContext, IStorefrontContext storefrontContext, bool recalculateTotals = false)
        {
            var currentCart = CartManager.GetCurrentCart(visitorContext, storefrontContext, true);

            if (!currentCart.ServiceProviderResult.Success)
            {
                return null;
            }

            return currentCart;
        }

        #endregion
    }
}