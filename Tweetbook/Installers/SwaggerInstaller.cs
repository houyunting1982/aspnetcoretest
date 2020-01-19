using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;

namespace Tweetbook.Installers
{
    public class SwaggerInstaller : IInstaller
    {
        public void InstallServices(IServiceCollection services, IConfiguration configuration) {
            // The doc name must be staying as same as the Version
            services.AddSwaggerGen(x => {
                x.SwaggerDoc("v1", new OpenApiInfo {
                    Title = "Tweetbook API",
                    Version = "v1"
                });
                x.ExampleFilters();
                //x.AddSecurityDefinition("Bearer", new ApiKeyScheme {
                x.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme {
                    Description = "JWT Authorization header using the bearer scheme",
                    Name = "Authorization",
                    // In = "header",
                    In = ParameterLocation.Header,
                    // Type = "apiKey"
                    Type = SecuritySchemeType.ApiKey
                });
                //x.AddSecurityRequirement(security);
                x.AddSecurityRequirement(new OpenApiSecurityRequirement {
                    {
                        new OpenApiSecurityScheme {
                            Reference = new OpenApiReference {
                                Id = "Bearer",
                                Type = ReferenceType.SecurityScheme
                            }
                        }, new List<string>()
                    }
                });

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                x.IncludeXmlComments(xmlPath);
            });

            services.AddSwaggerExamplesFromAssemblyOf<Startup>();
        }
    }
}