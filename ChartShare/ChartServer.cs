using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

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
            options.ListenAnyIP(port, lOptions =>
            {
                lOptions.UseHttps(httpsOptions =>
                {
                    httpsOptions.ServerCertificate = GetOrGenerateCert();
                });
            });
        });

        var chartData = cds;
        builder.Services.AddSingleton(chartData);

        // 注册 SignalR 服务
        builder.Services.AddSignalR(options => {
            options.MaximumReceiveMessageSize = 1024 * 1024 * 8; // 8 MB
        });

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyHeader()
                      .AllowAnyMethod()
                      .SetIsOriginAllowed(_ => true) // 允许所有来源
                      .AllowCredentials();

            });
        });

        App = builder.Build();

        App.UseCors();
        App.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });

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

    private static X509Certificate2 GetOrGenerateCert()
    {
        string certPath = "chart_share.pfx";
        string password = "chart_share";

        if (File.Exists(certPath))
        {
            // 从文件加载时，必须指定存储标志，否则 Kestrel 无法访问私钥
            return new X509Certificate2(certPath, password,
                X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet);
        }
        else
        {
            using var newCert = CreateCert();

            byte[] certData = newCert.Export(X509ContentType.Pfx, password);
            File.WriteAllBytes(certPath, certData);

            return new X509Certificate2(certData, password,
                X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet);
        }
    }

    private static X509Certificate2 CreateCert()
    {
        using RSA rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=Majdata-ChartShare",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        var sanBuilder = new SubjectAlternativeNameBuilder();
        sanBuilder.AddDnsName("localhost");        // localhost
        sanBuilder.AddIpAddress(System.Net.IPAddress.Loopback); // 127.0.0.1
        sanBuilder.AddIpAddress(System.Net.IPAddress.Any);      // 0.0.0.0
        request.CertificateExtensions.Add(sanBuilder.Build());

        request.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension(
                new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false));

        var cert = request.CreateSelfSigned(
            DateTimeOffset.Now.AddDays(-1),
            DateTimeOffset.Now.AddYears(100));

        return cert;
    }
}
