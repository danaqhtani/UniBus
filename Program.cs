using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using UniBusApp.Data;
using UniBusApp.Models;
using UniBusApp.Services;

var builder = WebApplication.CreateBuilder(args);

// 🔹 1) ربط قاعدة البيانات (SQL Server)
builder.Services.AddDbContext<UniBusDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("UniBusDb"));
});

// 🔹 2) تفعيل الترجمة (عربي / إنجليزي)
builder.Services.AddLocalization(options =>
{
    options.ResourcesPath = "Resources";
});

builder.Services.AddControllersWithViews()
    .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
    .AddDataAnnotationsLocalization();

// 🔹 3) ربط إعدادات الإيميل + خدمة الإرسال
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddScoped<TripGeneratorService>();  //for the generated trip
builder.Services.AddHttpClient();
builder.Services.AddScoped<TripTrackingService>();

// 🔹 4) تفعيل Session
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 🔹 5) بناء التطبيق
var app = builder.Build();

// 🔹 6) إعدادات الأخطاء
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// 🔹 7) إعداد اللغات المدعومة
var supportedCultures = new[] { "en", "ar" };

var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture("ar")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

localizationOptions.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider());

app.UseRequestLocalization(localizationOptions);

// 🔹 8) Middleware الأساسية
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.UseAuthorization();

// 🔹 9) الصفحة الافتراضية
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// 🔹 10) تشغيل التطبيق
app.Run();

