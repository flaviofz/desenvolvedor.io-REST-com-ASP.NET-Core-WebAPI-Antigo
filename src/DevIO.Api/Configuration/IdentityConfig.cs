using System.Text;
using DevIO.Api.Data;
using DevIO.Api.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace DevIO.Api.Configuration
{
    public static class IdentityConfig
    {
        public static IServiceCollection AddIdentityConfiguration(
            this IServiceCollection services, 
            IConfiguration configuration
        )
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            services.AddDefaultIdentity<IdentityUser>()
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddErrorDescriber<IdentityMensagensPortugues>() // Adicionando a tradução para os erros do identity
                .AddDefaultTokenProviders(); // Token para reset de senha ou identificação por e-mail

            // JWT
            var appSettingsSection = configuration.GetSection("AppSettings");
            services.Configure<AppSettings>(appSettingsSection); // Faz como se fosse um bind da classe para o trecho do appsetings.json

            var appSettings = appSettingsSection.Get<AppSettings>(); // Transformando no objeto
            var key = Encoding.ASCII.GetBytes(appSettings.Segredo);

            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme; // Sempre deve ser gerado um token
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme; // Esse token que deve ser checado para validação
            }).AddJwtBearer(x => 
            {
                x.RequireHttpsMetadata = true; // Se você só trabalha com HTTPS
                x.SaveToken = true; // Se o token deve ser guardado no httpAuthenticationProperties após autenticação válida
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true, // Quem está validando deve ser o mesmo que emitiu o token
                    IssuerSigningKey = new SymmetricSecurityKey(key), // Configuração da chave
                    ValidateIssuer = true, // Valida o Issuer pelo nome
                    ValidateAudience = true, // Onde seu token é valido?
                    ValidAudience = appSettings.ValidoEm, 
                    ValidIssuer = appSettings.Emissor
                };
            });

            return services;
        }
    }
}