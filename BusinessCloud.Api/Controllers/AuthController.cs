using BusinessCloud.Shared.Responses;
using Microsoft.AspNetCore.Mvc;
using  BusinessCloud.Application.Auth.Interfaces;



    namespace BusinessCloud.Api.Controllers
{
 

    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _authService.LoginAsync(request);

            return Ok(new ApiResponse<LoginResponse>
            {
                Success = true,
                Data = result
            });
        }
    }

}
