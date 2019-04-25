using Newtonsoft.Json;

namespace XC.SXC.StripePaymentProcessor.Models
{
    public partial class StripePaymentResponseModel
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("object")]
        public string Object { get; set; }

        [JsonProperty("card")]
        public Card Card { get; set; }

        [JsonProperty("clientIp")]
        public string ClientIp { get; set; }

        [JsonProperty("created")]
        public long Created { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("livemode")]
        public bool LiveMode { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("used")]
        public bool Used { get; set; }
    }

    public partial class Card
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("object")]
        public string Object { get; set; }

        [JsonProperty("addressCity")]
        public string AddressCity { get; set; }

        [JsonProperty("addressCountry")]
        public object AddressCountry { get; set; }

        [JsonProperty("addressLine1")]
        public string AddressLine1 { get; set; }

        [JsonProperty("addressLine1Check")]
        public string AddressLine1Check { get; set; }

        [JsonProperty("addressLine2")]
        public object AddressLine2 { get; set; }

        [JsonProperty("addressState")]
        public string AddressState { get; set; }

        [JsonProperty("addressZip")]
        public string AddressZip { get; set; }

        [JsonProperty("addressZipCheck")]
        public string AddressZipCheck { get; set; }

        [JsonProperty("brand")]
        public string Brand { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; }

        [JsonProperty("cvcCheck")]
        public string CvcCheck { get; set; }

        [JsonProperty("dynamicLast4")]
        public object DynamicLast4 { get; set; }

        [JsonProperty("expMonth")]
        public long ExpMonth { get; set; }

        [JsonProperty("expYear")]
        public long ExpYear { get; set; }

        [JsonProperty("funding")]
        public string Funding { get; set; }

        [JsonProperty("last4")]
        public int Last4 { get; set; }

        [JsonProperty("metadata")]
        public Metadata Metadata { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("tokenizationMethod")]
        public object TokenizationMethod { get; set; }
    }

    public partial class Metadata
    {
    }
}
