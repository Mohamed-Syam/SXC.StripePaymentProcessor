using System;
using Sitecore.Commerce.Entities.Carts;
using Sitecore.Commerce.Services;
using Sitecore.Commerce.XA.Feature.Cart.ExtensionMethods;
using Sitecore.Commerce.XA.Feature.Cart.Models.InputModels;
using Sitecore.Commerce.XA.Feature.Cart.Models.JsonResults;
using Sitecore.Commerce.XA.Feature.Cart.Repositories;
using Sitecore.Commerce.XA.Foundation.Common.Context;
using Sitecore.Commerce.XA.Foundation.Common.Models;
using Sitecore.Commerce.XA.Foundation.Common.Providers;
using Sitecore.Commerce.XA.Foundation.Connect;
using Sitecore.Commerce.XA.Foundation.Connect.Managers;
using Sitecore.Diagnostics;

namespace XC.SXC.StripePaymentProcessor.Repositories.Checkout
{
    public class XcCheckoutRepository : BaseCheckoutRepository, IXcCheckoutRepository
    {
        #region Declarations

        public IPaymentManager PaymentManager { get; protected set; }
        public IContext Context { get; }
        public IShippingManager ShippingManager { get; protected set; }
        public IGiftCardManager GiftCardManager { get; protected set; }

        #endregion

        #region Constructors

        public XcCheckoutRepository(IModelProvider modelProvider, 
            IStorefrontContext storefrontContext, 
            ICartManager cartManager, 
            IOrderManager orderManager,
            IPaymentManager paymentManager,
            IGiftCardManager giftCardManager,
            IShippingManager shippingManager,
            IAccountManager accountManager,
            IContext context,
            ICheckoutStepProvider checkoutStepProvider) : base(modelProvider, storefrontContext, cartManager, orderManager, accountManager, checkoutStepProvider)
        {
            Assert.ArgumentNotNull((object)paymentManager, nameof(paymentManager));
            Assert.ArgumentNotNull((object)giftCardManager, nameof(giftCardManager));
            Assert.ArgumentNotNull((object)shippingManager, nameof(shippingManager));

            PaymentManager = paymentManager;
            GiftCardManager = giftCardManager;
            ShippingManager = shippingManager;
            Context = context;
        }

        #endregion

        #region Implementations

        public BillingDataJsonResult GetBillingData(IVisitorContext visitorContext)
        {
            throw new System.NotImplementedException();
        }

        public SetPaymentMethodsJsonResult SetPaymentMethods(IVisitorContext visitorContext, PaymentInputModel inputModel)
        {
            var model = ModelProvider.GetModel<SetPaymentMethodsJsonResult>();

            try
            {
                var currentCart = CartManager.GetCurrentCart(visitorContext, StorefrontContext, true);

                if (!currentCart.ServiceProviderResult.Success)
                {
                    model.SetErrors((ServiceProviderResult) currentCart.ServiceProviderResult);
                    return model;
                }

                model.Initialize(inputModel.BillingItemPath);

                var updateCartWithEmailAddress = CartManager.UpdateCart(StorefrontContext.CurrentStorefront, currentCart.Result, new CartBase()
                {
                    Email = inputModel.UserEmail
                });

                if (!updateCartWithEmailAddress.ServiceProviderResult.Success)
                {
                    model.SetErrors((ServiceProviderResult)updateCartWithEmailAddress.ServiceProviderResult);
                    return model;
                }

                var addPaymentInfo = CartManager.AddPaymentInfo(this.StorefrontContext.CurrentStorefront, updateCartWithEmailAddress.Result, inputModel.BillingAddress?.ToPartyEntity(), inputModel.CreditCardPayment?.ToCreditCardPaymentArgument(), inputModel.FederatedPayment?.ToFederatedPaymentArgument(), inputModel.GiftCardPayment?.ToGiftCardPaymentArgument());

                if (!addPaymentInfo.ServiceProviderResult.Success)
                {
                    model.SetErrors((ServiceProviderResult)addPaymentInfo.ServiceProviderResult);
                    return model;
                }

                return model;

            }
            catch (Exception ex)
            {
                model.SetErrors(nameof(SetPaymentMethods), ex);
            }

            return model;
        }

        #endregion
    }
}