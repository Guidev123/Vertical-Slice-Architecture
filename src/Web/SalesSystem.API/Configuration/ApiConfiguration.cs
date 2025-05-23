﻿using MidR.DependencyInjection;
using SalesSystem.API.Middlewares;
using SalesSystem.Email;
using SalesSystem.Email.Models;
using SalesSystem.EventSourcing;
using SalesSystem.Payments.ACL.Configurations;
using SalesSystem.SharedKernel.Abstractions.Mediator;
using SalesSystem.SharedKernel.Notifications;
using SendGrid.Extensions.DependencyInjection;
using System.Reflection;

namespace SalesSystem.API.Configuration
{
    public static class ApiConfiguration
    {
        public const int DEFAULT_PAGE_NUMBER = 1;
        public const int DEFAULT_PAGE_SIZE = 15;

        private static readonly string[] _assemblies =
            ["SalesSystem.Payments.Application",
            "SalesSystem.Sales.Application",
            "SalesSystem.Catalog.Application",
            "SalesSystem.Registers.Application"
            ];

        public static void AddConfigurations(this WebApplicationBuilder builder)
        {
            builder.AddCorsConfig();
            builder.AddConfigureMediator();
            builder.AddCustomMiddlewares();
            builder.AddEmailServices();
            builder.AddHandlers();
            builder.AddModelConfig();
            builder.AddNotifications();
            builder.Services.AddEventStoreConfiguration();
            builder.Services.AddJwtConfiguration(builder.Configuration);
            builder.Services.AddSwaggerConfig();
            builder.Services.AddHttpContextAccessor();
        }

        public static void AddConfigureMediator(this WebApplicationBuilder builder)
            => builder.Services.AddScoped<IMediatorHandler, MediatorHandler>();

        public static void AddEmailServices(this WebApplicationBuilder builder)
        {
            builder.Services.AddSendGrid(x =>
            {
                x.ApiKey = builder.Configuration.GetValue<string>("EmailSettings:ApiKey");
            });
            builder.Services.AddScoped<IEmailService, EmailService>();
        }

        public static void AddHandlers(this WebApplicationBuilder builder)
        {
            foreach (var assembly in _assemblies)
            {
                Assembly.Load(assembly);
            }

            builder.Services.AddMidR(_assemblies);
        }

        public static void AddCustomMiddlewares(this WebApplicationBuilder builder)
        {
            builder.Services.AddTransient<GlobalExceptionMiddleware>();
        }

        public static void AddModelConfig(this WebApplicationBuilder builder)
        {
            builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection(nameof(StripeSettings)));
            builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection(nameof(EmailSettings)));
        }

        public static void AddNotifications(this WebApplicationBuilder builder)
            => builder.Services.AddScoped<INotificator, Notificator>();

        public static void AddCorsConfig(this WebApplicationBuilder builder)
        {
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("Total", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });
        }

        public static void AddApiUsing(this WebApplication app)
        {
            app.UseSwaggerConfig();
            app.UseMiddleware<GlobalExceptionMiddleware>();
            app.UseHttpsRedirection();

            app.UseCors("Total");

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
        }
    }
}