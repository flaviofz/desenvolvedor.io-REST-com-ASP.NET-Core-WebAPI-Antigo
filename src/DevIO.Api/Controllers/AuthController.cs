using System;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.Tasks;
using DevIO.Api.Extensions;
using DevIO.Api.ViewModels;
using DevIO.Business.Intefaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DevIO.Api.Controllers
{
    [Route("api")]
    public class AuthController : MainController
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly AppSettings _appSettings;

        public AuthController(
            INotificador notificador, 
            SignInManager<IdentityUser> signInManager, 
            UserManager<IdentityUser> userManager,
            IOptions<AppSettings> appSettings // Server para pegar dados que servem como parâmetros
        ) : base(notificador)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _appSettings = appSettings.Value;
        }

        [HttpPost("nova-conta")]
        public async Task<ActionResult> Registrar(
            RegisterUserViewModel registerUserViewModel
        )
        {
            if (!ModelState.IsValid) return CustomResponse(ModelState);

            var identityUser = new IdentityUser // Senha não é colocada no IdentityUser
            {
                UserName = registerUserViewModel.Email,
                Email = registerUserViewModel.Email,
                EmailConfirmed = true // E-mail pré-validado
            };

            var result = await _userManager.CreateAsync(
                identityUser, 
                registerUserViewModel.ConfirmPassword // é criptografado sempre | mandar de forma separada
            );
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(identityUser, false); // false = persisteente. Se vai lembrar dele no próximo login
                return CustomResponse(GerarJwt());
            }

            foreach (var error in result.Errors)            
                NotificarErro(error.Description);            

            return CustomResponse(registerUserViewModel);
        }

        [HttpPost("login")]
        public async Task<ActionResult> Entrar(
            LoginUserViewModel loginUserViewModel
        )
        {
            if (!ModelState.IsValid) return CustomResponse(ModelState);

            var result = await _signInManager.PasswordSignInAsync(
                loginUserViewModel.Email,
                loginUserViewModel.Password,
                isPersistent: false,
                lockoutOnFailure: true // Tentando mais de 5x com informações erradas o usuário será travado e só é liberado depois de X minutos
            );

            if (result.Succeeded)            
                return CustomResponse(GerarJwt());            
            if (result.IsLockedOut)
            {
                NotificarErro("Usuário temporariamente bloqueado por tentativas inválidas");
                return CustomResponse(loginUserViewModel);
            }

            NotificarErro("Usuário ou senha incorretos");
            return CustomResponse(loginUserViewModel);
        }

        private string GerarJwt()
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Segredo);
            var token = tokenHandler.CreateToken(new SecurityTokenDescriptor
            {
                Issuer = _appSettings.Emissor,
                Audience = _appSettings.ValidoEm,
                Expires = DateTime.UtcNow.AddHours(_appSettings.ExpiracaoHoras),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            });

            var encodedToken = tokenHandler.WriteToken(token); // Para ficar compatível com o padrão da WEb
            return encodedToken;
        }
    }
}