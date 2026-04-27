using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VehicleRental.Application.Interfaces;
using VehicleRental.Infrastructure.Data;

namespace VehicleRental.Tests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    // 每個 factory 實例使用固定名稱 — 確保所有請求與測試準備共用同一個 InMemory DB
    private readonly string _dbName = "TestDb-" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // 移除所有 AppDbContext / IAppDbContext 相關 DI 登錄（含 SqlServer provider 設定）
            var toRemove = services
                .Where(d =>
                    d.ServiceType == typeof(AppDbContext) ||
                    d.ServiceType == typeof(IAppDbContext) ||
                    d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    (d.ServiceType.IsGenericType &&
                     d.ServiceType.GetGenericTypeDefinition().Name
                         .StartsWith("IDbContextOptionsConfiguration")))
                .ToList();

            foreach (var d in toRemove)
                services.Remove(d);

            // 以固定名稱的 InMemory DB 重新登錄（同一 factory 所有請求共用）
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            services.AddScoped<IAppDbContext>(sp =>
                sp.GetRequiredService<AppDbContext>());
        });

        builder.UseEnvironment("Development");
    }
}


