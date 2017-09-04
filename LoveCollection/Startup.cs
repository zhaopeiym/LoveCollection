using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Text.Unicode;
using Microsoft.Extensions.WebEncoders;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.StaticFiles;

namespace LoveCollection
{
    public class Startup
    {
        /// <summary>
        /// 连接字符串
        /// </summary>
        public static string connection;
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //services.Configure<WebEncoderOptions>(options =>
            //{
            //    options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All);
            //});

            services.AddMvc();

            //注意：一定要加 sslmode=none 
            connection = Configuration.GetConnectionString("MySqlConnection");        
            services.AddDbContext<CollectionDBCotext>(options => options.UseMySql(connection));

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {

            var staticfile = new StaticFileOptions();
            //staticfile.FileProvider = new PhysicalFileProvider(@"C:\");//指定目录 这里指定C盘,也可以是其它目录
            //staticfile.ServeUnknownFileTypes = true;
            //staticfile.DefaultContentType = "application/x-msdownload"; //设置默认  MIME
            var provider = new FileExtensionContentTypeProvider();
            provider.Mappings.Add(".crx", "application/octet-stream");//手动设置对应MIME
            staticfile.ContentTypeProvider = provider;
            app.UseStaticFiles(staticfile);



            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
