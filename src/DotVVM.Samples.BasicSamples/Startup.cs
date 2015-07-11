using System;
using System.IO;
using System.Web.Hosting;
using Microsoft.Owin;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles;
using Owin;
using DotVVM.Framework;
using DotVVM.Framework.Configuration;
using Microsoft.Owin.Security.Cookies;
using Microsoft.AspNet.Identity;
using DotVVM.Framework.Storage;
using DotVVM.Framework.ResourceManagement;

[assembly: OwinStartup(typeof(DotVVM.Samples.BasicSamples.Startup))]
namespace DotVVM.Samples.BasicSamples
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            //app.UseDotvvmErrorPages();
            app.UseErrorPage();

            app.UseCookieAuthentication(new CookieAuthenticationOptions()
            {
                LoginPath = new PathString("/AuthSample/Login"),
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                Provider = new CookieAuthenticationProvider()
                {
                    OnApplyRedirect = c =>
                    {
                        // redirect to login page on 401 request
                        if (c.Response.StatusCode == 401 && c.Request.Method == "GET")
                        {
                            c.Response.StatusCode = 302;
                            c.Response.Headers["Location"] = c.RedirectUri;
                        }
                        // do not do anything on redirection to returnurl
                        // to not return page when ViewModel is expected
                        // we should implement this in DotVVM framework,
                        // not samples
                    }
                }
            });

            var applicationPhysicalPath = HostingEnvironment.ApplicationPhysicalPath;

            // use DotVVM
            DotvvmConfiguration dotvvmconfiguration = app.UseDotVVM(applicationPhysicalPath);
            dotvvmconfiguration.RouteTable.Add("Sample1", "Sample1", "sample1.dothtml", null);
            dotvvmconfiguration.RouteTable.Add("Sample2", "Sample2", "sample2.dothtml", null);
            dotvvmconfiguration.RouteTable.Add("Sample3", "Sample3", "sample3.dothtml", null);
            dotvvmconfiguration.RouteTable.Add("Sample4", "Sample4", "sample4.dothtml", null);
            dotvvmconfiguration.RouteTable.Add("Sample5", "Sample5", "sample5.dothtml", null);
            dotvvmconfiguration.RouteTable.Add("Sample6", "Sample6", "sample6.dothtml", null);
            dotvvmconfiguration.RouteTable.Add("Sample7", "Sample7", "sample7.dothtml", null);
            dotvvmconfiguration.RouteTable.Add("Sample8", "Sample8", "sample8.dothtml", null);
            dotvvmconfiguration.RouteTable.Add("Sample9", "Sample9", "sample9.dothtml", null);
            dotvvmconfiguration.RouteTable.Add("Sample10", "Sample10", "sample10.dothtml", null);
            dotvvmconfiguration.RouteTable.Add("Sample11", "Sample11", "sample11.dothtml", null);
            dotvvmconfiguration.RouteTable.Add("Sample12", "Sample12", "sample12.dothtml", null);
            dotvvmconfiguration.RouteTable.Add("Sample13", "Sample13", "sample13.dothtml", null);
            dotvvmconfiguration.RouteTable.Add("Sample14", "Sample14", "sample14.dothtml", null);
            dotvvmconfiguration.RouteTable.Add("Sample15", "Sample15", "sample15.dothtml", null);
            dotvvmconfiguration.RouteTable.Add("Sample16", "Sample16", "sample16.dothtml", null);
            dotvvmconfiguration.RouteTable.Add("Sample17_SPA", "Sample17", "sample17.dothtml", null);
            dotvvmconfiguration.RouteTable.Add("Sample17_A", "Sample17/A/{Id}", "sample17_a.dothtml", null);
            dotvvmconfiguration.RouteTable.Add("Sample17_B", "Sample17/B", "sample17_b.dothtml", null);
            dotvvmconfiguration.RouteTable.Add("Sample18", "Sample18", "sample18.dothtml", null);
            dotvvmconfiguration.RouteTable.Add("Sample19", "Sample19", "sample19.dothtml", null);
            dotvvmconfiguration.RouteTable.Add("AuthSampleLogin", "AuthSample/Login", "AuthSample/login.dothtml", null);
            dotvvmconfiguration.RouteTable.Add("AuthSamplePage", "AuthSample/SecuredPage", "AuthSample/securedPage.dothtml", null);

            var bundles = new BundlingResourceProcessor();
            bundles.RegisterBundle(dotvvmconfiguration.Resources.FindNamedResource("testJsBundle"), "testJs", "testJs2");
            dotvvmconfiguration.Resources.DefaultResourceProcessors.Add(bundles);

            dotvvmconfiguration.Styles.Register("div").SetAttribute("data-testAttribute", "this is attribute was set by styling engine because this element is div");

            dotvvmconfiguration.ServiceLocator.RegisterSingleton<IUploadedFileStorage>(
                () => new FileSystemUploadedFileStorage(Path.Combine(applicationPhysicalPath, "TempUpload"), TimeSpan.FromMinutes(30)));

            // use static files
            app.UseStaticFiles(new StaticFileOptions()
            {
                FileSystem = new PhysicalFileSystem(applicationPhysicalPath)
            });
        }
    }
}
