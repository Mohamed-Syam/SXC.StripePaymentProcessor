using System.Web.Mvc;
using System.Web.SessionState;
using Sitecore.Commerce.XA.Feature.Cart.Repositories;
using Sitecore.Commerce.XA.Foundation.Common.Attributes;
using Sitecore.Commerce.XA.Foundation.Common.Context;
using Sitecore.Commerce.XA.Foundation.Common.Controllers;
using Sitecore.Commerce.XA.Foundation.Connect;
using Sitecore.Diagnostics;

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

        }

        #endregion

        #region Billing

        [AllowAnonymous]
        [StorefrontSessionState(SessionStateBehavior.ReadOnly)]
        public ActionResult XcBilling()
        {
            return (ActionResult) View(GetRenderingView(nameof(XcBilling)), BillingRepository.GetBillingRenderingModel(this.Rendering));
        }

        #endregion
    }
}