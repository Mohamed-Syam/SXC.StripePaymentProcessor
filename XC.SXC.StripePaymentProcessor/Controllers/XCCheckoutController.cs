using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Web.Mvc;
using System.Web.SessionState;
using Newtonsoft.Json;
using Sitecore.Commerce.XA.Feature.Cart.Models.InputModels;
using Sitecore.Commerce.XA.Feature.Cart.Repositories;
using Sitecore.Commerce.XA.Foundation.Common.Attributes;
using Sitecore.Commerce.XA.Foundation.Common.Context;
using Sitecore.Commerce.XA.Foundation.Common.Controllers;
using Sitecore.Commerce.XA.Foundation.Connect;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Sitecore.XA.Foundation.Mvc;
using Stripe;
using XC.SXC.StripePaymentProcessor.Models;
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
        public ActionResult XcBilling(StripePaymentResponseModel stripePaymentResponseModel)
        {
            var stripeApiKey = Settings.GetSetting("stripeApiKey", "sk_test_nOpeQ7HuJ2uOkYVoto3TW5R800npGIrfCK");
            var stripeCustomerService = new CustomerService();
            var stripeAuthorizeChargeService = new ChargeService();
            var stripeCustomerId = string.Empty;

            if (stripePaymentResponseModel != null && !string.IsNullOrEmpty(stripePaymentResponseModel.Id))
            {
                StripeConfiguration.SetApiKey(stripeApiKey);

                #region Create Stripe Customer

                //Create Stripe Customer
                var customerCreateOptions = new CustomerCreateOptions
                {
                    Email = stripePaymentResponseModel.Email,
                    SourceToken = stripePaymentResponseModel.Id
                };

                var stripeCustomer = stripeCustomerService.Create(customerCreateOptions);

                if (stripeCustomer != null)
                {
                    stripeCustomerId = stripeCustomer.Id;
                }

                #endregion

                #region Authorize Transacation

                var authorizeChargeCreateOptions = new ChargeCreateOptions
                {
                    Amount = 999,
                    Currency = "USD",
                    StatementDescriptor = "XC-Stripe-Purchase",
                    ReceiptEmail = stripePaymentResponseModel.Email,
                    Capture = false,
                    CustomerId = stripeCustomer?.Id
                };

                var authorizeCharge = stripeAuthorizeChargeService.Create(authorizeChargeCreateOptions);

                if (authorizeCharge != null && authorizeCharge.Status == Constants.StripeResponseMessages.Success)
                {
                    var paymentInputModel = new PaymentInputModel
                    {
                        BillingItemPath = $"/sitecore/content/Sitecore/Storefront/Home/checkout/billing",
                        UserEmail = stripePaymentResponseModel.Email,
                        FederatedPayment = new FederatedPaymentInputModel
                        {
                            Amount = 220.00M,
                            CardPaymentAcceptCardPrefix = stripePaymentResponseModel.Card.Brand,
                            CardToken = $"{stripePaymentResponseModel.Id}|{stripeCustomer?.Id}",
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
                            CreditCardNumber = stripePaymentResponseModel.Card.Last4.ToString(),
                            CustomerNameOnPayment = stripePaymentResponseModel.Card.Name,
                            ExpirationMonth = Convert.ToInt32(stripePaymentResponseModel.Card.ExpMonth),
                            ExpirationYear = Convert.ToInt32(stripePaymentResponseModel.Card.ExpYear),
                            PaymentMethodID = "0CFFAB11-2674-4A18-AB04-228B1F8A1DEC",
                        }
                    };

                    var setPaymentMethodsResult = XcCheckoutRepository.SetPaymentMethods(VisitorContext, paymentInputModel);

                    if (setPaymentMethodsResult.Success)
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

                #endregion
            }

            return Json(new
            {
                Success = false,
                ErrorMessage = "Stripe returned an invalid response",
                CardDetails = Json(new
                {
                    CardType = "",
                    LastFourDigits = "",
                    ExpiryDate = ""
                })
            });
        }

        #endregion
    }
}