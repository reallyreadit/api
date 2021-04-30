using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using api.ActionFilters;
using Npgsql;
using api.DataAccess.Models;
using System.Threading.Tasks;
using api.Configuration;
using Microsoft.Extensions.Configuration;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using MyAuthenticationOptions = api.Configuration.AuthenticationOptions;
using MyCookieOptions = api.Configuration.CookieOptions;
using MyDataProtectionOptions = api.Configuration.DataProtectionOptions;
using Microsoft.Extensions.Options;
using api.Messaging;
using Mvc.RenderViewToString;
using Microsoft.AspNetCore.Mvc.Razor;
using System;
using Microsoft.AspNetCore.DataProtection;
using api.Security;
using api.ReadingVerification;
using api.Encryption;
using api.Notifications;
using api.Commenting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Authorization;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using api.BackgroundProcessing;
using api.Authentication;
using api.Serialization;
using System.Linq;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using System.Net;
using api.Routing;
using api.ImageProcessing;
using SixLabors.Fonts;

namespace api {
	public class Startup {
		private IHostEnvironment env;
		private IConfiguration config;
		public Startup(
			IHostEnvironment env,
			IConfiguration config
		) {
			this.env = env;
			this.config = config;
		}
		public void ConfigureServices(IServiceCollection services) {
			// configure options
			IConfigurationSection
				authOptsConfigSection = config.GetSection("Authentication"),
				cookieConfigSection = config.GetSection("Cookies"),
				emailOptsConfigSection = config.GetSection("Email"),
				pushOptsConfigSection = config.GetSection("PushNotifications"),
				subscriptionsConfigSection = config.GetSection("Subscriptions");
			var authOpts = authOptsConfigSection.Get<MyAuthenticationOptions>();
			var cookieOpts = cookieConfigSection.Get<MyCookieOptions>();
			var emailOpts = emailOptsConfigSection.Get<EmailOptions>();
			var pushOpts = pushOptsConfigSection.Get<PushNotificationsOptions>();
			var subscriptionsOpts = subscriptionsConfigSection.Get<SubscriptionsOptions>();
			services
				.Configure<MyAuthenticationOptions>(authOptsConfigSection)
				.Configure<CaptchaOptions>(config.GetSection("Captcha"))
				.Configure<MyCookieOptions>(cookieConfigSection)
				.Configure<DatabaseOptions>(config.GetSection("Database"))
				.Configure<EmbedOptions>(config.GetSection("Embed"))
				.Configure<EmailOptions>(emailOptsConfigSection)
				.Configure<HashidsOptions>(config.GetSection("Hashids"))
				.Configure<PushNotificationsOptions>(pushOptsConfigSection)
				.Configure<RazorViewEngineOptions>(x => x.ViewLocationFormats.Add("/src/Messaging/Views/{0}.cshtml"))
				.Configure<ReadingVerificationOptions>(config.GetSection("ReadingVerification"))
				.Configure<ServiceEndpointsOptions>(config.GetSection("ServiceEndpoints"))
				.Configure<SubscriptionsOptions>(subscriptionsConfigSection)
				.Configure<TokenizationOptions>(config.GetSection("Tokenization"));
			// configure services
			Stripe.StripeConfiguration.ApiKey = subscriptionsOpts.StripeApiSecretKey;
			SigningCredentials appleAuthClientSecretSigningKey;
			if (env.IsProduction()) {
				appleAuthClientSecretSigningKey = new SigningCredentials(
					new ECDsaSecurityKey(
						new ECDsaCng(
							CngKey.Import(
								Convert.FromBase64String(
									PemParser
										.Parse(
											File.ReadAllText(authOpts.AppleAuth.ClientSecretSigningKeyPath)
										)
										.Single()
										.EncodedBody
								),
								CngKeyBlobFormat.Pkcs8PrivateBlob
							)
						)
					),
					SecurityAlgorithms.EcdsaSha256
				);
			} else {
				appleAuthClientSecretSigningKey = new SigningCredentials(
					new SymmetricSecurityKey(
						new byte[] { 0 }
					),
					SecurityAlgorithms.None
				);
			}
			// setup fonts
			var textFontFamily = new FontCollection()
				.Install("fonts/museo-sans-300.ttf");
			var emojiFontFamily = SystemFonts.Find(
				config
					.GetSection("TwitterImageRendering")
					.Get<TwitterImageRenderingOptions>()
					.SystemEmojiFontName
			);
			services
				.AddHostedService<QueuedHostedService>()
				.AddScoped<AuthenticationService>()
				.AddScoped<CaptchaService>()
				.AddScoped<CommentingService>()
				.AddScoped<NotificationService>()
				.AddScoped<ObfuscationService>()
				.AddTransient<RazorViewToStringRenderer>()
				.AddScoped<ReadingVerificationService>()
				.AddTransient<AppleAuthService>(
					services => new AppleAuthService(
						appleAuthClientSecretSigningKey,
						services.GetService<IHttpClientFactory>(),
						services.GetService<IOptions<MyAuthenticationOptions>>(),
						services.GetService<IOptions<DatabaseOptions>>(),
						services.GetService<ILogger<AppleAuthService>>()
					)
				)
				.AddTransient<RoutingService>()
				.AddTransient<TwitterAuthService>()
				.AddTransient<TweetImageRenderingService>(
					services => new TweetImageRenderingService(
						textFontFamily: textFontFamily,
						emojiFontFamily: emojiFontFamily
					)
				)
				.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
			// configure authentication and authorization
			services
				.AddAuthentication(defaultScheme: authOpts.Scheme)
				.AddCookie(
					authenticationScheme: authOpts.Scheme,
					configureOptions: options => {
						options.Cookie.Domain = cookieOpts.Domain;
						options.Cookie.Name = authOpts.CookieName;
						options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
						options.Cookie.HttpOnly = true;
						options.Cookie.SameSite = SameSiteMode.None;
						options.ExpireTimeSpan = TimeSpan.FromDays(180);
						options.SlidingExpiration = true;
						options.Events.OnRedirectToLogin = context => {
							context.Response.StatusCode = 401;
							return Task.CompletedTask;
						};
						options.Events.OnRedirectToAccessDenied = context => {
							context.Response.StatusCode = 403;
							return Task.CompletedTask;
						};
					}
				);
			services.AddAuthorization(
				options => {
					var legacyCookiePolicy = new AuthorizationPolicyBuilder(authenticationSchemes: authOpts.Scheme)
						.RequireAuthenticatedUser()
						.Build();
					options.DefaultPolicy = legacyCookiePolicy;
					options.FallbackPolicy = legacyCookiePolicy;
				}
			);
			// configure http clients
			X509Certificate2 apnsClientCert;
			if (env.IsProduction()) {
				using (var store = new X509Store(StoreName.My, StoreLocation.LocalMachine, OpenFlags.ReadOnly)) {
					apnsClientCert = store.Certificates.Find(
						findType: X509FindType.FindByThumbprint,
						findValue: pushOpts.ClientCertThumbprint,
						validOnly: false
					)[0];
				}
			} else {
				apnsClientCert = new X509Certificate2();
			}
			services
				.AddHttpClient()
				.AddHttpClient<ApnsService>()
				.ConfigurePrimaryHttpMessageHandler(
					() => new HttpClientHandler() {
						ClientCertificates = { apnsClientCert }
					}
				);
			// configure http context
			services.AddHttpContextAccessor();
			// configure email service
			switch (emailOpts.DeliveryMethod) {
				case EmailDeliveryMethod.AmazonSes:
					services.AddScoped<EmailService, AmazonSesEmailService>();
					break;
				case EmailDeliveryMethod.Smtp:
					services.AddScoped<EmailService, SmtpEmailService>();
					break;
				default:
					throw new ArgumentException("Unexpected value for EmailDeliveryMethod");
			}
			// configure shared key ring in production
			if (env.IsProduction()) {
				var dataProtectionOptions = config
					.GetSection("DataProtection")
					.Get<MyDataProtectionOptions>();
				services
					.AddDataProtection()
					.SetApplicationName(dataProtectionOptions.ApplicationName)
					.PersistKeysToFileSystem(new DirectoryInfo(dataProtectionOptions.KeyPath));
			}
			// configure MVC
			services.AddControllersWithViews(
				options => {
					// configure delay in production to simulate network delay
					if (env.IsDevelopment()) {
						options.Filters.Add(new DelayActionFilter(500));
					}
				}
			);
		}
		public void Configure(
			IApplicationBuilder app
		) {
			if (env.IsDevelopment()) {
				app.UseDeveloperExceptionPage();
			}
			// configure forwarded headers
			app.UseForwardedHeaders(
				new ForwardedHeadersOptions() {
					ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
					KnownNetworks = {
						new IPNetwork(
							prefix: new IPAddress(
								new byte[] { 172, 31, 10, 0 }
							),
							prefixLength: 24
						),
						new IPNetwork(
							prefix: new IPAddress(
								new byte[] { 172, 31, 11, 0 }
							),
							prefixLength: 24
						)
					},
					RequireHeaderSymmetry = false
				}
			);
			// configure routing
			app.UseRouting();
			// configure cors
			var corsOptions = config
				.GetSection("Cors")
				.Get<CorsOptions>();
			app.UseCors(
				cors => cors
					.SetIsOriginAllowed(
						origin => corsOptions.AllowedOrigins.Any(
							allowedOrigin => allowedOrigin.EndsWith("://") ?
								origin.StartsWith(allowedOrigin) :
								origin == allowedOrigin
						)
					)
					.AllowCredentials()
					.AllowAnyHeader()
					.AllowAnyMethod()
					.SetPreflightMaxAge(TimeSpan.FromDays(1))
			);
			// configure cookie authentication & authorization
			app.UseAuthentication();
			app.UseAuthorization();
			// configure mvc
			app.UseEndpoints(
				endpoints => {
					endpoints.MapDefaultControllerRoute();
					endpoints.MapControllerRoute(
						name: "Articles/ListComments Redirect",
						pattern: "Articles/ListComments",
						defaults: new {
							Action = "Comments",
							Controller = "Social"
						}
					);
					endpoints.MapControllerRoute(
						name: "Articles/PostComment Redirect",
						pattern: "Articles/PostComment",
						defaults: new {
							Action = "Comment",
							Controller = "Social"
						}
					);
				}
			);
			// configure Npgsql
			NpgsqlConnection.GlobalTypeMapper.MapEnum<ArticleFlair>();
			NpgsqlConnection.GlobalTypeMapper.MapEnum<AuthServiceProvider>();
			NpgsqlConnection.GlobalTypeMapper.MapEnum<AuthorAssignmentMethod>();
			NpgsqlConnection.GlobalTypeMapper.MapEnum<DisplayTheme>();
			NpgsqlConnection.GlobalTypeMapper.MapEnum<SourceRuleAction>();
			NpgsqlConnection.GlobalTypeMapper.MapEnum<UserAccountRole>();
			NpgsqlConnection.GlobalTypeMapper.MapEnum<NotificationChannel>();
			NpgsqlConnection.GlobalTypeMapper.MapEnum<NotificationAction>();
			NpgsqlConnection.GlobalTypeMapper.MapEnum<NotificationEventFrequency>();
			NpgsqlConnection.GlobalTypeMapper.MapEnum<NotificationEventType>();
			NpgsqlConnection.GlobalTypeMapper.MapEnum<NotificationPushUnregistrationReason>();
			NpgsqlConnection.GlobalTypeMapper.MapEnum<SubscriptionEnvironment>();
			NpgsqlConnection.GlobalTypeMapper.MapEnum<SubscriptionEventSource>();
			NpgsqlConnection.GlobalTypeMapper.MapEnum<SubscriptionPaymentMethodBrand>();
			NpgsqlConnection.GlobalTypeMapper.MapEnum<SubscriptionPaymentMethodWallet>();
			NpgsqlConnection.GlobalTypeMapper.MapEnum<SubscriptionPaymentStatus>();
			NpgsqlConnection.GlobalTypeMapper.MapEnum<SubscriptionProvider>();
			NpgsqlConnection.GlobalTypeMapper.MapEnum<TwitterHandleAssignment>();
			NpgsqlConnection.GlobalTypeMapper.MapComposite<ArticleAuthor>();
			NpgsqlConnection.GlobalTypeMapper.MapComposite<AuthorMetadata>();
			NpgsqlConnection.GlobalTypeMapper.MapComposite<CommentAddendum>();
			NpgsqlConnection.GlobalTypeMapper.MapComposite<Ranking>();
			NpgsqlConnection.GlobalTypeMapper.MapComposite<StreakRanking>();
			NpgsqlConnection.GlobalTypeMapper.MapComposite<SubscriptionDistributionAuthorCalculation>();
			NpgsqlConnection.GlobalTypeMapper.MapComposite<SubscriptionDistributionAuthorReport>();
			NpgsqlConnection.GlobalTypeMapper.MapComposite<SubscriptionStatusLatestPeriod>();
			NpgsqlConnection.GlobalTypeMapper.MapComposite<SubscriptionStatusLatestRenewalStatusChange>();
			NpgsqlConnection.GlobalTypeMapper.MapComposite<TagMetadata>();
			// configure Dapper
			Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
		}
	}
}