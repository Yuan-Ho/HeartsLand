using System.Web;
using System.Web.Optimization;

namespace MvcWebRole1
{
	public class BundleConfig
	{
		// 如需 Bundling 的詳細資訊，請造訪 http://go.microsoft.com/fwlink/?LinkId=254725
		public static void RegisterBundles(BundleCollection bundles)
		{
			bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
						"~/Scripts/jquery-{version}.js"));

			bundles.Add(new ScriptBundle("~/bundles/jqueryui").Include(
						"~/Scripts/jquery-ui-{version}.js"));

			bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
						"~/Scripts/jquery.unobtrusive*",
						"~/Scripts/jquery.validate*"));

			// 使用開發版本的 Modernizr 進行開發並學習。然後，當您
			// 準備好實際執行時，請使用 http://modernizr.com 上的建置工具，只選擇您需要的測試。
			bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
						"~/Scripts/modernizr-*"));

			bundles.Add(new StyleBundle("~/Content/css").Include("~/Content/site.css"));

			bundles.Add(new StyleBundle("~/Content/themes/base/css").Include(
						"~/Content/themes/base/jquery.ui.core.css",
						"~/Content/themes/base/jquery.ui.resizable.css",
						"~/Content/themes/base/jquery.ui.selectable.css",
						"~/Content/themes/base/jquery.ui.accordion.css",
						"~/Content/themes/base/jquery.ui.autocomplete.css",
						"~/Content/themes/base/jquery.ui.button.css",
						"~/Content/themes/base/jquery.ui.dialog.css",
						"~/Content/themes/base/jquery.ui.slider.css",
						"~/Content/themes/base/jquery.ui.tabs.css",
						"~/Content/themes/base/jquery.ui.datepicker.css",
						"~/Content/themes/base/jquery.ui.progressbar.css",
						"~/Content/themes/base/jquery.ui.theme.css"));
			//
			//#if DEBUG
			string folder = "";
			//#else
			//			string folder = "closure-compiler/";		// closure compiler with SIMPLE optimization is not smaller than BundleTable.EnableOptimizations.
			//#endif
			bundles.Add(new ScriptBundle("~/bundles/hearts_common").Include(
				"~/Scripts/" + folder + "hearts_util.js",
				"~/Scripts/" + folder + "hearts_component.js"));

			bundles.Add(new ScriptBundle("~/bundles/ViewDiscussion").Include(
				"~/Scripts/" + folder + "ViewDiscussion.js",
				"~/Scripts/crypto-js_3-1-2_core-min.js",
				"~/Scripts/crypto-js_3-1-2_enc-base64-min.js",
				"~/Scripts/crypto-js_3-1-2_aes.js"));
			bundles.Add(new ScriptBundle("~/bundles/ViewDiscussionMap").Include("~/Scripts/" + folder + "ViewDiscussionMap.js"));
			bundles.Add(new ScriptBundle("~/bundles/ViewDiscussionSky").Include("~/Scripts/" + folder + "ViewDiscussionSky.js"));
			bundles.Add(new ScriptBundle("~/bundles/ViewDiscussionScribble").Include("~/Scripts/" + folder + "ViewDiscussionScribble.js"));

			bundles.Add(new ScriptBundle("~/bundles/hearts_horizontal").Include("~/Scripts/" + folder + "hearts_horizontal.js"));
			bundles.Add(new ScriptBundle("~/bundles/hearts_map").Include("~/Scripts/" + folder + "hearts_map.js"));
			bundles.Add(new ScriptBundle("~/bundles/hearts_sky").Include("~/Scripts/" + folder + "hearts_sky.js"));
			bundles.Add(new ScriptBundle("~/bundles/scribble_3rd_party").Include(
				"~/Scripts/" + folder + "paper-core.min.js",
				"~/Scripts/" + folder + "chromoselector.min.js"
				));

			// The path of css bundles should match that of css files so that relative paths of image files are correct.
			bundles.Add(new StyleBundle("~/Content/hearts_common").Include("~/Content/hearts_common.css"));
			bundles.Add(new StyleBundle("~/Content/hearts_horizontal").Include("~/Content/hearts_horizontal.css"));
			bundles.Add(new StyleBundle("~/Content/hearts_map").Include("~/Content/hearts_map.css"));
			bundles.Add(new StyleBundle("~/Content/hearts_sky").Include("~/Content/hearts_sky.css"));
			bundles.Add(new StyleBundle("~/Content/hearts_scribble").Include("~/Content/hearts_scribble.css"));
			bundles.Add(new StyleBundle("~/Content/scribble_3rd_party").Include("~/Content/chromoselector.css"));

			bundles.Add(new StyleBundle("~/Content/themes/hearts/css").Include(
				"~/Content/themes/hearts/jquery.ui.core.css",
				"~/Content/themes/hearts/jquery.ui.resizable.css",
				"~/Content/themes/hearts/jquery.ui.selectable.css",
				"~/Content/themes/hearts/jquery.ui.button.css",
				"~/Content/themes/hearts/jquery.ui.dialog.css",
				"~/Content/themes/hearts/jquery.ui.slider.css",
				"~/Content/themes/hearts/jquery.ui.menu.css",
				"~/Content/themes/hearts/jquery.ui.tooltip.css",
				"~/Content/themes/hearts/jquery.ui.theme.css"));
#if !DEBUG
			BundleTable.EnableOptimizations = true;
#endif
		}
	}
}