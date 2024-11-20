using MongoDB.Driver;
using DotNetEnv;
using CareerAI.Data;
using CareerAI.Services;
using CareerAI.Extensions;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        // Load environment variables from .env file
        Env.Load();

        // Add services to the container.
        services.AddControllersWithViews();
        services.AddSingleton<EmailService>();
        services.AddSingleton<SubDomainRouteTransformer>();

        // Configure MongoDB settings using environment variables
        var mongoConnectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING");
        var mongoDatabaseName = Environment.GetEnvironmentVariable("MONGODB_DATABASE_NAME");

        var mongoClient = new MongoClient(mongoConnectionString);
        var mongoDatabase = mongoClient.GetDatabase(mongoDatabaseName);

        // Register the MongoDB database instance
        services.AddSingleton<IMongoDatabase>(mongoDatabase);


        // Register UserRepository
        services.AddScoped<UserRepository>(sp =>
        {
            var database = sp.GetRequiredService<IMongoDatabase>();
            return new UserRepository(database);
        });

        // Register ChatHistoryRepository
        services.AddScoped<ChatHistoryRepository>(sp =>
        {
            var database = sp.GetRequiredService<IMongoDatabase>();
            return new ChatHistoryRepository(database);
        });

        // Add HttpClient and GoogleApiService
        services.AddHttpClient<GoogleApiService>(client =>
        {
            // Optionally configure the HttpClient here
        });

        services.AddSingleton(provider =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            var apiKey = configuration["GoogleApi:ApiKey"];
            var logger = provider.GetRequiredService<ILogger<GoogleApiService>>();
            return new GoogleApiService(provider.GetRequiredService<HttpClient>(), apiKey, logger);
        });

        // Add session services
        services.AddDistributedMemoryCache();
        services.AddSession(options =>
        {
            options.Cookie.HttpOnly = true; // Make the session cookie HTTP-only
            options.Cookie.IsEssential = true; // Make the session cookie essential
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // Configure the HTTP request pipeline.
        if (!env.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();
        app.UseSession();

        // app.UseEndpoints(endpoints =>
        // {
        //     endpoints.MapControllerRoute(
        //         name: "default",
        //         pattern: "{controller=Home}/{action=Index}/{id?}");

        //     endpoints.MapControllerRoute(
        //         name: "chat",
        //         pattern: "{controller=Chat}/{action=Index}/{id?}");
        // });
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllerRoute(
                name: "areas",
                pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
            );

            endpoints.MapDynamicControllerRoute<SubDomainRouteTransformer>(
                "{controller=Home}/{action=Index}/{id?}"
            );
        });
    }
}

public class SubDomainRouteTransformer : DynamicRouteValueTransformer
{
    public override async ValueTask<RouteValueDictionary> TransformAsync(HttpContext httpContext, RouteValueDictionary values)
    {
        var subDomain = httpContext.Request.Host.Host.Split(".").FirstOrDefault();
        Console.WriteLine(subDomain);
        if (!string.IsNullOrEmpty(subDomain) && subDomain != "career")
        {
            values["area"] = subDomain;
        }
        return values;
    }
}