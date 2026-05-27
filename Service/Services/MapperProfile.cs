using AutoMapper;
using Common.Dto;
using Repository.Entities;

namespace Service.Services
{
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
         
            
           
            CreateMap<Region, RegionDto>().ReverseMap();
            CreateMap<Coupon, CouponDto>().ReverseMap();
            CreateMap<CarFeedback, CarFeedbackDto>().ReverseMap();

            // car
             CreateMap<Car, CarDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.CarCategory))
            .ReverseMap();

            // user
            CreateMap<User, UserDto>()
           .ForMember(dest => dest.UserType, opt => opt.MapFrom(src => src.UserType.ToString()));

            CreateMap<UserDto, User>()
                .ForMember(dest => dest.Id, opt => opt.Ignore()) 
                .ForMember(dest => dest.UserType, opt => opt.Ignore()); 

            CreateMap<Order, OrderDto>()
           .ForMember(dest => dest.CarModel,
               opt => opt.MapFrom(src => src.Car != null ? src.Car.Model : ""))
           .ForMember(dest => dest.UserFullName,
               opt => opt.MapFrom(src => src.User != null ? $"{src.User.FirstName} {src.User.LastName}" : ""))
           .ForMember(dest => dest.Status,
               opt => opt.MapFrom(src => src.Status));
        
        
            CreateMap<OrderDto, Order>()
                   .ForMember(dest => dest.Id, opt => opt.Ignore())
                   .ForMember(dest => dest.Car, opt => opt.Ignore())
                   .ForMember(dest => dest.User, opt => opt.Ignore())
                   .ForMember(dest => dest.Inspection, opt => opt.Ignore())
                   .ForMember(dest => dest.Feedback, opt => opt.Ignore())
                   .ForMember(dest => dest.Coupon, opt => opt.Ignore())
                   .ForMember(dest => dest.TotalDays, opt => opt.MapFrom(src => src.TotalDays))
                   .ForMember(dest => dest.TotalHours, opt => opt.MapFrom(src => src.TotalHours));
               
            CreateMap<CarInspection, CarInspectionDto>().ReverseMap();

            CreateMap<Repository.Entities.OrderStatus, Common.Dto.OrderStatus>().ReverseMap();
            CreateMap<Repository.Entities.CarStatus, Common.Dto.CarStatus>().ReverseMap();
        }

    }
}