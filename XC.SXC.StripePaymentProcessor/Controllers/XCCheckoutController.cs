using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Web.Mvc;
using System.Web.SessionState;
using System.Web.UI;
using Newtonsoft.Json;
using Sitecore.Commerce.XA.Feature.Cart.Models.InputModels;
using Sitecore.Commerce.XA.Feature.Cart.Repositories;
using Sitecore.Commerce.XA.Foundation.Common.Attributes;
using Sitecore.Commerce.XA.Foundation.Common.Context;
using Sitecore.Commerce.XA.Foundation.Common.Controllers;
using Sitecore.Commerce.XA.Foundation.Common.Models.JsonResults;
using Sitecore.Commerce.XA.Foundation.Connect;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Sitecore.XA.Foundation.Mvc;
using Stripe;
using XC.SXC.StripePaymentProcessor.Models;
using XC.SXC.StripePaymentProcessor.Repositories.Cart;
using XC.SXC.StripePaymentProcessor.Repositories.Checkout;

namespace XC.SXC.StripePaymentProcessor.Controllers
{
    public class XcCheckoutController : BaseCommerceStandardController
    {
        #region Declarations

        public IStartCheckoutRepository StartCheckoutRepository { get; protected set; }
        public IStepIndicatorRepository StepIndicatorRepository { get; protected set; }
        public IDeliveryRepository DeliveryRepository { get; protected set; }
        public IBillingRepository BillingRepository { get; protected set; }
        public IReviewRepository ReviewRepository { get; protected set; }
        public IOrderConfirmationRepository OrderConfirmationRepository { get; protected set; }
        public IVisitorContext VisitorContext { get; protected set; }
        public IXcCheckoutRepository XcCheckoutRepository { get; protected set; }
        public IXcCartRepository XcCartRepository { get; protected set; }
        public string StripeApiKey { get; set; }
        public CustomerService StripeCustomerService { get; set; }
        public ChargeService StripeAuthorizeChargeService { get; set; }
        public TokenService StripeTokenService { get; set; }
        public BankAccountService StripeBankAccountService { get; set; }

        #endregion

        #region Constructors

        public XcCheckoutController(
            IStorefrontContext storefrontContext,
            IStartCheckoutRepository startCheckoutRepository,
            IStepIndicatorRepository stepIndicatorRepository,
            IDeliveryRepository deliveryRepository,
            IBillingRepository billingRepository,
            IReviewRepository reviewRepository,
            IOrderConfirmationRepository orderConfirmationRepository,
            IVisitorContext visitorContext,
            IXcCheckoutRepository xcCheckoutRepository,
            IXcCartRepository xcCartRepository,
            IContext sitecoreContext) : base(storefrontContext, sitecoreContext)
        {
            Assert.ArgumentNotNull((object)startCheckoutRepository, nameof(startCheckoutRepository));
            Assert.ArgumentNotNull((object)stepIndicatorRepository, nameof(stepIndicatorRepository));
            Assert.ArgumentNotNull((object)deliveryRepository, nameof(deliveryRepository));
            Assert.ArgumentNotNull((object)billingRepository, nameof(billingRepository));
            Assert.ArgumentNotNull((object)reviewRepository, nameof(reviewRepository));
            Assert.ArgumentNotNull((object)orderConfirmationRepository, nameof(orderConfirmationRepository));

            StartCheckoutRepository = startCheckoutRepository;
            StepIndicatorRepository = stepIndicatorRepository;
            DeliveryRepository = deliveryRepository;
            BillingRepository = billingRepository;
            ReviewRepository = reviewRepository;
            OrderConfirmationRepository = orderConfirmationRepository;
            VisitorContext = visitorContext;
            XcCheckoutRepository = xcCheckoutRepository;
            XcCartRepository = xcCartRepository;
            StripeApiKey = Settings.GetSetting("stripeApiKey", "sk_test_nOpeQ7HuJ2uOkYVoto3TW5R800npGIrfCK");
            StripeCustomerService = new CustomerService();
            StripeAuthorizeChargeService = new ChargeService();
            StripeTokenService = new TokenService();
            StripeBankAccountService = new BankAccountService();
        }

        #endregion

        #region Billing

        [AllowAnonymous]
        [StorefrontSessionState(SessionStateBehavior.ReadOnly)]
        public ActionResult XcBilling()
        {
            return (ActionResult) View(GetRenderingView(nameof(XcBilling)), BillingRepository.GetBillingRenderingModel(this.Rendering));
        }

        [HttpPost]
        public ActionResult SetAchPaymentMethod()
        {
            var defaultCart = XcCartRepository.GetCurrentCart(VisitorContext, StorefrontContext);

            if (defaultCart != null && defaultCart.ServiceProviderResult.Success)
            {
                var paymentInputModel = new PaymentInputModel
                {
                    BillingItemPath = $"/sitecore/content/Sitecore/Storefront/Home/checkout/billing",
                    UserEmail = Sitecore.Context.User.IsAuthenticated ? Sitecore.Context.User.Profile?.Email : string.Empty,
                    FederatedPayment = new FederatedPaymentInputModel
                    {
                        Amount = defaultCart.Result.Total.Amount,
                        CardPaymentAcceptCardPrefix = "",
                        CardToken = $"ACH|{Guid.NewGuid().ToString()}",
                        PaymentMethodID = "0CFFAB11-2674-4A18-AB04-228B1F8A1DEC",
                    },
                    BillingAddress = new PartyInputModel
                    {
                        Address1 = "123 Main Street",
                        PartyId = "Billing",
                        ExternalId = "Billing",
                        Name = "Srikanth Kondapally",
                        City = "Atlanta",
                        Country = "US",
                        State = "GA",
                        ZipPostalCode = "30346"
                    },
                    CreditCardPayment = new CreditCardPaymentInputModel
                    {
                        PaymentMethodID = "0CFFAB11-2674-4A18-AB04-228B1F8A1DEC",
                        ValidationCode = $"ACH|{Guid.NewGuid().ToString()}",
                        PartyID = "Billing",
                        Amount = defaultCart.Result.Total.Amount,
                        CustomerNameOnPayment = "Srikanth Kondapally"
                    }
                };

                var setAchPaymentMethodsResult = XcCheckoutRepository.SetPaymentMethods(VisitorContext, paymentInputModel);

                if (setAchPaymentMethodsResult.Success)
                {
                    var stripePaymentResponseModel = new StripePaymentResponseModel
                    {
                         Email = defaultCart.Result.Email,
                         Object = "ACH"
                    };

                    var storePaymentDetailsResult = XcCheckoutRepository.StorePaymentDetails(stripePaymentResponseModel, defaultCart.Result.ExternalId, true);

                    if (storePaymentDetailsResult)
                    {
                        return Json(new
                        {
                            Success = true,
                            ErrorMessage = "",
                            CardDetails = Json(new
                            {
                                CardType = "",
                                LastFourDigits = "",
                                ExpiryDate = ""
                            })
                        });
                    }
                }
            }

            return Json(new
            {
                Success = false,
                ErrorMessage = "There was an issue validating ACH payment with Stripe",
                CardDetails = Json(new
                {
                    CardType = "",
                    LastFourDigits = "",
                    ExpiryDate = ""
                })
            });
        }

        [HttpPost]
        public ActionResult XcBilling(StripePaymentResponseModel stripePaymentResponseModel)
        {
            if (stripePaymentResponseModel != null && !string.IsNullOrEmpty(stripePaymentResponseModel.Id))
            {
                var defaultCart = XcCartRepository.GetCurrentCart(VisitorContext, StorefrontContext);

                if (defaultCart?.ServiceProviderResult != null && defaultCart.ServiceProviderResult.Success)
                {
                    var paymentInputModel = new PaymentInputModel
                    {
                        BillingItemPath = $"/sitecore/content/Sitecore/Storefront/Home/checkout/billing",
                        UserEmail = Sitecore.Context.User.IsAuthenticated ? Sitecore.Context.User.Profile?.Email : stripePaymentResponseModel.Email ?? "srikanth.kondapally@xcentium.com",
                        FederatedPayment = new FederatedPaymentInputModel
                        {
                            Amount = defaultCart.Result.Total.Amount,
                            CardPaymentAcceptCardPrefix = stripePaymentResponseModel.Card.Brand,
                            CardToken = $"CC|{stripePaymentResponseModel.Id}",
                            PaymentMethodID = "0CFFAB11-2674-4A18-AB04-228B1F8A1DEC",
                        },
                        BillingAddress = new PartyInputModel
                        {
                            Address1 = stripePaymentResponseModel.Card.AddressLine1,
                            PartyId = "Billing",
                            ExternalId = "Billing",
                            Name = stripePaymentResponseModel.Card.Name,
                            City = stripePaymentResponseModel.Card.AddressCity,
                            Country = stripePaymentResponseModel.Card.Country,
                            State = stripePaymentResponseModel.Card.AddressState,
                            ZipPostalCode = stripePaymentResponseModel.Card.AddressZip
                        },
                        CreditCardPayment = new CreditCardPaymentInputModel
                        {
                            PaymentMethodID = "0CFFAB11-2674-4A18-AB04-228B1F8A1DEC",
                            ValidationCode = $"CC|{stripePaymentResponseModel.Id}",
                            PartyID = "Billing",
                            Amount = defaultCart.Result.Total.Amount,
                            CustomerNameOnPayment = stripePaymentResponseModel.Card.Name
                        }
                    };

                    var setPaymentMethodsResult = XcCheckoutRepository.SetPaymentMethods(VisitorContext, paymentInputModel);

                    if (setPaymentMethodsResult.Success)
                    {
                        var storePaymentDetailsResult = XcCheckoutRepository.StorePaymentDetails(stripePaymentResponseModel, defaultCart.Result.ExternalId);

                        if (storePaymentDetailsResult)
                        {
                            return Json(new
                            {
                                Success = true,
                                ErrorMessage = "",
                                CardDetails = Json(new
                                {
                                    CardType = stripePaymentResponseModel.Card.Brand,
                                    LastFourDigits = stripePaymentResponseModel.Card.Last4,
                                    ExpiryDate = $"{stripePaymentResponseModel.Card.ExpMonth}/{stripePaymentResponseModel.Card.ExpYear}"
                                })
                            });
                        }
                    }
                }
            }

            return Json(new
            {
                Success = false,
                ErrorMessage = "There was an issue validating credit card payment with Stripe",
                CardDetails = Json(new
                {
                    CardType = "",
                    LastFourDigits = "",
                    ExpiryDate = ""
                })
            });
        }

        #endregion

        #region Set Payment Methods Override

        [ValidateHttpPostHandler]
        [AllowAnonymous]
        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        [OutputCache(Location = OutputCacheLocation.None, NoStore = true)]
        public JsonResult SetPaymentMethods(PaymentInputModel inputModel)
        {
            BaseJsonResult result = new BaseJsonResult(this.SitecoreContext, this.StorefrontContext);
            //this.ValidateModel(result);

            //if (result.HasErrors)
            //{
            //    return this.Json((object)result);
            //}

            return this.Json((object)this.XcCheckoutRepository.SendUserToReviewPage(this.VisitorContext, inputModel));
        }

        #endregion

        #region Review

        [AllowAnonymous]
        [StorefrontSessionState(SessionStateBehavior.ReadOnly)]
        public ActionResult XcReview()
        {
            return (ActionResult)this.View(this.GetRenderingView(nameof(Review)), (object)this.ReviewRepository.GetReviewRenderingModel(this.Rendering));
        }

        [ValidateHttpPostHandler]
        [ValidateJsonAntiForgeryToken]
        [AllowAnonymous]
        [HttpPost]
        [OutputCache(Location = OutputCacheLocation.None, NoStore = true)]
        public JsonResult XcReviewData()
        {
            return this.Json((object)this.ReviewRepository.GetReviewData(this.VisitorContext), JsonRequestBehavior.AllowGet);
        }

        #endregion
    }
}