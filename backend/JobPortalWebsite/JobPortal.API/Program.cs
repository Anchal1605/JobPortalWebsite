using JobPortal.API.Data;
using JobPortal.API.Middleware; //import the global exception middleware
using JobPortal.API.Options;
using JobPortal.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;  //adds jwt auth handler
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens; // provides token validation classes
using Microsoft.OpenApi.Models;
using System.Text; //convert string to bytes
namespace JobPortal.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //creates a web application builder object
            //this object is used to configure the web application
            //it contains services and configuration settings

            var builder = WebApplication.CreateBuilder(args);
            //tells asp.net core to use our AppDbContext for DB operations.
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            //reads jwt settings from appsettings.json
            var jwtSettings = builder.Configuration.GetSection("Jwt");
            var key = jwtSettings["Key"];


            //registers JwtBearer authentication service
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                //configures token validation parameters
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                };
            });


            // Add services to the container.
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AngularDev", builder =>
                {
                    builder.WithOrigins("http://localhost:4200")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
                });
            });

            builder.Services.Configure<FileStorageOptions>(
                builder.Configuration.GetSection(FileStorageOptions.SectionName));
            builder.Services.AddSingleton<FileStorageService>();
            builder.Services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 5 * 1024 * 1024;
            });

            builder.Services.AddControllers();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AngularDev", builder =>
                {
                    builder.WithOrigins("http://localhost:4200")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
                });
            });
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "JobPortal API", Version = "v1" });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter: Bearer {your token}"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                });
            });
            var app = builder.Build();

            var fileStorage = app.Services.GetRequiredService<FileStorageService>();
            fileStorage.EnsureUploadDirectoriesExist();

            app.UseStaticFiles(); // serves static files from the wwwroot folder
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            app.UseMiddleware<GlobalExceptionMiddleware>(); //use the global exception middleware
            app.UseHttpsRedirection();
            app.UseCors("AngularDev");
            app.UseAuthentication();   // reads token, build user identity
            app.UseAuthorization();    // checks if user has access to the resource


            app.MapControllers();

            app.Run();
        }
    }
}
