using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;
using OrchardCore.Localization;
using System.Threading;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Options;

namespace POLocalizaion
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMemoryCache();

            services.AddLocalization(options =>
            {
                // I prefer Properties over the default `Resources` folder
                // due to namespace issues if you have a Resources type as
                // most people do for shared resources.
                options.ResourcesPath = "Properties";
            });

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddSingleton<IStringLocalizerFactory, MyStringLocalizerFactory>();
            services.AddSingleton<IHtmlLocalizerFactory, MyHtmlLocalizerFactory>();


            services.AddMvc()
                .AddMvcLocalization(LanguageViewLocationExpanderFormat.Suffix)
                .AddViewLocalization()
                .AddDataAnnotationsLocalization()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1)
                ;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            var supportedCultures = new List<CultureInfo>
            {
                new CultureInfo("en-US"),
                new CultureInfo("ar-SA")
            };

            var option = new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("en-US"),
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures
            };

            var locOptions = app.ApplicationServices.GetService<IOptions<RequestLocalizationOptions>>();
            locOptions.Value.SupportedCultures = supportedCultures;
            locOptions.Value.SupportedUICultures = supportedCultures;

            app.UseRequestLocalization(locOptions.Value);


            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }


    public class MyLocalizer : IStringLocalizer
    {
        private static Dictionary<string, Dictionary<string, string>> CulturesDictionaries { get; set; } =
            new Dictionary<string, Dictionary<string, string>>();


        static MyLocalizer()
        {

            var cultureDictionary = new Dictionary<string, string>();

            cultureDictionary.Add("Hello", "Hello");
            cultureDictionary.Add("FullNameRequired", "{0} is required. (en)");
            cultureDictionary.Add("FullNameMaxLength", "{0} cannot have more than {1} charactors (en)");
            cultureDictionary.Add("FullName", "Full name");

            CulturesDictionaries.Add("en-US", cultureDictionary);

            cultureDictionary = new Dictionary<string, string>();
            cultureDictionary.Add("Hello", "Ahlan wa Sehlan");
            cultureDictionary.Add("FullNameRequired", "{0} is required. (ar)");
            cultureDictionary.Add("FullNameMaxLength", "{0} cannot have more than {1} charactors (ar)");
            cultureDictionary.Add("FullName", "(Arabic) Full name");
            CulturesDictionaries.Add("ar-SA", cultureDictionary);

        }

        public MyLocalizer()
        {

        }

        public LocalizedString this[string name]
        {
            get
            {
                if (name == null)
                {
                    throw new ArgumentNullException(nameof(name));
                }

                var translation = GetTranslation(name);
                return new LocalizedString(name, translation ?? name, translation == null);
            }
        }

        public LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                var translation = GetTranslation(name);
                var formatted = string.Format(translation, arguments);

                return new LocalizedString(name, formatted, translation != null);
            }
        }

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            var cultureDictionary = CulturesDictionaries[CultureInfo.CurrentCulture.Name];
            return cultureDictionary.Select(t => new LocalizedString(t.Key, t.Value));
        }


        public IStringLocalizer WithCulture(CultureInfo culture)
        {
            return MyStringLocalizerFactory.MyLocalizers[culture.Name];
        }

        public string GetTranslation(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            string translation;
            var recordFound = CulturesDictionaries[CultureInfo.CurrentCulture.Name].TryGetValue(name, out translation);

            return (new LocalizedString(name, translation ?? name, recordFound));

        }
    }

    public class MyStringLocalizerFactory : IStringLocalizerFactory
    {
        public static Dictionary<string, MyLocalizer> MyLocalizers { get; set; }
            = new Dictionary<string, MyLocalizer>();

        public IStringLocalizer Create(Type resourceSource)
        {
            MyLocalizer localizer = null;

            if(!MyLocalizers.TryGetValue(CultureInfo.CurrentCulture.Name, out localizer))
            {
                localizer = new MyLocalizer();
                MyLocalizers.Add(CultureInfo.CurrentCulture.Name, localizer);
            }

            return localizer;
        }

        public IStringLocalizer Create(string baseName, string location)
        {
            MyLocalizer localizer = null;

            if (!MyLocalizers.TryGetValue(CultureInfo.CurrentCulture.Name, out localizer))
            {
                localizer = new MyLocalizer();
                MyLocalizers.Add(CultureInfo.CurrentCulture.Name, localizer);
            }

            return localizer;
        }
    }

    public class MyHtmlLocalizer : HtmlLocalizer
    {
        private readonly IStringLocalizer _localizer;

        public MyHtmlLocalizer(IStringLocalizer localizer) : base(localizer)
        {
            _localizer = localizer;
        }

        public override LocalizedHtmlString this[string name]
        {
            get
            {
                return ToHtmlString(_localizer[name]);
            }
        }

        public override LocalizedHtmlString this[string name, params object[] arguments]
        {
            get
            {
               

                return ToHtmlString(_localizer[name], arguments);
            }
        }


    }

    public class MyHtmlLocalizerFactory : IHtmlLocalizerFactory
    {
        private readonly IStringLocalizerFactory _stringLocalizerFactory;

        public MyHtmlLocalizerFactory(IStringLocalizerFactory stringLocalizerFactory)
        {
            _stringLocalizerFactory = stringLocalizerFactory;
        }

        public IHtmlLocalizer Create(Type resourceSource)
        {
            return new MyHtmlLocalizer(_stringLocalizerFactory.Create(resourceSource));
        }

        public IHtmlLocalizer Create(string baseName, string location)
        {
            return new MyHtmlLocalizer(_stringLocalizerFactory.Create(baseName, location));
        }
    }
}
