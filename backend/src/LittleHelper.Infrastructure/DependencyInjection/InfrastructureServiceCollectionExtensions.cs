using LittleHelper.Domain.CycleTracking;
using LittleHelper.Domain.Users;
using LittleHelper.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LittleHelper.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string sqliteConnectionString)
    {
        services.AddDbContext<LittleHelperDbContext>(options => options.UseSqlite(sqliteConnectionString));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICycleLogRepository, CycleLogRepository>();
        return services;
    }
}
