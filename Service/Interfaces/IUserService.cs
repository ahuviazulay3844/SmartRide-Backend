using Common.Dto;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
namespace Service.Interfaces
{
    public interface IUserService: IService<UserDto>
    {
        string Login(LoginDto loginDto);
        UserDto GetByEmail(string email);
        bool ChangePassword(int userId, string oldPassword, string newPassword);
        bool ToggleBlockUser(int userId);
        UserDto GetCurrentUser();
        int GetTotalUsersCount();
        bool ResetPassword(string email, string code, string newPassword);
        Task<bool> RequestPasswordReset(string email);


    }
}