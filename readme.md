# reallyread.it api
## Setup Guide
1. Install the .NET Core 3.1 SDK: https://dotnet.microsoft.com/download
2. Configure the ASP.NET core environment for development: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/environments?view=aspnetcore-3.1#set-the-environment

        ASPNETCORE_ENVIRONMENT=Development
3. Create the following configuration files, making these changes:

    - Replace `PG_USER`, `PG_PASS` and `DB_NAME` under the `Database` section with the values you chose during the database setup.
	 - Optionally set values for `Host` and `Port` under the `Email.SmtpServer` section if you want to capture email messages that would be sent by the server.
	 - Optionally set the value for `StripeApiSecretKey` under the `Subscriptions` section if you want to perform test purchases through Stripe.

    Note the following regarding 3rd party services:
	 - AppleID authentication is not supported in development mode.
	 - Apple Push Notifications are not supported in development mode.
	 - Twitter integration is supported in development mode using the Readup Twitter Test Server: https://github.com/reallyreadit/twitter-test-server
    <!--end list-->

        hostsettings.json
    ```json
    {
    	"urls": "http://0.0.0.0:5000"
    }
    ```
        appsettings.json
    ```json
    {
    	"Authentication": {
    		"ApiKey": "AAAAAAAAAAAAAAAAAAAAAA==",
    		"CookieName": "devSessionKey",
    		"CookieSecure": "Always",
    		"Scheme": "rrit-auth-scheme",
    		"TwitterAuth": {
    			"BrowserAuthCallback": "https://api.dev.readup.com/Auth/TwitterAuthenticationCallback",
    			"BrowserLinkCallback": "https://api.dev.readup.com/Auth/TwitterLinkCallback",
    			"BrowserPopupCallback": "https://api.dev.readup.com/Auth/TwitterPopupCallback",
    			"ConsumerKey": "",
    			"ConsumerSecret": "",
    			"SearchAccount": {
    				"Handle": "",
    				"OAuthToken": "",
    				"OAuthTokenSecret": ""
    			},
    			"TwitterApiServerUrl": "https://twitter-test.dev.readup.com",
    			"TwitterUploadServerUrl": "https://twitter-test.dev.readup.com",
    			"WebViewCallback": "readup://"
    		}
    	},
    	"Captcha": {
    		"VerifyCaptcha": false
    	},
    	"Cookies": {
    		"Domain": ".dev.readup.com"
    	},
    	"Cors": {
    		"AllowedOrigins": [
    			"chrome-extension://",
    			"https://blog.dev.readup.com",
    			"https://dev.readup.com",
    			"moz-extension://",
    			"safari-web-extension://"
    		]
    	},
    	"Database": {
    		"ConnectionString": "Host=localhost;Username=PG_USER;Password=PG_PASS;Database=DB_NAME"
    	},
    	"Embed": {
    		"AllowedHosts": [
    			"blog.dev.readup.com"
    		]
    	},
    	"Email": {
    		"DeliveryMethod": "Smtp",
    		"From": {
    			"Name": "Readup",
    			"Address": "support@readup.com"
    		},
    		"SmtpServer": {
    			"Host": "",
    			"Port": 0
    		}
    	},
    	"Hashids": {
    		"Salt": "AAAAAAAAAAAAAAAAAAAAAA=="
    	},
    	"ReadingVerification": {
    		"EncryptionKey": "AAAAAAAAAAAAAAAAAAAAAA=="
    	},
    	"ServiceEndpoints": {
    		"ApiServer": {
    			"Protocol": "https",
    			"Host": "api.dev.readup.com"
    		},
    		"StaticContentServer": {
    			"Protocol": "https",
    			"Host": "static.dev.readup.com"
    		},
    		"WebServer": {
    			"Protocol": "https",
    			"Host": "dev.readup.com"
    		}
    	},
    	"Subscriptions": {
    		"AppStoreSandboxUrl": "https://sandbox.itunes.apple.com/verifyReceipt",
    		"AppStoreProductionUrl": "https://buy.itunes.apple.com/verifyReceipt",
    		"StripeApiSecretKey": "",
    		"StripeSubscriptionProductId": "prod_v1_subscription"
    	},
    	"Tokenization": {
    		"EncryptionKey": "AAAAAAAAAAAAAAAAAAAAAA=="
    	}
    }
    ```
5. Restore packages

        dotnet restore
4. Build and run the server

        dotnet watch run