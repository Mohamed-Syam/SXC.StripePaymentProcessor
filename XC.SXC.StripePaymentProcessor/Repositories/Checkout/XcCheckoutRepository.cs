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
using XC.Commerce.Engine.Plugin.Stripe.Pipelines.Arguments;
using XC.SXC.StripePaymentProcessor.Models;
using Card = XC.Commerce.Engine.Plugin.Stripe.Pipelines.Arguments.Card;
using Metadata = XC.Commerce.Engine.Plugin.Stripe.Pipelines.Arguments.Metadata;

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

        public bool StorePaymentDetails(StripePaymentResponseModel stripePaymentResponseModel, string cartId, bool userSelectedAchPayment = false)
        {
            var storePaymentDetailsArgument = new StorePaymentDetailsArgument();

            if (userSelectedAchPayment)
            {
                storePaymentDetailsArgument = new StorePaymentDetailsArgument
                {
                    CartId = cartId,
                    Email = stripePaymentResponseModel.Email,
                    CreateStripeCustomer = false,
                    IsAchAccountBeingUsed = true
                };
            }
            else
            {
                storePaymentDetailsArgument = new StorePaymentDetailsArgument
                {
                    Email = stripePaymentResponseModel.Email,
                    CreateStripeCustomer = true,
                    CardDetails = new Card()
                    {
                        Id = stripePaymentResponseModel.Card.Id,
                        Name = stripePaymentResponseModel.Card.Name,
                        ExpMonth = stripePaymentResponseModel.Card.ExpMonth,
                        ExpYear = stripePaymentResponseModel.Card.ExpYear,
                        Brand = stripePaymentResponseModel.Card.Brand,
                        Last4 = stripePaymentResponseModel.Card.Last4,
                        Country = stripePaymentResponseModel.Card.Country,
                        AddressZip = stripePaymentResponseModel.Card.AddressZip,
                        AddressLine1 = stripePaymentResponseModel.Card.AddressLine1,
                        AddressState = stripePaymentResponseModel.Card.AddressState,
                        AddressCity = stripePaymentResponseModel.Card.AddressCity,
                        AddressLine1Check = stripePaymentResponseModel.Card.AddressLine1Check,
                        AddressZipCheck = stripePaymentResponseModel.Card.AddressZipCheck,
                        CvcCheck = stripePaymentResponseModel.Card.CvcCheck,
                        Funding = stripePaymentResponseModel.Card.Funding,
                        Metadata = new Metadata(),
                        Object = stripePaymentResponseModel.Card.Object
                    },
                    Object = stripePaymentResponseModel.Object,
                    CartId = cartId,
                    ClientIp = stripePaymentResponseModel.ClientIp,
                    Created = stripePaymentResponseModel.Created,
                    LiveMode = stripePaymentResponseModel.LiveMode,
                    TokenId = stripePaymentResponseModel.Id,
                    Type = stripePaymentResponseModel.Type,
                    Used = stripePaymentResponseModel.Used
                };
            }

            return new Engine.Connect.Managers.PaymentManager().StorePaymentDetails(storePaymentDetailsArgument);
        }

        public bool CreateStripeCustomer(string email, string stripeTokenId, string cartId)
        {
            var createStripeCustomerArgument = new CreateStripeCustomerArgument
            {
                Email = "",
                StripeTokenId = ""
            };

            return new Engine.Connect.Managers.PaymentManager().CreateStripeCustomer(createStripeCustomerArgument);
        }

        #endregion
    }
}