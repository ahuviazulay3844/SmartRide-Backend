using AutoMapper;
using Common.Dto;
using Repository.Entities;

namespace Service.Services
{
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
            // 1. מיפויים פשוטים
            CreateMap<Region, RegionDto>().ReverseMap();
            CreateMap<Coupon, CouponDto>().ReverseMap();
            CreateMap<CarFeedback, CarFeedbackDto>().ReverseMap();

            // 2. מיפוי רכבים
            CreateMap<Car, CarDto>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ReverseMap();

            // 3. מיפוי משתמשים
            CreateMap<User, UserDto>()
                .ForMember(dest => dest.UserType, opt => opt.MapFrom(src => src.UserType.ToString()))
                .ReverseMap();

            // 4. מיפוי הזמנות
            CreateMap<Order, OrderDto>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.CarModel, opt => opt.MapFrom(src => src.Car != null ? src.Car.Model : null))
                .ReverseMap();
        }
    }
}