using Sitecore.Commerce.XA.Feature.Cart.Models.InputModels;
using Sitecore.Commerce.XA.Feature.Cart.Models.JsonResults;
using Sitecore.Commerce.XA.Foundation.Connect;
using XC.SXC.StripePaymentProcessor.Models;

namespace XC.SXC.StripePaymentProcessor.Repositories.Checkout
{
    public interface IXcCheckoutRepository
    {
        BillingDataJsonResult GetBillingData(IVisitorContext visitorContext);

        SetPaymentMethodsJsonResult SetPaymentMethods(IVisitorContext visitorContext, PaymentInputModel inputModel);

        SetPaymentMethodsJsonResult SendUserToReviewPage(IVisitorContext visitorContext, PaymentInputModel inputModel);

        bool StorePaymentDetails(StripePaymentResponseModel stripePaymentResponseModel, string cartId, bool userSelectedAchPayment = false);

        bool CreateStripeCustomer(string email, string stripeTokenId, string cartId);
    }
}
