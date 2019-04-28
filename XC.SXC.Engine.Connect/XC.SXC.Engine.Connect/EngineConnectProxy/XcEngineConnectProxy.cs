using System;
using Sitecore.Commerce.Engine;
using Sitecore.Commerce.Engine.Connect;
using Sitecore.Commerce.ServiceProxy;
using XC.Commerce.Engine.Plugin.Stripe.Pipelines.Arguments;

namespace XC.SXC.Engine.Connect.EngineConnectProxy
{
    public class XcEngineConnectProxy
    {
        private readonly string _userId;
        private readonly string _customerId;
        private readonly string _currency;
        private readonly string _language;
        private readonly DateTime? _effectiveDate;

        public XcEngineConnectProxy(string shopName, string userId, string customerId = "", string currency = "", string language = "", DateTime? effectiveDate = null, string environment = "")
        {
            _userId = userId;
            _customerId = customerId;
            _currency = currency;
            _language = language;
            _effectiveDate = effectiveDate;
        }

        protected virtual Container GetContainer()
        {
            return EngineConnectUtility.GetShopsContainer("", "", _userId, _customerId, _language, _currency, _effectiveDate);
        }

        public bool StorePaymentDetails(StorePaymentDetailsArgument storePaymentDetailsArgument)
        {
            var command = Proxy.DoCommand(GetContainer().StorePaymentDetails(storePaymentDetailsArgument));

            return command.ResponseCode == Extensions.SuccessCode();
        }

        public bool CreateStripeCustomer(CreateStripeCustomerArgument createStripeCustomerArgument)
        {
            var command = Proxy.DoCommand(GetContainer().CreateStripeCustomer(createStripeCustomerArgument));

            return command.ResponseCode == Extensions.SuccessCode();
        }
    }
}