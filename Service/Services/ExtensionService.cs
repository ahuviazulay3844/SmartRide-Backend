using Microsoft.Extensions.DependencyInjection;
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
            
        
            return services;
        }
    }
}