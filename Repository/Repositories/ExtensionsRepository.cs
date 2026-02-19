using Microsoft.Extensions.DependencyInjection;
using Repository.Entities;
using Repository.Interfaces;
using Repository.Repositories;

namespace Repository
{
    public static class RepositoryExtensions
    {
        public static IServiceCollection AddRepository(this IServiceCollection services)
        {


            services.AddScoped<IRepository<User>, UserRepository>();
            services.AddScoped<IRepository<Car>, CarRepository>();
            services.AddScoped<IRepository<Order>, OrderRepository>();
            services.AddScoped<IRepository<Region>, RegionRepository>();
            services.AddScoped<IRepository<Coupon>, CouponRepository>();
            services.AddScoped<IRepository<CarFeedback>, FeedbackRepository>();
            return services;
        }
    }
}