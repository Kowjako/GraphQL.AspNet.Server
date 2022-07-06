using GraphQL.Execution;
using GraphQL.Samples.Schemas.Chat;
using GraphQL.Server.Transports.AspNetCore;
using GraphQL.Server.Ui.Altair;
using GraphQL.Server.Ui.GraphiQL;
using GraphQL.Server.Ui.Playground;
using GraphQL.Server.Ui.Voyager;

namespace GraphQL.Samples.Complex;

public class StartupWithRouting
{
    public StartupWithRouting(IConfiguration configuration, IWebHostEnvironment environment)
    {
        Configuration = configuration;
        Environment = environment;
    }

    public IConfiguration Configuration { get; }

    public IWebHostEnvironment Environment { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddRouting()
            .AddSingleton<IChat, Chat>()
            .Configure<ErrorInfoProviderOptions>(opt => opt.ExposeExceptionStackTrace = Environment.IsDevelopment());

        services.AddGraphQL(builder => builder
            .AddApolloTracing()
            .AddSchema<ChatSchema>()
            .AddAutoClrMappings()
            .ConfigureExecutionOptions(options =>
            {
                options.EnableMetrics = Environment.IsDevelopment();
                var logger = options.RequestServices!.GetRequiredService<ILogger<Startup>>();
                options.UnhandledExceptionDelegate = ctx =>
                {
                    logger.LogError("{Error} occurred", ctx.OriginalException.Message);
                    return Task.CompletedTask;
                };
            })
            .AddSystemTextJson()
            .AddErrorInfoProvider<CustomErrorInfoProvider>()
            .AddDataLoader()
            .AddUserContextBuilder(context => new Dictionary<string, object?> { { "user", context.User.Identity!.IsAuthenticated ? context.User : null } })
            .AddGraphTypes(typeof(ChatSchema).Assembly));
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app)
    {
        if (Environment.IsDevelopment())
            app.UseDeveloperExceptionPage();

        app.UseWebSockets();

        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGraphQL<GraphQLHttpMiddlewareWithLogs<ChatSchema>>("/graphql", new GraphQLHttpMiddlewareOptions());

            endpoints.MapGraphQLPlayground(new PlaygroundOptions
            {
                BetaUpdates = true,
                RequestCredentials = RequestCredentials.Omit,
                HideTracingResponse = false,

                EditorCursorShape = EditorCursorShape.Line,
                EditorTheme = EditorTheme.Light,
                EditorFontSize = 14,
                EditorReuseHeaders = true,
                EditorFontFamily = "Consolas",

                PrettierPrintWidth = 80,
                PrettierTabWidth = 2,
                PrettierUseTabs = true,

                SchemaDisableComments = false,
                SchemaPollingEnabled = true,
                SchemaPollingEndpointFilter = "*localhost*",
                SchemaPollingInterval = 5000,

                Headers = new Dictionary<string, object>
                {
                    ["MyHeader1"] = "MyValue",
                    ["MyHeader2"] = 42,
                },
            });

            endpoints.MapGraphQLGraphiQL(new GraphiQLOptions
            {
                Headers = new Dictionary<string, string>
                {
                    ["X-api-token"] = "130fh9823bd023hd892d0j238dh",
                }
            });

            endpoints.MapGraphQLAltair(new AltairOptions
            {
                Headers = new Dictionary<string, string>
                {
                    ["X-api-token"] = "130fh9823bd023hd892d0j238dh",
                }
            });

            endpoints.MapGraphQLVoyager(new VoyagerOptions
            {
                Headers = new Dictionary<string, object>
                {
                    ["MyHeader1"] = "MyValue",
                    ["MyHeader2"] = 42,
                },
            });
        });
    }
}