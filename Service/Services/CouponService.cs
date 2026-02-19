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

namespace Service.Services
{
    public class CouponService : ICouponService
    {
        private readonly IRepository<Coupon> _couponRepository;

        public CouponService(IRepository<Coupon> couponRepository)
        {
            _couponRepository = couponRepository;
        }
        public CouponDto Add(CouponDto item)
        {
            var isCodeTaken = _couponRepository.GetAll().Any(c => c.Code == item.Code);
            if (isCodeTaken)
            {
                throw new Exception("קוד קופון זה כבר קיים במערכת");
            }

            Coupon newCoupon = new Coupon
            {
                Code = item.Code,
                DiscountAmount = item.DiscountAmount,
                ExpirationDate = item.ExpirationDate,
                IsUsed = false,           
                DiscountType = Enum.TryParse(item.DiscountType, out DiscountType type) ? type : DiscountType.Amount
            };

            var saved = _couponRepository.Add(newCoupon);
            item.Id = saved.Id;
            return item;
     
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
            return _couponRepository.GetAll().Select(c => new CouponDto
            {
                Id = c.Id,
                Code = c.Code,
                DiscountAmount = c.DiscountAmount,
                ExpirationDate = c.ExpirationDate,
                IsUsed = c.IsUsed,
                DiscountType = c.DiscountType.ToString()
            }).ToList();    
        }

        public CouponDto? GetById(int id)
        {
            var c = _couponRepository.GetById(id);
            if (c == null) return null;

            return new CouponDto
            {
                Id = c.Id,
                Code = c.Code,
                DiscountAmount = c.DiscountAmount,
                DiscountType = c.DiscountType.ToString(),
                ExpirationDate = c.ExpirationDate,
                IsUsed = c.IsUsed
            };
        }

        public bool Update(int id, CouponDto item)
        {
            var existingCoupon = _couponRepository.GetById(id);
            if (existingCoupon == null) return false;

            var isCodeTaken = _couponRepository.GetAll().Any(c => c.Code == item.Code && c.Id != id);
            if (isCodeTaken)
            {
                throw new Exception("קוד קופון זה כבר תפוס");
            }

            existingCoupon.Code = item.Code;
            existingCoupon.DiscountAmount = item.DiscountAmount;
            existingCoupon.ExpirationDate = item.ExpirationDate;
            existingCoupon.IsUsed = item.IsUsed;

            if (Enum.TryParse(item.DiscountType, out DiscountType type))
                existingCoupon.DiscountType = type;

            return _couponRepository.Update(id, existingCoupon);
        }
    }
}
