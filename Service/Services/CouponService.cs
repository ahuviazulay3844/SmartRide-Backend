using AutoMapper;
using Common.Dto;
using Repository.Entities;
using Repository.Interfaces;
using Repository.Repositories;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Azure.Core.HttpHeader;

namespace Service.Services
{
    public class CouponService : ICouponService
    {
        private readonly IRepository<Coupon> _couponRepository;
        private readonly IMapper _mapper;

        public CouponService(IRepository<Coupon> couponRepository, IMapper mapper)
        {
            _couponRepository = couponRepository;
            _mapper = mapper;

        }
        public async Task<CouponDto> Add(CouponDto item)
        {
            var isCodeTaken = _couponRepository.GetAll().Any(c => c.Code == item.Code);
            if (isCodeTaken)
            {
                return null;
            }

            Coupon newCoupon = _mapper.Map<Coupon>(item);
            var saved = _couponRepository.Add(newCoupon);

            return _mapper.Map<CouponDto>(saved);
        }

        public bool Delete(int id)
        {
            if (!Exists(id)) return false;
            return _couponRepository.Delete(id);
        }

        public bool Exists(int id)
        {
            return _couponRepository.Exists(id);    
        }

        public IEnumerable<CouponDto> GetAll()
        {
            var coupons = _couponRepository.GetAll();
            return _mapper.Map<IEnumerable<CouponDto>>(coupons);
        }

        public CouponDto? GetById(int id)
        {
            var c = _couponRepository.GetById(id);
            if (c == null) return null;
            return _mapper.Map<CouponDto>(c);
        }

        public bool Update(int id, CouponDto item)
        {
            var existingCoupon = _couponRepository.GetById(id);
            if (existingCoupon == null) return false;

            var isCodeTaken = _couponRepository.GetAll().Any(c => c.Code == item.Code && c.Id != id);
            if (isCodeTaken)
            {
                return false;
            }
            _mapper.Map(item, existingCoupon);
            return _couponRepository.Update(id, existingCoupon);
        }

        public bool IsCouponValid(string code, int userId, decimal currentOrderAmount)
        {
            var coupon = _couponRepository.GetAll().FirstOrDefault(c => c.Code == code);
            if (coupon == null) return false;
            if (coupon.ExpirationDate.HasValue && coupon.ExpirationDate < DateTime.Now) return false;
            if (coupon.IsUsed) return false;
            if (coupon.UserId.HasValue && coupon.UserId != userId) return false;
            if (coupon.MinimumOrderAmount.HasValue && currentOrderAmount < coupon.MinimumOrderAmount) return false;
            return true;
        }

        public bool MarkAsUsed(int userId, string couponCode)
        {
            var coupon = _couponRepository.GetAll().FirstOrDefault(c => c.Code == couponCode);
            if (coupon == null) return false;
            var alreadyUsed = coupon.Orders != null && coupon.Orders
                .Any(o => o.UserId == userId );
            if (alreadyUsed) return false;           
            coupon.IsUsed = true;
            return _couponRepository.Update(coupon.Id, coupon);
        }

        public IEnumerable<CouponDto> GetUnusedCouponsByUserId(int userId)
        {
            var coupons = _couponRepository.GetAll()
                .Where(c => c.UserId == userId && !c.IsUsed)
                .ToList();
            return _mapper.Map<IEnumerable<CouponDto>>(coupons);
        }

        public IEnumerable<CouponDto> GetExpiringSoon(int days)
        {
            var expirationDate = DateTime.Now.AddDays(days);
            var coupons = _couponRepository.GetAll()
                .Where(c => c.ExpirationDate.HasValue && c.ExpirationDate.Value <= expirationDate)
                .ToList();
            return _mapper.Map<IEnumerable<CouponDto>>(coupons);
        }

        public decimal ApplyDiscount(string code, decimal originalAmount, int userId)
        {
            var coupon = _couponRepository.GetAll().FirstOrDefault(c => c.Code == code);
            if (coupon == null) return originalAmount;
            if (coupon.MinimumOrderAmount.HasValue && originalAmount < coupon.MinimumOrderAmount.Value)
            {
                return originalAmount;
            }
            if (!IsCouponValid(code, userId, originalAmount))
            {
                return originalAmount;
            }
            decimal discount = 0;
            if (coupon.DiscountType == DiscountType.Percentage)
            {
                discount = originalAmount * (coupon.DiscountAmount / 100);
            }
            else
            {
                discount = coupon.DiscountAmount;
            }
            var finalAmount = originalAmount - discount;
            return finalAmount < 0 ? originalAmount : finalAmount;
        }
    
        public bool ConfirmRedemption(int userId, string couponCode, decimal amount)
        {

            if (!IsCouponValid(couponCode, userId, amount))
            {
                return false;
            }

            return MarkAsUsed(userId, couponCode);
        }

  
    }
}
