using Going.Plaid;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddDbContextFactory<PennySaverDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy => policy.WithOrigins("http://localhost:5173")
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials());
});

builder.Services.Configure<JwtOption>(
    builder.Configuration.GetSection("JwtSettings")
);
builder.Services.AddOptions<PlaidSettings>()
    .Bind(builder.Configuration.GetSection(PlaidSettings.SectionName))
    .Validate(options => !options.Enabled || 
        (!string.IsNullOrEmpty(options.ClientId) && 
         !string.IsNullOrEmpty(options.Secret)), 
        "Plaid:ClientId and Plaid:Secret are required when Plaid integration is enabled.")
    .ValidateOnStart();

var plaidEnabled = builder.Configuration.GetValue<bool>("Plaid:Enabled");

if (plaidEnabled)
{
    builder.Services.AddPlaid(builder.Configuration);
    builder.Services.AddSingleton<IBankSyncService, BankSyncService>();
}
else
{
    // Safety check: Prevent running mock financial syncs on live production servers
    if (builder.Environment.IsProduction())
    {
        throw new InvalidOperationException("Cannot use MockBankSyncService in a Production environment!");
    }
    builder.Services.AddSingleton<IBankSyncService, MockBankSyncService>();
}
builder.Services.AddScoped<IAccountSyncCoordinator, AccountSyncCoordinator>();
builder.Services.AddSingleton<IPlaidClientWrapper, PlaidClientWrapper>();

var jwtOptions = builder.Configuration.GetSection("JwtSettings").Get<JwtOption>() ?? throw new InvalidOperationException("JWT Settings are missing.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.IncludeErrorDetails = true; // Dev, off for Production

    options.MapInboundClaims = false;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtOptions.Issuer,

        ValidateAudience = true,
        ValidAudience = jwtOptions.Audience,
        
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
        
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30),

        RoleClaimType = "role",
        NameClaimType = "name"
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Reports.read", policy => policy.RequireClaim("scope", "Reports.read"));
});
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

app.UseCors("AllowReactApp");

using (var scope = app.Services.CreateScope())
{
    try
    {
        var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<PennySaverDbContext>>();
        //SeedUsers.Seed(contextFactory);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred while seeding the database: {ex.Message}");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication(); 
app.UseAuthorization();

app.MapControllers();


app.Run();