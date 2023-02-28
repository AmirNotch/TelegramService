using Domain;
using Microsoft.EntityFrameworkCore;
using Persistence;
using TelegramService.IServices;

namespace TelegramService.Services
{
    public class AuthRepository : IAuthRepository
    {
        private DataContext _authContext;
        
        public AuthRepository(DataContext authContext)
        {
            _authContext = authContext;
        }

        public Token GetToken(long phoneNumber)
        {
            return _authContext.Tokens.Where(t => t.PhoneNumber == phoneNumber).FirstOrDefault();
        }

        public bool TokenExists(long phone)
        {
            return _authContext.Tokens.Any(t => t.PhoneNumber == phone);
        }

        public bool TokenExists(int api_id, string api_hash)
        {
            return _authContext.Tokens.Any(t => t.API_ID == api_id && t.API_HASH == api_hash);
        }

        public bool CreateToken(Token token)
        {
            _authContext.AddAsync(token);
            return Save();
        }

        public bool Save()
        {
            var saved = _authContext.SaveChanges();
            return saved >= 0 ? true : false;
        }
    }    
}

