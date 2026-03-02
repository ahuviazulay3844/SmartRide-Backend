using Common.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface ICouponService : IService<CouponDto>
    {
        bool MarkAsUsed(int userId, string couponCode);
        bool IsCouponValid(string code, int userId, decimal currentOrderAmount);
        IEnumerable<CouponDto> GetUnusedCouponsByUserId(int userId);
        IEnumerable<CouponDto> GetExpiringSoon(int days);
        decimal ApplyDiscount(string code, decimal originalAmount, int userId);
    }
}
