using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrderApi.Data;
using OrderWorker;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddDbContext<OrderContext>(options =>
            options.UseNpgsql(context.Configuration.GetConnectionString("Postgres")));

        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
