# reallyread.it api
## Setup Guide
1. Install the .NET Core SDK 1.1.5
2. Configure the .NET core environment for development

        ASPNETCORE_ENVIRONMENT=Development
3. Configure the api server

        /hosting.json:
    ```json
    {
    	"server.urls": "http://0.0.0.0:5000"
    }
    ```
        /appsettings.json
    ```json
    {
    	"Authentication": {
    		"CookieDomain": "dev.reallyread.it",
    		"CookieName": "devSessionKey",
    		"CookieSecure": "None",
    		"Scheme": "rrit-auth-scheme"
    	},
    	"Cors": {
    		"Origins": [
    			"http://dev.reallyread.it",
    			"chrome-extension://YOUR-LOCAL-EXTENSION-ID-HERE"
    		]
    	},
    	"Database": {
    		"ConnectionString": "Host=localhost;Username=YOUR-POSTGRES-USERNAME-HERE;Password=YOUR-POSTGRES-PASSWORD-HERE;Database=rrit"
    	},
    	"Email": {
    		"DeliveryMethod": "Smtp",
    		"EncryptionKey": "AAAAAAAAAAAAAAAAAAAAAA==",
    		"From": {
    			"Name": "reallyread.it",
    			"Address": "support@reallyread.it"
    		},
    		"SmtpServer": {
    			"Host": "localhost",
    			"Port": 25
    		}
    	},
    	"ServiceEndpoints": {
    		"ApiServer": {
    			"Protocol": "http",
    			"Host": "api.dev.reallyread.it"
    		},
    		"WebServer": {
    			"Protocol": "http",
    			"Host": "dev.reallyread.it"
    		}
    	}
    }
    ```
5. Restore packages

        dotnet restore
4. Build and run the server

        dotnet run
5. Set up a local SMTP server to capture emails (optional)

    When running in a dev environment the api server will attempt to connect to an SMTP server to send transactional emails. Installing a lightweight local server allows you to capture these emails. I'd recomment Papercut for Windows: https://github.com/ChangemakerStudios/Papercut
