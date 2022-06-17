# reallyread.it api

### Setup with Docker

See the [dev-env](https://github.com/reallyreadit/dev-env) instructions to set up this API server as part of the Docker Compose project.

## Manual Setup
1. Install the .NET Core 3.1 SDK: https://dotnet.microsoft.com/download
2. Configure the ASP.NET core environment for development: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/environments?view=aspnetcore-3.1#set-the-environment

        export ASPNETCORE_ENVIRONMENT=Development
3. Create the following configuration files, making these changes:

    - Replace `PG_USER`, `PG_PASS` and `DB_NAME` under the `Database` section with the values you chose during the database setup.
    - Set the value for `SystemEmojiFontName` under the `TwitterImageRendering` section. The default values are `Apple Color Emoji` or `Segoe UI Emoji` for macOS and Windows respectively. When running with Linux, find, install and enter `Noto Color Emoji`.
	 - Optionally set values for `Host` and `Port` under the `Email.SmtpServer` section if you want to capture email messages that would be sent by the server.
	 - Optionally set the value for `StripeApiSecretKey` under the `Subscriptions` section if you want to perform test purchases through Stripe.
	 - Optionally set the value for `StripeWebhookSigningSecret` under the `Subscriptions` section if you want to receive and process Stripe webhook events.

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
    	"PushNotifications": {
    		"ApnsServer": {
    			"Protocol": "https",
    			"Host": "api.sandbox.push.apple.com"
    		},
    		"ClientCertThumbprint": ""
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
    		"ProviderAccountEnvironment": "Production",
    		"StripeApiSecretKey": "",
    		"StripeSubscriptionProductId": "prod_v1_subscription",
    		"StripeWebhookSigningSecret": ""
    	},
    	"Tokenization": {
    		"EncryptionKey": "AAAAAAAAAAAAAAAAAAAAAA=="
    	},
    	"TwitterImageRendering": {
    		"SystemEmojiFontName": ""
    	}
    }
    ```
5. Restore packages

        dotnet restore
4. Build and run the server

        dotnet watch run


To use development tools with the C# extension in VSCode (via OmniSharp), the .NET SDK 6 also needs to be installed on your machine.