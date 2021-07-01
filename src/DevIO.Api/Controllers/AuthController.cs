using System.Threading.Tasks;
using DevIO.Api.ViewModels;
using DevIO.Business.Intefaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DevIO.Api.Controllers
{
    [Route("api")]
    public class AuthController : MainController
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;

        public AuthController(
            INotificador notificador, 
            SignInManager<IdentityUser> signInManager, 
            UserManager<IdentityUser> userManager
        ) : base(notificador)
        {
            _signInManager = signInManager;
            _userManager = userManager;
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
            if ( result.Succeeded)
            {
                await _signInManager.SignInAsync(identityUser, false); // false = persisteente. Se vai lembrar dele no próximo login
                return CustomResponse(registerUserViewModel);
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
                return CustomResponse(loginUserViewModel);            
            if (result.IsLockedOut)
            {
                NotificarErro("Usuário temporariamente bloqueado por tentativas inválidas");
                return CustomResponse(loginUserViewModel);
            }

            NotificarErro("Usuário ou senha incorretos");
            return CustomResponse(loginUserViewModel);
        }
    }
}