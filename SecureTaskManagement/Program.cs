using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SecureTaskManagement.Data;
using SecureTaskManagement.Hubs;
using SecureTaskManagement.Services;
using System.Text;


namespace SecureTaskManagement
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
             
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
            
            var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
                            ?? builder.Configuration["JwtSettings:Secret"]!;
             
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
                        ValidAudience = builder.Configuration["JwtSettings:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];
                            var path = context.HttpContext.Request.Path;

                            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/tasks"))
                            {
                                context.Token = accessToken;
                            }
                            return Task.CompletedTask;
                        }
                    };
        });
             

            builder.Services.AddAuthorization();
            builder.Services.AddScoped<TokenService>();
            builder.Services.AddSignalR();             
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
 
          
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "SecureTaskManagement API", Version = "v1" });

                var bearerScheme = new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter 'Bearer' [space] and then your token.",
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                };

                c.AddSecurityDefinition("Bearer", bearerScheme);

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { bearerScheme, Array.Empty<string>() }
    });
            });

         
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                    policy
                        .WithOrigins("http://localhost:4200")  
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials());
            });


            var app = builder.Build();
             
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.Migrate();
            }
             
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
             
            app.UseCors();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseHttpsRedirection();
            app.MapControllers();
            app.MapHub<TaskHub>("/hubs/tasks");
            app.Run();
        }
    }
}