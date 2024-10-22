// Licensed under the EUPL-1.2-or-later
using System.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace RenderRazorMVCToString;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        var sp = CreateServiceProvider();

        const string template = "Views/View.cshtml";

        var result = await RenderRazorView(sp, template);

        Console.WriteLine(result);

    }
    
    static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();


        // Stuff that just exists because RazorViewEngine needs it
        services.AddSingleton<IWebHostEnvironment>(new WebHostEnvironment { ApplicationName = AppDomain.CurrentDomain.FriendlyName});
        var diagnosticListener = new DiagnosticListener("MyFancyListener");
        services.AddSingleton<DiagnosticSource>(diagnosticListener);
        services.AddSingleton(diagnosticListener);

        // Add MVC
        var mvcCoreBuilder = services.AddControllersWithViews();

        // Tell MVC where to look for Razor views
        var defaultFileProvider = new PhysicalFileProvider( AppDomain.CurrentDomain.BaseDirectory);
        mvcCoreBuilder.AddRazorRuntimeCompilation(
            o =>
            {
                o.FileProviders.Clear();
                o.FileProviders.Add(defaultFileProvider);
            });

        var serviceProvider = services.BuildServiceProvider();
        return serviceProvider;
    }

    static async Task<string> RenderRazorView(ServiceProvider sp1, string s)
    {
        var viewRenderer = sp1.GetRequiredService<IRazorViewEngine>();

        var viewEngineResult = viewRenderer.GetView(executingFilePath: null, viewPath: s, isMainPage: true);
        var view = viewEngineResult.View;
        if (view == null)
            throw new InvalidOperationException("View is null");

        var httpContext = new DefaultHttpContext { RequestServices = sp1 };
        var ac = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var tempDataProvider = sp1.GetRequiredService<ITempDataProvider>();

        await using var output = new StringWriter();
        var viewContext = new ViewContext(
            ac,
            view,
            new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            new TempDataDictionary(httpContext, tempDataProvider),
            output,
            new HtmlHelperOptions());

        // Set a model if needed
        viewContext.ViewData.Model = new object();

        await view.RenderAsync(viewContext);
        return output.ToString();
    }
}