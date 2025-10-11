using System;
using System.Threading.Tasks;
using EventPl.Dto;

namespace EventPl.Services.Interface
{
    public interface IAuthService
    {
        /// <summary>
        /// تسجيل دخول بالـ Email (للـ Admin) أو Phone (للـ User)
        /// </summary>
        Task<UserDto?> LoginByIdentifierAsync(string identifier);
    }
}
