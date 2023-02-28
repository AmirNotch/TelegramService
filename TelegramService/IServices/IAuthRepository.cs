using Domain;

namespace TelegramService.IServices
{
    public interface IAuthRepository
    {
        Token GetToken(long phoneNumber);
        
        bool TokenExists(long phone);
        bool TokenExists(int api_id, string api_hash);
        bool CreateToken(Token token);
        bool Save();
    }
}

