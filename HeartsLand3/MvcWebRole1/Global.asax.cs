using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using MvcWebRole1.Models;
using System.Timers;
using Microsoft.Web.Helpers;
using System.Configuration;

namespace MvcWebRole1
{
	// 注意: 如需啟用 IIS6 或 IIS7 傳統模式的說明，
	// 請造訪 http://go.microsoft.com/?LinkId=9394801

	public class MvcApplication : System.Web.HttpApplication
	{
		protected void Application_Start()
		{
			AreaRegistration.RegisterAllAreas();

			WebApiConfig.Register(GlobalConfiguration.Configuration);
			FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
			RouteConfig.RegisterRoutes(RouteTable.Routes);
			BundleConfig.RegisterBundles(BundleTable.Bundles);
			AuthConfig.RegisterAuth();
			//
			Warehouse.Initialize();
			ReCaptcha.PrivateKey = ConfigurationManager.AppSettings["ReCaptchaPrivateKey"];
			//
			Timer timer;
#if DEBUG
			timer = new Timer(3000);

			timer.Elapsed += this.unitTest;

			timer.AutoReset = false;
			timer.Enabled = true;
#endif
			//
#if DEBUG
			timer = new Timer(60 * 1000);
#else
			timer = new Timer(5 * 60 * 1000);
#endif

			timer.Elapsed += this.saveState;

			timer.AutoReset = false;
			timer.Enabled = true;
		}
		private void unitTest(object source, ElapsedEventArgs e)
		{
			MyTest.TestMain();
		}
		private void saveState(object source, ElapsedEventArgs e)
		{
			// will re-entry.

			// LatestDiscussionRolls.SaveState();

			Timer timer = (Timer)source;
			timer.Enabled = true;
		}
	}
}