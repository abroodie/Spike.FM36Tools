using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage.Files.Shares;
using ESFA.DC.FileService;
using ESFA.DC.FileService.Config;
using ESFA.DC.FileService.Config.Interface;
using ESFA.DC.FileService.Interface;
using ESFA.DC.JobContext.Interface;
using ESFA.DC.Queueing;
using ESFA.DC.Queueing.Interface;
using ESFA.DC.Queueing.Interface.Configuration;
using ESFA.DC.Serialization.Interfaces;
using ESFA.DC.Serialization.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spike.FM36Tool.Application.JobContext;
using Spike.FM36Tool.Application.PeriodEnd;
using Spike.FM36Tool.Application.Submission;
using Spike.FM36Tool.Data;

namespace Spike.FM36Tool
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddSingleton<WeatherForecastService>();
            AddDcServices(services);
        }

        public void AddDcServices(IServiceCollection services)
        {
            var subscriptionName = Configuration.GetSection("DcConfiguration")["SubscriptionName"];
            var topicName = Configuration.GetSection("DcConfiguration")["topicName"];
            var storageContainer = Configuration.GetSection("DcConfiguration")["blobStorageContainer"];
            var storageConnectionString = Configuration.GetConnectionString("DcStorageConnectionString");
            var servicebusConnectionString = Configuration.GetConnectionString("DcServicebusConnectionString");
            services.AddSingleton<JsonSerializationService>();
            services.AddSingleton<IJsonSerializationService>(x => x.GetRequiredService<JsonSerializationService>());
            services.AddSingleton<ISerializationService>(x => x.GetRequiredService<JsonSerializationService>());
            services.AddSingleton<IAzureStorageFileServiceConfiguration>(c => new AzureStorageFileServiceConfiguration
                {ConnectionString = storageConnectionString});

            services.AddScoped<IFileService, AzureStorageFileService>();

            services.AddSingleton<ITopicConfiguration>(c => new TopicConfiguration(servicebusConnectionString,
                topicName, subscriptionName, 10, maximumCallbackTimeSpan: TimeSpan.FromMinutes(40)));

            services.AddScoped<ITopicPublishService<JobContextDto>, TopicPublishService<JobContextDto>>();
            services.AddScoped<DcHelper>();
            services.AddSingleton<ShareClient>(new ShareClient(
                Configuration.GetConnectionString("FileStorageConnectionString"),
                Configuration.GetSection("AppSettings")["ShareName"]));

            services.AddScoped<AzureFileStorageFM36FilesProvider>();
            services.AddScoped<AzureFileStorageFm36FolderSubmission>();
            services.AddScoped<TopicPublishingServiceFactory>();
            services.AddScoped<PeriodEndService>();
        }


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
