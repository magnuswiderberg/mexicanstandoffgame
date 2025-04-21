using Blazor.Components;
using Blazor.GameEvents;
using Game.Repository;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.StaticFiles;
using Shared.GameEvents;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();


builder.Services.AddSignalR();
builder.Services.AddResponseCompression(options =>
{
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/octet-stream"]);
});

builder.Services.AddSingleton<GameRepository>();
builder.Services.AddScoped<IGameEvents, GameEventsHub>();


var app = builder.Build();

app.UseResponseCompression(); // Note: Needs to added immediately after app.Build()

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var gameEvents = scope.ServiceProvider.GetRequiredService<IGameEvents>();
    await app.Services.GetRequiredService<GameRepository>().GetOrCreateDevelopmentGameAsync(gameEvents);
}
else
{
    //app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = new FileExtensionContentTypeProvider
    {
        Mappings =
        {
            [".mp3"] = "audio/mpeg"
        }
    }
});
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapHub<GameEventsHub>(IGameEvents.HubUrl); // Note: Should be added just before app.Run()

await app.RunAsync();
