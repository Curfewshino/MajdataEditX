using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace MajdataEdit.ChartShare;

public static class ChartServer
{
    public static WebApplication? App { get; set; }

    // 启动服务器
    public static async Task StartAsync(HubDataService cds, string path, int port = 8014)
    {
        var builder = WebApplication.CreateBuilder();

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(port);
        });

#if DEBUG
        builder.Logging.ClearProviders();
        builder.Logging.AddDebug();
        builder.Logging.SetMinimumLevel(LogLevel.Debug);
#endif

        var chartData = cds;
        builder.Services.AddSingleton(chartData);

        // 注册 SignalR 服务
        builder.Services.AddSignalR(options => {
            options.MaximumReceiveMessageSize = 1024 * 1024 * 8; // 8 MB
#if DEBUG
            options.ClientTimeoutInterval = TimeSpan.FromMinutes(30);
            options.KeepAliveInterval = TimeSpan.FromMinutes(15); //有容乃大
#endif
        });

        App = builder.Build();

        App.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(path),
            RequestPath = "/chartFiles"
        });

        // 映射 Hub
        App.MapHub<ChartHub>("/chartHub");

        await App.StartAsync();
    }

    // 停止服务器
    public static async Task StopAsync()
    {
        if (App != null)
        {
            await App.StopAsync();
            await App.DisposeAsync();
            App = null;
        }
    }
}
