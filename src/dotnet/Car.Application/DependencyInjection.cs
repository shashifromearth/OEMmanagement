using System.Reflection;
using Car.Application.AgentTurns;
using Car.Application.WorkOrders;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Car.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddValidatorsFromAssemblyContaining<HandleAgentTurnValidator>();
        services.AddValidatorsFromAssemblyContaining<ScheduleServiceValidator>();
        return services;
    }
}
