using PersonalTools.Classes.Dashboard;
using PersonalTools.Classes.Notes;
using PersonalTools.Classes.Skins;
using PersonalTools.Data.Local;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024;
});

builder.Services.AddScoped<IDashboardFuncs, DashboardFuncs>();

// Storage
builder.Services.AddScoped<ILocalJsonData, LocalJsonData>();

// Skins
builder.Services.AddScoped<ISkinFuncs, SkinFuncs>();

// Notes
builder.Services.AddScoped<INoteFuncs, NoteFuncs>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();