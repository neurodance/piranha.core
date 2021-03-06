﻿using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Piranha;

namespace Blog
{
    public class Startup
    {
        #region Properties
        /// <summary>
        /// The application config.
        /// </summary>
        public IConfigurationRoot Configuration { get; set; }
        #endregion

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="env">The current hosting environment</param>
        public Startup(IHostingEnvironment env) {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            Configuration = builder.Build();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services) {
            services.AddMvc(config => {
                config.ModelBinderProviders.Insert(0, new Piranha.Areas.Manager.Binders.AbstractModelBinderProvider());
            });
            services.AddMvc();
            services.AddPiranhaEF(options => options.UseSqlite("Filename=./blog.db"));
            services.AddPiranhaManager();
            services.AddScoped<IApi, Api>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IApi api, Piranha.EF.Db db) {
            loggerFactory.AddConsole();

            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }

            // Build page types
            var pageTypeBuilder = new Piranha.Builder.Json.PageTypeBuilder(api)
                .AddJsonFile("piranha.json");
            pageTypeBuilder.Build();
            var blockTypeBuilder = new Piranha.Builder.Json.BlockTypeBuilder(api)
                .AddJsonFile("piranha.json");
            blockTypeBuilder.Build();

            // Initialize the piranha application
            App.Init(api);

            // Register middleware
            app.UseStaticFiles();
            app.UsePiranha();
            app.UsePiranhaManager();

            app.UseMvc(routes => {
                routes.MapRoute(name: "areaRoute",
                    template: "{area:exists}/{controller}/{action}/{id?}",
                    defaults: new { controller = "Home", action = "Index" });

                routes.MapRoute(
                    name: "default",
                    template: "{controller=home}/{action=index}/{id?}");
            });
            Seed(api, db);
        }



        /// <summary>
        /// Seeds some test data.
        /// </summary>
        /// <param name="db"></param>
        private void Seed(IApi api, Piranha.EF.Db db) {
            if (db.Categories.Count() == 0) {
                // Add the blog category
                var category = new Piranha.EF.Data.Category() {
                    Id = Guid.NewGuid(),
                    Title = "Blog",
                    ArchiveTitle = "Blog Archive"
                };
                db.Categories.Add(category);

                // Add a post
                var post = new Piranha.EF.Data.Post() {
                    CategoryId = category.Id,
                    Title = "My first post",
                    Excerpt = "Etiam porta sem malesuada magna mollis euismod.",
                    Body = "<p>Praesent commodo cursus magna, vel scelerisque nisl consectetur et. Morbi leo risus, porta ac consectetur ac, vestibulum at eros. Integer posuere erat a ante venenatis dapibus posuere velit aliquet. Nullam id dolor id nibh ultricies vehicula ut id elit.</p>",
                    Published = DateTime.Now
                };
                db.Posts.Add(post);

                // Add the startpage
                var startPage = Models.StartPageModel.Create("Start");
                startPage.Title = "Welcome to Piranha CMS";
                startPage.Slug = "start";
                startPage.Content = "<p>Lorem ipsum</p>";
                startPage.Intro.Title = "Say hi to the new version of Piranha CMS!";
                startPage.Intro.Body = "We hope you like it :)";
                startPage.Slider.Add(new Models.SliderItem() {
                    Title = "Slide 1",
                    Body = "<p>Lorem</p>"
                });
                startPage.Slider.Add(new Models.SliderItem() {
                    Title = "Slide 2",
                    Body = "<p>Ipsum</p>"
                });
                startPage.Published = DateTime.Now;
                api.Pages.Save(startPage);

                db.SaveChanges();
            }
        }
    }
}
