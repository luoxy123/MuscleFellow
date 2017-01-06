using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MuscleFellow.Data;
using MuscleFellow.Data.Interfaces;
using MuscleFellow.Data.Repositories;
using MuscleFellow.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MuscleFellow.API.JWT;
using Microsoft.Extensions.Options;

namespace MuscleFellow.API
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Dependencies Injection
            AddDependencies(services);
            // 读取appSettings.json 的配置信息
            services.Configure<WebApiSettings>(settings => settings.HostName = Configuration["HostName"]);
            services.Configure<WebApiSettings>(settings => settings.SecretKey = Configuration["SecretKey"]);
            services.Configure<WebApiSettings>(settings => settings.ServiceUrl = Configuration["ServiceUrl"]);
            services.Configure<IdentityOptions>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                
                // Lockout settings
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
                options.Lockout.MaxFailedAccessAttempts = 10;

                // Cookie settings
                options.Cookies.ApplicationCookie.ExpireTimeSpan = TimeSpan.FromDays(150);
                //options.Cookies.ApplicationCookie.LoginPath = "/Account/LogIn";
                //options.Cookies.ApplicationCookie.LogoutPath = "/Account/LogOff";
                                // User settings
                options.User.RequireUniqueEmail = true;
                
            });



            services.AddEntityFramework()
                .AddDbContext<MuscleFellowDbContext>(options=>options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));



            // Add framework services.
            //services.AddDbContext<MuscleFellowDbContext>(options =>
            //    options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<MuscleFellowDbContext>()
                .AddDefaultTokenProviders();

            // Add framework services.
            services.AddMvc();

            // Add session
            services.AddSession(options => options.IdleTimeout = TimeSpan.FromMinutes(20));
            
            
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            

            //Add ASP.NET Core Identity
            //app.UseIdentity().UseCookieAuthentication(
            //    new CookieAuthenticationOptions()
            //    {
            //        AuthenticationScheme = "Cookie",
            //        AutomaticAuthenticate = false
            //        //AccessDeniedPath = new PathString("/api/v1/Account/Login"),
            //        //LoginPath = new PathString("/api/v1/Account/Login")
                    
            //    });

            app.UseIdentity().UseCookieAuthentication(new CookieAuthenticationOptions()
            {
                AuthenticationScheme="Cookie",
                AutomaticAuthenticate = false,
                LoginPath = new PathString("/api/v1/Account/Login")
                

            });

            // Add JWT　Protection
            var secretKey = Configuration["SecretKey"];
            var signingKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey));
            var tokenValidationParameters = new TokenValidationParameters
            {
                // The signing key must match! 
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingKey,
                // Validate the JWT Issuer (iss) claim 
                ValidateIssuer = true,
                ValidIssuer = "MuscleFellow",
                // Validate the JWT Audience (aud) claim 
                ValidateAudience = true,
                ValidAudience = "MuscleFellowAudience",
                // Validate the token expiry 
                ValidateLifetime = true,
                // If you want to allow a certain amount of clock drift, set that here: 
                ClockSkew = TimeSpan.Zero
            };


            app.UseJwtBearerAuthentication(new JwtBearerOptions
            {
                AuthenticationScheme = "Bearer",
                AutomaticAuthenticate = false,
                AutomaticChallenge = false,
                TokenValidationParameters = tokenValidationParameters,
                IncludeErrorDetails=true
            });

            

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "api/{controller=values}/{action=get}/{id?}");
            });

            app.UseStaticFiles();
        }
        public IServiceCollection AddDependencies(IServiceCollection services)
        {
            services.AddScoped<IBrandRepository, BrandRepository>();
            services.AddScoped<ICartItemRepository, CartItemRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<IOrderDetailRepository, OrderDetailRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IProductImageRepository, ProductImageRepository>();
            services.AddScoped<IShipAddressRepository, ShipAddressRepository>();
            services.AddScoped<MuscleFellowDbContext>();
            return services;
        }
    }
}
