using AutoMapper;
using Common.Dto;
using Repository.Entities;

namespace Service.Services
{
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
            // מיפוי בין ה-Enums השונים
            CreateMap<Repository.Entities.OrderStatus, Common.Dto.OrderStatus>().ReverseMap();
            CreateMap<Repository.Entities.CarStatus, Common.Dto.CarStatus>().ReverseMap();
            // 1. מיפויים פשוטים
            CreateMap<Region, RegionDto>().ReverseMap();
            CreateMap<Coupon, CouponDto>().ReverseMap();
            CreateMap<CarFeedback, CarFeedbackDto>().ReverseMap();

            // 2. מיפוי רכבים
             CreateMap<Car, CarDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.CarCategory))
            .ForMember(dest => dest.PricePerHour, opt => opt.MapFrom(src => src.PricePerHour))
            .ForMember(dest => dest.PricePerDay, opt => opt.MapFrom(src => src.PricePerDay))
            .ForMember(dest => dest.PricePerKm, opt => opt.MapFrom(src => src.PricePerKm))
            .ReverseMap();

            // 3. מיפוי משתמשים
            CreateMap<User, UserDto>()
         .ForMember(dest => dest.UserType, opt => opt.MapFrom(src => src.UserType.ToString()));

            // מ-DTO ל-Entity (עבור ה-Update וה-Add)
            CreateMap<UserDto, User>()
                .ForMember(dest => dest.Id, opt => opt.Ignore()) // התעלמות מה-Id כדי למנוע את שגיאת ה-InvalidOperationException
                .ForMember(dest => dest.UserType, opt => opt.Ignore()); // בדרך כלל לא נרצה לעדכן סוג משתמש דרך פרופיל רגיל

            // 4. מיפוי הזמנות
            //CreateMap<Order, OrderDto>()
            //    .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            //    .ForMember(dest => dest.CarModel, opt => opt.MapFrom(src => src.Car != null ? src.Car.Model : null))
            //    .ReverseMap();
            // מ-Entity ל-DTO (בשביל ה-React)
            //CreateMap<Order, OrderDto>()
            //    .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status)) // נשמור עליו כ-Enum בתוך ה-DTO
            //    .ForMember(dest => dest.CarModel, opt => opt.MapFrom(src => src.Car != null ? src.Car.Model : null));

            //// מ-DTO חזרה ל-Entity (בשביל ה-Update)
            //CreateMap<OrderDto, Order>()
            //   .ForMember(dest => dest.Status, opt => opt.MapFrom(src => Repository.Entities.OrderStatus.Pending)) // כפייה של סטטוס התחלתי
            //   .ForMember(dest => dest.Car, opt => opt.Ignore());
            // 4. מיפוי הזמנות משופר
            //CreateMap<Order, OrderDto>()
            //    .ForMember(dest => dest.CarModel, opt => opt.MapFrom(src => src.Car != null ? src.Car.Model : ""))
            //    .ForMember(dest => dest.UserFullName, opt => opt.MapFrom(src => src.User != null ? $"{src.User.FirstName} {src.User.LastName}" : ""))
            //    .ReverseMap() // זה מאפשר מעבר דו-כיווני אוטומטי לרוב השדות
            //    .ForMember(dest => dest.Car, opt => opt.Ignore()) // אל תנסה למפות את אובייקט הרכב עצמו, רק את ה-ID
            //    .ForMember(dest => dest.User, opt => opt.Ignore()); // אל תנסה למפות את אובייקט המשתמש
            CreateMap<Order, OrderDto>()
        .ForMember(dest => dest.CarModel,
            opt => opt.MapFrom(src => src.Car != null ? src.Car.Model : ""))
        .ForMember(dest => dest.UserFullName,
            opt => opt.MapFrom(src => src.User != null ? $"{src.User.FirstName} {src.User.LastName}" : ""))
        .ForMember(dest => dest.Status,
            opt => opt.MapFrom(src => src.Status));

            CreateMap<OrderDto, Order>()
                .ForMember(dest => dest.Car, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.TotalDays, opt => opt.MapFrom(src => src.TotalDays))
                .ForMember(dest => dest.TotalHours, opt => opt.MapFrom(src => src.TotalHours));
            // מיפוי עבור השאלון - דו כיווני
            CreateMap<CarInspection, CarInspectionDto>().ReverseMap();

        }

    }
}