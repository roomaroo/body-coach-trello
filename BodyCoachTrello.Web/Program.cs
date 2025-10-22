using BodyCoachTrello.Core.Configuration;
using BodyCoachTrello.Core.Services;
using BodyCoachTrello.Web.Hubs;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add configuration
builder.Configuration.AddUserSecrets<Program>();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddSignalR();

// Configure Trello settings
builder.Services.Configure<TrelloConfiguration>(
    builder.Configuration.GetSection(TrelloConfiguration.SectionName));

// Note: Trello configuration validation will happen at runtime

// HTTP Client for Trello API
builder.Services.AddHttpClient<ITrelloApiService, TrelloApiService>();

// Application services
builder.Services.AddScoped<IShoppingListParser, ShoppingListParser>();
builder.Services.AddScoped<ITrelloApiService, TrelloApiService>();
builder.Services.AddScoped<IShoppingListImporter, ShoppingListImporter>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

// Map SignalR hub
app.MapHub<ShoppingListProgressHub>("/progressHub");

// Map API controllers
app.MapControllers();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
