using AutoMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Repository;
using Service.Interfaces; 
using Service.Services;

namespace Service.Services

{
    public static class ExtensionService
    {
        public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
        {      
            services.AddRepository();          
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ICarService, CarService>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<ICarFeedbackService, CarFeedbackService>();
            services.AddScoped<IRegionService, RegionService>();
            services.AddScoped<ICouponService, CouponService>();
            services.AddAutoMapper(typeof(MapperProfile));
            services.Configure<EmailSettings>(configuration.GetSection("EmailSettings")); services.AddScoped<IEmailService, EmailService>();
            services.AddHttpContextAccessor();
            return services;
        }
    }
}