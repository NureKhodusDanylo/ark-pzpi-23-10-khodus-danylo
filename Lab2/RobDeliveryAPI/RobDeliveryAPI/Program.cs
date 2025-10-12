using Application.Abstractions.Interfaces;
using Application.Services;
using Entities.Config;
using Infrastructure;
using Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;

namespace RobDeliveryAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            var builder = WebApplication.CreateBuilder(args);

            Config config = new Config();
            builder.Configuration.Bind(config);
            builder.Services.AddSingleton(config);

            var connectionString = config.ConnectionStrings.DefaultConnection;

            builder.Services.AddDbContext<MyDbContext>(options =>
                options.UseSqlite(connectionString));

            builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IPasswordHasher, Sha256PasswordHasher>();
            builder.Services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();
            builder.Services.AddScoped<ITokenService, BaseTokenService>();

            builder.Services.AddControllers();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}