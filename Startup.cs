using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ArmisApp.Services;
using ArmisApp.Models.Identity;
using ArmisApp.Models.Domain.context;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileProviders;
using System.IO;
using System.Text;
using System.Net;
using Microsoft.Net.Http.Headers;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using ArmisApp.Models.Utility;
using Parbad.Builder;
using Parbad.Storage.EntityFrameworkCore.Builder;
using Microsoft.Extensions.Hosting;
using ArmisApp.Models.Repository;
using ArmisApp.Models.ExMethod;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;
using Parbad.Gateway.Melli;
using Parbad.Gateway.Mellat;
using Parbad.Gateway.ParbadVirtual;
using ArmisApp.Models.SignalRChat;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
//using WebMarkupMin.AspNetCore2;

namespace ArmisApp
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<DataContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<DataContext>()
                .AddDefaultTokenProviders();
            //services.AddDataProtection()
            //    .ProtectKeysWithDpapi(protectToLocalMachine: true);

            services.Configure<SecurityStampValidatorOptions>(options =>
            options.ValidationInterval = TimeSpan.FromMinutes(0));

            services.AddAuthentication(
                options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
            })
            .AddCookie()
            .AddGoogle(options =>
            {
                options.ClientId = Configuration["Authentication:Google:ClientId"];
                options.ClientSecret = Configuration["Authentication:Google:ClientSecret"];
                options.SignInScheme = IdentityConstants.ExternalScheme;
            });
            //.AddMicrosoftAccount(microsoftOptions =>
            //{
            //    microsoftOptions.ClientId = Configuration["Authentication:Microsoft:ClientId"];
            //    microsoftOptions.ClientSecret = Configuration["Authentication:Microsoft:ClientSecret"];
            //    microsoftOptions.SignInScheme = IdentityConstants.ExternalScheme;
            //});

            // رفع محدودیت ارسال داده در فرم های ایجکس
            services.Configure<FormOptions>(options =>
            {
                options.ValueCountLimit = int.MaxValue;
            });
            // -- Load Files
            // If using Kestrel:
            services.Configure<KestrelServerOptions>(options =>
            {
                options.AllowSynchronousIO = true;
            });
            // If using IIS:
            services.Configure<IISServerOptions>(options =>
            {
                options.AllowSynchronousIO = true;
            });
            services.Configure<IdentityOptions>(options =>
            {
                // Password settings.
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequiredLength = 6;
                options.Password.RequiredUniqueChars = 0;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromDays(30);
            });

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
            });
            services.ConfigureApplicationCookie(options =>
            {
                // Cookie settings
                options.Cookie.HttpOnly = true;
                options.Cookie.Name = ".AdventureWork";
                options.ExpireTimeSpan = TimeSpan.FromDays(60);
                // If the LoginPath isn't set, ASP.NET Core defaults 
                // the path to /Account/Login.
                options.LoginPath = "/Account/Login";
                // If the AccessDeniedPath isn't set, ASP.NET Core defaults 
                // the path to /Account/AccessDenied.
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.SlidingExpiration = true;
                //options.Cookie.SameSite= Microsoft.AspNetCore.Http.SameSiteMode.None;
            });

            services.AddTransient<IEmailSender, EmailSender>();
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                    builder =>
                    {
                        builder
                        .AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                        //.AllowCredentials();
                    });
            });
            services.Configure<IISOptions>(options =>
            {
                options.ForwardClientCertificate = false;
            });
            services.Configure<FormOptions>(options =>
            {
                options.ValueLengthLimit = int.MaxValue;
                options.MultipartBodyLengthLimit = int.MaxValue;
                options.MemoryBufferThreshold = int.MaxValue;
            });
            IFileProvider physicalProvider = new PhysicalFileProvider(Directory.GetCurrentDirectory());

            services.AddSingleton<IFileProvider>(physicalProvider);

            services.Configure<GzipCompressionProviderOptions>
            (options => options.Level = CompressionLevel.Optimal);
            services.AddResponseCompression(options =>
            {
                options.Providers.Add<GzipCompressionProvider>();
            });
            // Add browser detection service
            services.AddBrowserDetection();
            services.AddMvc(option => option.EnableEndpointRouting = false);
            services.AddDistributedMemoryCache();
            services.AddResponseCaching();
            services.AddControllersWithViews().AddRazorRuntimeCompilation();
            services.Configure<IISServerOptions>(options =>
            {
                options.AutomaticAuthentication = false;
            });
            services.Configure<IISOptions>(options =>
            {
                options.ForwardClientCertificate = false;
            });
            services.AddSingleton<HtmlEncoder>(
            HtmlEncoder.Create(allowedRanges: new[] { UnicodeRanges.BasicLatin,
                                            UnicodeRanges.Arabic }));

            // اجرای زمانبندی کد ها
            services.AddHostedService<TimedHostedService>();
            //
            services.AddSignalR();
            services.AddControllers().AddNewtonsoftJson(options =>
            options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
            );

            services.AddParbad()
                .ConfigureGateways(gateways =>
                {
                    gateways
                        .AddMelli()
                        .WithAccounts(accounts =>
                        {
                            accounts.Add<MelliAccountSource>(ServiceLifetime.Transient);
                        });
                    gateways
                       .AddMellat()
                       .WithAccounts(accounts =>
                       {
                           accounts.Add<MellatAccountSource>(ServiceLifetime.Transient);
                       });
                    gateways
                         .AddParbadVirtual()
                         .WithOptions(options => options.GatewayPath = "/MyVirtualGateway");
                })
                .ConfigureHttpContext(builder => builder.UseDefaultAspNetCore())
                .ConfigureStorage(builder =>
                {
                    builder.UseEfCore(options =>
                    {
                        // Using SQL Server
                        var assemblyName = typeof(Startup).Assembly.GetName().Name;
                        IConfigurationRoot configuration = new ConfigurationBuilder()
                        .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                        .AddJsonFile("appsettings.json")
                        .Build();
                        //options.ConfigureDbContext = db => db.UseSqlServer("Server=.;DataBase=ArmisDB;Initial Catalog=ArmisDB;Trusted_Connection=True;", sql => sql.MigrationsAssembly(assemblyName));
                        options.ConfigureDbContext = db => db.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), sql => sql.MigrationsAssembly(assemblyName));
                    });
                });


            services.AddSession(options =>
            {
                // Set a short timeout for easy testing.
                options.IdleTimeout = TimeSpan.FromHours(5);
                options.Cookie.HttpOnly = true;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();

            }
            else
            {
                app.UseExceptionHandler("/Error");
                //app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseSession();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();


            app.UseParbadVirtualGateway();
            //var options = new RewriteOptions();
            //options.Rules.Add(new NonWwwRule());
            //app.UseRewriter(options);

            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = ctx =>
                {
                    //const int durationInSeconds = 60 * 60 * 24;
                    const int durationInSeconds = 31536000;
                    ctx.Context.Response.Headers[HeaderNames.CacheControl] =
                        "public,max-age=" + durationInSeconds;

                    string Path = ctx.Context.Request.Path;
                    if (Path.ToLower().StartsWith("/cdn/courses") || Path.ToLower().StartsWith("/cdn/files"))
                    {
                        UserRepository rep = new UserRepository();
                        var IPAddress = rep.GetClientIP(ctx.Context);

                        string UserName = ctx.Context.Request.Query["u"];

                        if (UserName != null)
                        {
                            Encoding encoding = Encoding.GetEncoding("iso-8859-1");
                            string dUserPass = encoding.GetString(Convert.FromBase64String(UserName));
                            string[] spliteVal = dUserPass.Split("&");

                            //int seperatorIndex = dUserPass.IndexOf('&');
                            //string username = dUserPass.Substring(0, seperatorIndex);
                            //string password = dUserPass.Substring(seperatorIndex + 1);
                            string username = spliteVal[0];
                            string password = spliteVal[1];
                            if (!password.Contains("."))
                            {
                                var DecryptIP = password.ConvertNumeral();
                                if (DecryptIP == IPAddress)
                                {
                                    return;
                                }
                            }
                        }
                        ctx.Context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        //ctx.Context.Response.ContentLength = 0;
                        //ctx.Context.Response.Body = Stream.Null;
                        ctx.Context.Response.Redirect("/AccessDenied");
                    }
                }
            });
            //app.UseWebMarkupMin();
            app.UseResponseCaching();
            app.UseCors("CorsPolicy");
            app.UseStatusCodePagesWithReExecute("/Error/{0}");
            app.UseAuthentication();

            app.UseCookiePolicy(new CookiePolicyOptions()
            {
                MinimumSameSitePolicy = Microsoft.AspNetCore.Http.SameSiteMode.Lax
            });
            app.UseMvcWithDefaultRoute();
            app.UseResponseCompression();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapHub<ChatHub>("/chatHub");
            });
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

            app.UseMvc(Route => Route.MapRoute(
                name: "AdvanceRoteHome",
                template: "{Action}/{id?}",
                defaults: new { controller = "Home", Action = "Index" }
                ));
            app.UseMvc(Route => Route.MapRoute(
                name: "dl",
                template: "dl/{type}/{id}/{token:guid}",
                defaults: new { controller = "Home", Action = "dl" }
                ));
            app.UseMvc(Route => Route.MapRoute(
                name: "Blog",
                template: "Blog/{type?}/{title?}",
                defaults: new { controller = "Home", Action = "Blog" }
                ));
            app.UseMvc(Route => Route.MapRoute(
                name: "Profile",
                template: "Profile/{userName?}",
                defaults: new { controller = "Student", Action = "Profile" }
                ));
            app.UseMvc(Route => Route.MapRoute(
                name: "Settings",
                template: "Settings",
                defaults: new { controller = "Student", Action = "Settings" }
                ));
            app.UseMvc(Route => Route.MapRoute(
                name: "Terms",
                template: "Terms/{userName?}",
                    defaults: new { controller = "Student", Action = "Terms" }
                ));
        }
    }
}
