using Microsoft.Extensions.DependencyInjection;
using AutoMapper;
using Service.Interfaces; 
using Service.Services;
using Repository;

namespace Service.Services

{
    public static class ExtensionService
    {
        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            
            services.AddRepository();          
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ICarService, CarService>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<ICarFeedbackService, CarFeedbackService>();
            services.AddScoped<IRegionService, RegionService>();
            services.AddScoped<ICouponService, CouponService>();
            services.AddAutoMapper(typeof(MapperProfile));
            services.AddScoped<IEmailService, EmailService>();
            services.AddHttpContextAccessor();
            return services;
        }
    }
}