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
            var stripeCustomerId = string.Empty;
            var bankAccount = new BankAccount();
            var achCustomer = new Customer();
            StripeConfiguration.SetApiKey(StripeApiKey);

            #region Create Token - This is needed only if the user is adding a new bank account

            var tokenCreateOptions = new TokenCreateOptions
            {
                BankAccount = new BankAccountOptions
                {
                    AccountHolderName = "John Doe",
                    AccountHolderType = "Individual",
                    AccountNumber = "000123456789",
                    Country = "US",
                    Currency = "USD",
                    RoutingNumber = "110000000"
                },
            };

            var createTokenResult = StripeTokenService.Create(tokenCreateOptions);

            #endregion

            #region Create Customer - This is needed when we need to create a ACH customer on Stripe

            if (createTokenResult != null && createTokenResult.BankAccount != null)
            {
                var customerCreateOptions = new CustomerCreateOptions
                {
                    SourceToken = createTokenResult.Id,
                    Description = "ACH Customer",
                    Email = $"srikanth.kondapally@xcentium.com",
                    Address = new AddressOptions
                    {
                        Line1 = "123 Main Street",
                        Line2 = "Suite 303",
                        City = "Atlanta",
                        State = "GA",
                        Country = "US",
                        PostalCode = "30346"
                    }
                };

                achCustomer = StripeCustomerService.Create(customerCreateOptions);
            }

            #endregion

            #region Bank Account Verification - This is needed to verify user's bank account information. If it is not verified, it cannot be used for payments

            if (achCustomer != null)
            {
                var bankAccountId = achCustomer.Sources?.Data?.FirstOrDefault(x => x.Object == "bank_account") != null
                    ? achCustomer.Sources.Data.FirstOrDefault(x => x.Object == "bank_account")?.Id ?? string.Empty
                    : string.Empty;

                if (!string.IsNullOrEmpty(bankAccountId))
                {
                    var bankAccountVerifyOptions = new BankAccountVerifyOptions
                    {
                        Amounts = new List<long>
                        {
                            32, 45 //32 and 45 are the test values which will always return a success. If any other combination is passed, it will fail - applicable only for TEST mode
                        }
                    };

                    bankAccount = StripeBankAccountService.Verify(achCustomer.Id, bankAccountId, bankAccountVerifyOptions);
                }
            }

            #endregion

            #region Apply payment using ACH

            if (bankAccount.Status == "verified")
            {
                var chargeCreateOptions = new ChargeCreateOptions
                {
                    Amount = 999,
                    Currency = "USD",
                    StatementDescriptor = "XC-Stripe-POC-ACH",
                    ReceiptEmail = "srikanth.kondapally@xcentium.com",
                    CustomerId = achCustomer?.Id
                };

                var authorizationChargeResult = StripeAuthorizeChargeService.Create(chargeCreateOptions);

                if (authorizationChargeResult != null && authorizationChargeResult.Status == "pending")
                {
                    var paymentInputModel = new PaymentInputModel
                    {
                        BillingItemPath = $"/sitecore/content/Sitecore/Storefront/Home/checkout/billing",
                        UserEmail = "srikanth.kondapally@xcentium.com",
                        FederatedPayment = new FederatedPaymentInputModel
                        {
                            Amount = 220.00M,
                            CardPaymentAcceptCardPrefix = bankAccount?.BankName,
                            CardToken = $"{bankAccount?.AccountId}|{achCustomer?.Id}",
                            PaymentMethodID = "0CFFAB11-2674-4A18-AB04-228B1F8A1DEC",
                        },
                        BillingAddress = new PartyInputModel
                        {
                            Address1 = achCustomer?.Address.Line1,
                            PartyId = "0",
                            ExternalId = "Billing",
                            Name = bankAccount.AccountHolderName,
                            City = achCustomer?.Address?.City,
                            Country = achCustomer?.Address?.Country,
                            State = achCustomer?.Address?.State,
                            ZipPostalCode = achCustomer?.Address?.PostalCode
                        }
                    };

                    var setAchPaymentMethodsResult = XcCheckoutRepository.SetPaymentMethods(VisitorContext, paymentInputModel);

                    if (setAchPaymentMethodsResult.Success)
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

            #endregion

            return Json(new
            {
                Success = false,
                ErrorMessage = "There was an issue setting ACH Payment with Stripe",
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
            var stripeCustomerId = string.Empty;

            if (stripePaymentResponseModel != null && !string.IsNullOrEmpty(stripePaymentResponseModel.Id))
            {
                StripeConfiguration.SetApiKey(StripeApiKey);

                #region Create Stripe Customer

                //Create Stripe Customer
                var customerCreateOptions = new CustomerCreateOptions
                {
                    Email = stripePaymentResponseModel.Email,
                    SourceToken = stripePaymentResponseModel.Id
                };

                var stripeCustomer = StripeCustomerService.Create(customerCreateOptions);

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
                    StatementDescriptor = "XC-Stripe-POC-CC",
                    ReceiptEmail = stripePaymentResponseModel.Email,
                    Capture = false,
                    CustomerId = stripeCustomer?.Id
                };

                var authorizeCharge = StripeAuthorizeChargeService.Create(authorizeChargeCreateOptions);

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
                ErrorMessage = "There was an issue setting credit card payment with Stripe",
                CardDetails = Json(new
                {
                    CardType = "",
                    LastFourDigits = "",
                    ExpiryDate = ""
                })
            });
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