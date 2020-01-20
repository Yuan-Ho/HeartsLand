using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using MvcWebRole1.Models;

namespace MvcWebRole1
{
	public class IdConstraint : IRouteConstraint
	{
		private char Identifier;

		public IdConstraint(char id)
		{
			this.Identifier = id;
		}
		public bool Match(HttpContextBase httpContext, Route route, string parameterName, RouteValueDictionary values, RouteDirection routeDirection)
		{
			string value = (string)values[parameterName];
			// Value won't be empty string but may be UrlParameter.Optional.
			return value[0] == this.Identifier && Util.IsNumber(value, 1);
		}
	}

	public class RouteConfig
	{
		public static void RegisterRoutes(RouteCollection routes)
		{
			routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

			routes.MapRoute(
				"View discussion",
				"{level_1_id}/{level_2_id}",
				new { controller = "Hearts", action = "ViewDiscussion" },
				new { level_1_id = new IdConstraint(SandId.BOARD_ID_CHAR), level_2_id = new IdConstraint(SandId.DISCUSSION_ID_CHAR) }
			);

			routes.MapRoute(
				"View board",
				"{level_1_id}/{any_thing}",
				new { controller = "Hearts", action = "ViewBoard", any_thing = UrlParameter.Optional },
				new { level_1_id = new IdConstraint(SandId.BOARD_ID_CHAR) }
			);

			routes.MapRoute(
				"View selection",
				"{level_1_id}/{any_thing}",
				new { controller = "Hearts", action = "ViewSelection", any_thing = UrlParameter.Optional },
				new { level_1_id = new IdConstraint(SandId.SELECTION_ID_CHAR) }
			);

			routes.MapRoute(
				name: "Account",
				url: "Account/{action}",
				defaults: new { controller = "Account" }
			);

			routes.MapRoute(
				"Hearts",
				"{action}/{level_1_id}/{level_2_id}",
				new { controller = "Hearts", action = "Home", level_1_id = UrlParameter.Optional, level_2_id = UrlParameter.Optional }
			);
#if OLD
			routes.MapRoute(
				name: "Default",
				url: "{controller}/{action}/{id}",
				defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
			);
#endif
		}
	}
}