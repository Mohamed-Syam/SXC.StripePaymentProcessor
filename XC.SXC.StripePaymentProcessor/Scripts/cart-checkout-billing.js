// Copyright 2017 Sitecore Corporation A/S
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file 
// except in compliance with the License. You may obtain a copy of the License at
//       http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the 
// License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, 
// either express or implied. See the License for the specific language governing permissions 
// and limitations under the License.
// -------------------------------------------------------------------------------------------

(function (root, factory) {
    'use strict';
    if (typeof define === 'function' && define.amd) {
        // use AMD define funtion to support AMD modules if in use
        define(['exports'], factory);

    } else if (typeof exports === 'object') {
        // to support CommonJS
        factory(exports);

    }

    // browser global variable
    root.CheckoutBilling = factory;
    root.CheckoutBilling_ComponentClass = "cxa-checkoutbilling-component";

}(this, function (element) {
    'use strict';
    var component = new Component(element);
    component.Name = "CXA/Feature/CheckoutBilling";
    component.billingVM = null;
    component.Element = element;

    component.InExperienceEditorMode = function () {
        component.Visual.Disable();
    }

    component.StartListening = function () {
		  
    }
    component.StopListening = function () {
    }

    component.Init = function () {
        component.billingVM = new BillingDataViewModel();
        component.creditCardPaymentViewModel = new BillingDataViewModel();
        component.billingVM.load();
        ko.applyBindingsWithValidation(component.billingVM, component.Element);
        component.StartListening();
        component.SetupBillingPage();
    }
	component.stripeInit = function() {
    // Simple localization
    const isStripeDev = window.location.hostname === 'storefront.local';
    const localeIndex = isStripeDev ? 2 : 1;
    window.__exampleLocale = window.location.pathname.split('/')[localeIndex] || 'en';
    const urlPrefix = isStripeDev ? '/elements-examples/' : '/';
    
    document.querySelectorAll('.optionList a').forEach(function(langNode) {
      const langValue = langNode.getAttribute('data-lang');
      const langUrl = langValue === 'en' ? urlPrefix : (urlPrefix + langValue + '/');
    
      if (langUrl === window.location.pathname || langUrl === window.location.pathname + '/') {
        langNode.className += ' selected';
        langNode.parentNode.setAttribute('aria-selected', 'true');
      } else {
        langNode.setAttribute('href', langUrl);
        langNode.parentNode.setAttribute('aria-selected', 'false');
      }
    });
    
            "use strict";
            var stripe = Stripe('pk_test_ZpqkF5x1guX4lt6Tzu6r1zOR00BCXsXhp3');
    
            var elements = stripe.elements({
                locale: window.__exampleLocale
            });
    
            /* Begin: Render Card Element */
            var style = {
                    base: {
                         'lineHeight': '1.35',
                    'fontSize': '1.11rem',
                    'color': '#495057',
                    'fontFamily': 'apple-system,BlinkMacSystemFont,"Segoe UI",Roboto,"Helvetica Neue",Arial,sans-serif'
                    }
            };
            var stripeCard = elements.create("card",
                {
                    iconStyle:"solid"
                    
                });
            stripeCard.mount("#stripe-creditcard");
            window.mountStripe = function(){
               stripeCard.mount("#stripe-creditcard");
            };
    
            /* End: Render Card Element */
    
            /*Begin: Render Payment Request Button*/
			var paymentAmount = parseFloat((component.billingVM.cart().totalAmount()).toFixed(2).replace('.', ''));
            var paymentRequest = stripe.paymentRequest({
                country: "US",
                currency: "usd",
                total: {
                    amount: paymentAmount, 
                    label: "Total"
                },
                requestShipping: false,
                requestPayerEmail: true,
                shippingOptions: [
                    {
                        id: "free-shipping",
                        label: "Free shipping",
                        detail: "Arrives in 5 to 7 days",
                        amount: 0
                    },
                    {
                        id: "expedited-shipping",
                        label: "Priority shipping",
                        detail: "Arrives in 2 to 3 days",
                        amount: 599
                    }
                ]
            });
    
            paymentRequest.on("token", function (result) {
                component.billingVM.paymentProcessing(true);
                var stripepaymentform = document.querySelector(".stripepaymentform");
                result.complete("success");
				if (result.token) {
					var data = JSON.stringify(RemoveUnderscores(result.token));
					// If we received a token, show the token ID.
					$.ajax({ cache: false,
						url: "/api/cxa/xccheckout/xcbilling",
						type: "POST",
						contentType: 'application/json; charset=utf-8',
						data: data,
						dataType: 'json'
					}).done(function (data) {
						if(data.Success){
                            component.billingVM.achStripePaymentProcessed(true);
                            bindViewWithHtml(data);
                            component.billingVM.paymentProcessing(false);
                        }
						else{
                            console.log('Please try again');   
                            component.billingVM.paymentProcessing(false);

						}
						
					}).fail(function (data) {
                        console.log("error submitting Stripe Credit Card payment", data);
                        component.billingVM.paymentProcessing(false);                        
					});
				}
            });
    
            var paymentRequestElement = elements.create("paymentRequestButton", {
                paymentRequest: paymentRequest,
                style: {
                    paymentRequestButton: {
                        theme: "light-outline"
                    }
                }
            });
    
            paymentRequest.canMakePayment().then(function (result) {
                if (result) {
                    // document.querySelector(".stripepaymentform .card-only").style.display = "none";
                    // document.querySelector(
                            // ".stripepaymentform .payment-request-available"
                        // ).style.display =
                        // "block";
                    paymentRequestElement.mount("#stripe-paymentRequest");
                }
            });
            function bindViewWithHtml(data){
                var cardData = data.CardDetails.Data;
                var imageType = "";
                switch(cardData.CardType){
                    case "Discover":
                    imageType = "<span class='payment-method-icon discover'></span>";
                    break;
                    case "American Express":
                    imageType = "<span class='payment-method-icon american-express'></span>";
                    break;
                    case "MasterCard":
                    imageType = "<span class='payment-method-icon master-card'></span>";
                    break;
                    default:
                    imageType = "<span class='payment-method-icon visa'></span>";

                }
                $(component.RootElement).find('.credit-card-payment-section').html('');
                $(component.RootElement).find('.credit-card-payment-section').html('<div style="margin-bottom: 25px; font-size: 16px; position: relative;"><div>'+ imageType + ' <span style="display: inline-block; margin-left: 50px; "> ending in ' + cardData.LastFourDigits + '&nbsp;|&nbsp;' +  cardData.ExpiryDate+'</span></div></div>' );   
            }
            /*End: Render Payment Request Button*/
            registerElements(stripeCard);
            function registerElements(stripeCardElement) {
                var email = document.querySelectorAll('#billingInputEmail');
                var paymentForm = document.querySelectorAll('.credit-card-payment-form');
                // Listen on the form's 'submit' handler...
                document.body.addEventListener('click',
                    function (e) {
                        if (e.srcElement.id === 'submitStripePayment') {
                            e.preventDefault();
                            component.billingVM.paymentProcessing(true);

                            // Gather additional customer data we may have collected in our form.
                            var emailValue = email[0].value || "";
                            console.log(email);
                            var name = document.querySelector('#address-name');
                            var address1 = document.querySelector('#address-address1');
                            var city = document.querySelector('#address-city');
                            var state = document.querySelector('#address-state');
                            var zip = document.querySelector('#address-country');
                            var additionalData = {
                                email: emailValue ? emailValue : undefined,
                                name: name ? name.value : undefined,
                                address_line1: address1 ? address1.value : undefined,
                                address_city: city ? city.value : undefined,
                                address_state: state ? state.value : undefined,
                                address_zip: zip ? zip.value : undefined,
                            };
                            console.log(additionalData);
                            
                            // Use Stripe.js to create a token. We only need to pass in one Element
                            // from the Element group in order to create a token. We can also pass
                            // in the additional customer data we collected in our form.
                            stripe.createToken(stripeCardElement, additionalData).then(function (result) {
                            window.stripePaymentdone = true;
    
                                if (result.token) {
									var data = JSON.stringify(RemoveUnderscores(result.token));
                                    // If we received a token, show the token ID.
                                    $.ajax({ cache: false,
                                        url: "/api/cxa/xccheckout/xcbilling",
										type: "POST",
										contentType: 'application/json; charset=utf-8',
										data: data,
                                        dataType: 'json'
                                    }).done(function (data) {
                                        if(data.Success){
                                            component.billingVM.achStripePaymentProcessed(true);
                                            bindViewWithHtml(data);
                                            component.billingVM.paymentProcessing(false);

                                        }
                                        else{
                                            component.billingVM.paymentProcessing(false);
                                            console.log('Please try again');   
                                        }
                                        
                                    }).fail(function (data) {
                                        console.log("error submitting Stripe Credit Card payment", data);
                                        component.billingVM.paymentProcessing(false);
                                    });
                                }
                            }).catch(function (e) {
                                console.log(e);
                            });
    
                        }
                    });
            }

   };
    component.SetupBillingPage = function () {
        $('.apply-credit-card-toggle').on('click', function (event) {
            event.preventDefault();
			component.stripeInit();
            if(!component.billingVM.isEmailFilled()){
                return false;
            }
            // create accordion variables
            var accordion = $(this);
            var accordionContent = $(component.RootElement).find('.credit-card-payment-section');
            //var accordionToggleIcon = $(this).children('.toggle-icon');

            // toggle accordion link open class
            accordion.toggleClass("open");

            // toggle accordion content
            accordionContent.slideToggle(250);

            // change plus/minus icon
            if (accordion.hasClass("open")) {
                //accordionToggleIcon.html("<span class='glyphicon glyphicon-minus-sign'></span>");
                if ($(this).hasClass("ccpayment")) {
                    component.billingVM.creditCardPayment().isAdded(true);
                    if (component.billingVM.paymentClientToken() != null) {
                        component.billingVM.creditCardEnable(true);
                        component.billingVM.billingAddressEnable(true);
                    }
                }                
            } 
        });

        $('.apply-gift-card-toggle').on('click', function (event) {
            event.preventDefault();
			if(!component.billingVM.isEmailFilled()){
                return false;
            }

            // create accordion variables
            var accordion = $(this);
            var accordionContent = $(component.RootElement).find('.apply-gift-card-section');
            //var accordionToggleIcon = $(this).children('.toggle-icon');

            // toggle accordion link open class
            accordion.toggleClass("open");

            // toggle accordion content
            accordionContent.slideToggle(250);
            
        });

        $('.apply-ach-payment-toggle').on('click', function (event) {
            event.preventDefault();
			if(!component.billingVM.isEmailFilled()){
                return false;
            }
            // create accordion variables
            var accordion = $(this);
            var accordionContent = $(component.RootElement).find('.ach-payment-section');
            //var accordionToggleIcon = $(this).children('.toggle-icon');

            // toggle accordion link open class
            accordion.toggleClass("open");

            // toggle accordion content
            accordionContent.slideToggle(250);
            
        });
    }
}));
function RemoveUnderscores(json) {
  var regexp = new RegExp('_(\\w)', 'g');
  var replacer = function (match, p1, offset, string) {
        return p1.toUpperCase();
    }
    var returnObj = {};

  for (var key in json) {
      if (!json.hasOwnProperty(key)) {
        continue;
    }
    var value = json[key];
    var newKey = key.replace(regexp, replacer);

    if (typeof(value) === 'object') {
        var newObj = RemoveUnderscores(value);
        returnObj[newKey] = newObj;
    }
    else {
        returnObj[newKey] = value;
    }
  }

  return returnObj;
}

$(document).ready(function () {
    $("." + CheckoutBilling_ComponentClass).each(function () {
        var component = new CheckoutBilling(this);
   
});
 });
