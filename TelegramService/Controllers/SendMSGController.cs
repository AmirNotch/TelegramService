using System.Net;
using System.Net.Http.Headers;
using Domain;
using Microsoft.AspNetCore.Mvc;
using TelegramService.IServices;
using TL;

namespace TelegramService.Controllers;
[ApiController]
[Route("[controller]")]
public class SendMSGController : Controller
{
    private IAuthRepository _authRepository;
    
    public SendMSGController(IAuthRepository authRepository)
    {
        _authRepository = authRepository;
    }
    
    // POST
    [HttpPost("sendTextMsgToPhone/{fromPhoneNumber}/{toPhoneNumber}")]
    public async Task<IActionResult> SendTextMSG([FromRoute]long fromPhoneNumber, long toPhoneNumber, [FromBody]MessageText textMSG)
    {
        // if (!_authRepository.TokenExists(fromPhoneNumber))
        // {
        //     return NotFound();
        // }
        
        var token = _authRepository.GetToken(fromPhoneNumber);
        
        if (token == null)
        {
            return NotFound();
        }
        
        using (WTelegram.Client client = new WTelegram.Client(token.API_ID, token.API_HASH)) // this constructor doesn't need a Config method
        {
            await DoLogin("+"+fromPhoneNumber); // initial call with user's phone_number
            async Task DoLogin(string loginInfo) // (add this method to your code)
            {
                while (client.User == null)
                    switch (await client.Login(loginInfo)) // returns which config is needed to continue login
                    {
                        //case "verification_code": Console.Write("Code: "); loginInfo = Console.ReadLine(); break;
                        case "name": loginInfo = "John Doe"; break;    // if sign-up is required (first/last_name)
                        case "password": loginInfo = "secret!"; break; // if user has enabled 2FA
                        default: loginInfo = null; break;
                    }
                Console.WriteLine($"Good");
            }
            
            var result = await client.Contacts_ResolvePhone("+"+toPhoneNumber);
            await client.SendMessageAsync(result.User, textMSG.Text);
        }

        return Ok();
    }
    
    // POST
    [HttpPost("sendTextMsgToUserName/{fromPhoneNumber}/{userName}")]
    public async Task<IActionResult> SendTextMSG([FromRoute]long fromPhoneNumber, string userName, [FromBody]MessageText textMSG)
    {
        // if (!_authRepository.TokenExists(fromPhoneNumber))
        // {
        //     return NotFound();
        // }
        
        var token = _authRepository.GetToken(fromPhoneNumber);
        
        if (token == null)
        {
            return NotFound();
        }
        using (WTelegram.Client client = new WTelegram.Client(token.API_ID, token.API_HASH)) // this constructor doesn't need a Config method
        {
            await DoLogin("+"+fromPhoneNumber); // initial call with user's phone_number
            async Task DoLogin(string loginInfo) // (add this method to your code)
            {
                while (client.User == null)
                    switch (await client.Login(loginInfo)) // returns which config is needed to continue login
                    {
                        //case "verification_code": Console.Write("Code: "); loginInfo = Console.ReadLine(); break;
                        case "name": loginInfo = "John Doe"; break;    // if sign-up is required (first/last_name)
                        case "password": loginInfo = "secret!"; break; // if user has enabled 2FA
                        default: loginInfo = null; break;
                    }
                Console.WriteLine($"Good");
            }
            
            var result = await client.Contacts_ResolveUsername(userName);
            await client.SendMessageAsync(result.User, textMSG.Text);
        }

        return Ok();
    }
    
    [HttpPost("sendMediaMsgToPhone/{fromPhoneNumber}/{toPhoneNumber}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadFile(IFormFile file, CancellationToken cancellationToken,
        [FromRoute]long fromPhoneNumber, long toPhoneNumber, string text = "")
    {
        var token = _authRepository.GetToken(fromPhoneNumber);
        
        if (token == null)
        {
            return NotFound();
        }
        
        await WriteFile(file, fromPhoneNumber, toPhoneNumber, token, text);

        return Ok();
    }

    private async Task<bool> WriteFile(IFormFile file, long fromPhoneNumber, long toPhoneNumber, Token token, string textMSG)
    {
        bool isSaveSuccess = false;
        string fileName;
        try
        {
            var extension = "." + file.FileName.Split('.')[file.FileName.Split('.').Length - 1];
            fileName = DateTime.Now.Ticks + extension;

            var pathBuilt = Path.Combine(Directory.GetCurrentDirectory(), "Upload\\files");

            if (!Directory.Exists(pathBuilt))
            {
                Directory.CreateDirectory(pathBuilt);
            }
            
            var path = Path.Combine(Directory.GetCurrentDirectory(), "Upload\\files",
                fileName);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            
            using (WTelegram.Client client = new WTelegram.Client(token.API_ID, token.API_HASH)) // this constructor doesn't need a Config method
            {
                await DoLogin("+"+fromPhoneNumber); // initial call with user's phone_number
                async Task DoLogin(string loginInfo) // (add this method to your code)
                {
                    while (client.User == null)
                        switch (await client.Login(loginInfo)) // returns which config is needed to continue login
                        {
                            //case "verification_code": Console.Write("Code: "); loginInfo = Console.ReadLine(); break;
                            case "name": loginInfo = "John Doe"; break;    // if sign-up is required (first/last_name)
                            case "password": loginInfo = "secret!"; break; // if user has enabled 2FA
                            default: loginInfo = null; break;
                        }
                    Console.WriteLine($"Good");
                }

                if (textMSG.Length != 0)
                {
                    var inputFile = await client.UploadFileAsync(path);
                    var result = await client.Contacts_ResolvePhone("+"+toPhoneNumber);
                    await client.SendMediaAsync(result, textMSG, inputFile);
                }
                else
                {
                    var inputFile = await client.UploadFileAsync(path);
                    var result = await client.Contacts_ResolvePhone("+"+toPhoneNumber);
                    await client.SendMediaAsync(result, "", inputFile);
                }
            }

            isSaveSuccess = true;
        }
        catch (Exception e)
        {
            // log error
        }

        return isSaveSuccess;
    }
    
/// <summary>
/// 
/// </summary>
/// <param name="file"></param>
/// <param name="cancellationToken"></param>
/// <param name="fromPhoneNumber"></param>
/// <param name="userName"></param>
/// <param name="text"></param>
/// <returns></returns>
    [HttpPost("sendMediaMsgToUserName/{fromPhoneNumber}/{userName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadFile(IFormFile file, CancellationToken cancellationToken,
        [FromRoute]long fromPhoneNumber, string userName, string text = "")
    {
        var token = _authRepository.GetToken(fromPhoneNumber);
        
        if (token == null)
        {
            return NotFound();
        }
        
        await WriteFile(file, fromPhoneNumber, userName, token, text);

        return Ok();
    }

    private async Task<bool> WriteFile(IFormFile file, long fromPhoneNumber, string userName, Token token, string textMSG)
    {
        bool isSaveSuccess = false;
        string fileName;
        try
        {
            var extension = "." + file.FileName.Split('.')[file.FileName.Split('.').Length - 1];
            fileName = DateTime.Now.Ticks + extension;

            var pathBuilt = Path.Combine(Directory.GetCurrentDirectory(), "Upload\\files");

            if (!Directory.Exists(pathBuilt))
            {
                Directory.CreateDirectory(pathBuilt);
            }
            
            var path = Path.Combine(Directory.GetCurrentDirectory(), "Upload\\files",
                fileName);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            
            using (WTelegram.Client client = new WTelegram.Client(token.API_ID, token.API_HASH)) // this constructor doesn't need a Config method
            {
                await DoLogin("+"+fromPhoneNumber); // initial call with user's phone_number
                async Task DoLogin(string loginInfo) // (add this method to your code)
                {
                    while (client.User == null)
                        switch (await client.Login(loginInfo)) // returns which config is needed to continue login
                        {
                            //case "verification_code": Console.Write("Code: "); loginInfo = Console.ReadLine(); break;
                            case "name": loginInfo = "John Doe"; break;    // if sign-up is required (first/last_name)
                            case "password": loginInfo = "secret!"; break; // if user has enabled 2FA
                            default: loginInfo = null; break;
                        }
                    Console.WriteLine($"Good");
                }

                if (textMSG.Length != 0)
                {
                    var inputFile = await client.UploadFileAsync(path);
                    var result = await client.Contacts_ResolveUsername(userName);
                    await client.SendMediaAsync(result, textMSG, inputFile);
                }
                else
                {
                    var inputFile = await client.UploadFileAsync(path);
                    var result = await client.Contacts_ResolveUsername(userName);
                    await client.SendMediaAsync(result, "", inputFile);
                }
            }

            isSaveSuccess = true;
        }
        catch (Exception e)
        {
            // log error
        }

        return isSaveSuccess;
    } 
}