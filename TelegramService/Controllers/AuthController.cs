using System.Reflection;
using Domain;
using Microsoft.AspNetCore.Mvc;
using TelegramService.DTOs;
using TelegramService.IServices;
using System.Linq;
using System.IO;

namespace TelegramService.Controllers;
[ApiController]
[Route("[controller]")]
public class AuthController : Controller
{
    private IAuthRepository _authRepository;
    
    public AuthController(IAuthRepository authRepository)
    {
        _authRepository = authRepository;
    }
    
    [HttpPost("register/{phoneNumber}")]
    [ProducesResponseType(204)] // no content
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    [ProducesResponseType(200, Type = typeof(Token))]
    public async Task<IActionResult> CreateTelegram([FromRoute]long phoneNumber,[FromBody]TokenDTOs tokenDtOs)
    {
        if(_authRepository.TokenExists(tokenDtOs.API_ID, tokenDtOs.API_HASH))
        {
            return BadRequest("Token exist please choose another!");
        }

        using (WTelegram.Client
               client = new WTelegram.Client(tokenDtOs.API_ID,
                   tokenDtOs.API_HASH)) // this constructor doesn't need a Config method
        {
            try
            {
                await DoLoginNow(phoneNumber.ToString());

                async Task DoLoginNow(string loginInfo) // (add this method to your code)
                {
                    while (client.User == null)
                        switch (await client.Login(loginInfo)) // returns which config is needed to continue login
                        {
                            case "verification_code":
                                Console.Write("Code: ");
                                loginInfo = "";
                                break;
                            case "name":
                                loginInfo = "John Doe";
                                break; // if sign-up is required (first/last_name)
                            case "password":
                                loginInfo = "secret!";
                                break; // if user has enabled 2FA
                            default:
                                loginInfo = null;
                                break;
                        }

                    Console.WriteLine($"We are logged-in as {client.User} (id {client.User.id})");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        var token = new Token()
        {
            API_ID = tokenDtOs.API_ID,
            API_HASH = tokenDtOs.API_HASH,
            PhoneNumber = phoneNumber
        };
        
        if (!_authRepository.CreateToken(token))
        {
            ModelState.AddModelError("", $"Something went wrong saving the token " + $"{tokenDtOs.API_ID} and {tokenDtOs.API_HASH}");
            return StatusCode(500, ModelState);
        }

        return Ok(_authRepository.GetToken(phoneNumber));
    }

    [HttpPost("verify/{verificationCode}/{phoneNumber}")]
    public async Task<IActionResult> VerificateToken([FromRoute]string verificationCode, long phoneNumber)
    {
        if (!_authRepository.TokenExists(phoneNumber))
        {
            return NotFound();
        }

        var token = _authRepository.GetToken(phoneNumber);

        using (WTelegram.Client client = new WTelegram.Client(token.API_ID, token.API_HASH)) // this constructor doesn't need a Config method
        {
            
            await DoLogin(token.PhoneNumber.ToString(), client, verificationCode); // initial call with user's phone_number
        }

        return Ok(token);
    }

    [HttpPost("login/{phoneNumber}")]
    public async Task<IActionResult> Login([FromRoute]long phoneNumber)
    {
        if (!_authRepository.TokenExists(phoneNumber))
        {
            return NotFound();
        }
        
        var token = _authRepository.GetToken(phoneNumber);
        
        
        using (WTelegram.Client client = new WTelegram.Client(token.API_ID, token.API_HASH)) // this constructor doesn't need a Config method)
        {
            await client.Login(phoneNumber.ToString()); // initial call with user's phone_number
        }

        return Ok(token);
    }

    [HttpPost("LogOutSession")]
    public async Task<IActionResult> LogOut()
    {
        string deleteFileSession = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        string path = deleteFileSession;
        path = path.Remove(path.LastIndexOf("\\"));
        path = path.Remove(path.LastIndexOf("\\"));

        if (System.IO.File.Exists(path + "\\WTelegram.session"))
        {
            System.IO.File.Delete(path + "\\WTelegram.session");
        }
        else
        {
            return BadRequest("You are not authorized");
        }

        return Ok();
    }

    private async Task DoLogin(string loginInfo, WTelegram.Client client, string verificationCode) // (add this method to your code)
    {
        while (client.User == null)
            switch (await client.Login(loginInfo)) // returns which config is needed to continue login
            {
                case "verification_code": Console.Write("Code: "); loginInfo = "+" + verificationCode; break;
                case "name": loginInfo = "John Doe"; break;    // if sign-up is required (first/last_name)
                case "password": loginInfo = "secret!"; break; // if user has enabled 2FA
                default: loginInfo = null; break;
            }
        Console.WriteLine($"We are logged-in as {client.User} (id {client.User.id})");
    }
}