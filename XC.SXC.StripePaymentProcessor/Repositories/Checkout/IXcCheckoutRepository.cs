using Sitecore.Commerce.XA.Feature.Cart.Models.InputModels;
using Sitecore.Commerce.XA.Feature.Cart.Models.JsonResults;
using Sitecore.Commerce.XA.Foundation.Connect;

namespace XC.SXC.StripePaymentProcessor.Repositories.Checkout
{
    public interface IXcCheckoutRepository
    {
        BillingDataJsonResult GetBillingData(IVisitorContext visitorContext);

        SetPaymentMethodsJsonResult SetPaymentMethods(IVisitorContext visitorContext, PaymentInputModel inputModel);
    }
}
