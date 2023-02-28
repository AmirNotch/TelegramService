﻿using Domain;
using Microsoft.AspNetCore.Mvc;
using TelegramService.DTOs;
using TelegramService.IServices;

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
    public async Task<IActionResult> CreateTelegram([FromRoute]long phoneNumber,[FromBody]TokenDTOs tokenDtOs)
    {
        if(_authRepository.TokenExists(tokenDtOs.API_ID, tokenDtOs.API_HASH))
        { 
            ModelState.AddModelError("", "Token exist please choose another!");
        }
        
        WTelegram.Client client = new WTelegram.Client(tokenDtOs.API_ID, tokenDtOs.API_HASH); // this constructor doesn't need a Config method
        await client.Login(phoneNumber.ToString());

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
    
        WTelegram.Client client = new WTelegram.Client(token.API_ID, token.API_HASH); // this constructor doesn't need a Config method

        await DoLogin(token.PhoneNumber.ToString(), client, verificationCode); // initial call with user's phone_number
        

        return Ok($"You successfully verified, you are logged-in as {client.User} (phone {client.User.phone})");
    }

    [HttpPost("login/{phoneNumber}")]
    public async Task<IActionResult> Login([FromRoute]long phoneNumber)
    {
        if (!_authRepository.TokenExists(phoneNumber))
        {
            return NotFound();
        }
        
        var token = _authRepository.GetToken(phoneNumber);
        
        WTelegram.Client client = new WTelegram.Client(token.API_ID, token.API_HASH); // this constructor doesn't need a Config method
        await client.Login(phoneNumber.ToString()); // initial call with user's phone_number
        
        
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