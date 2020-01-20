using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MvcWebRole1.Models;
using MvcWebRole1.Filters;
using System.Web.UI;
using Microsoft.Web.Helpers;
using System.Diagnostics;

namespace MvcWebRole1.Controllers
{
	public class HeartsController : Controller
	{
		private static void checkLetterInput(string creator, string words, bool is_edit)
		{
			if (creator.Length < HeartsConfiguration.CREATOR_FIELD_MIN_LEN) Util.ThrowBadRequestException("名字長度過短。");
			else if (creator.Length > HeartsConfiguration.CREATOR_FIELD_MAX_LEN) Util.ThrowBadRequestException("名字長度過長。");
			else if (!Util.WithinCharSetUserName(creator)) Util.ThrowBadRequestException("名字含有不合法的字元。");
			else if (creator == "RETURN_FAIL") Util.ThrowBadRequestException("名字為RETURN_FAIL。");

			if (words.Length > (is_edit ? HeartsConfiguration.WORDS_FIELD_EDIT_MAX_LEN : HeartsConfiguration.WORDS_FIELD_INPUT_LIMIT))
				Util.ThrowBadRequestException("內容長度過長。");
			else if (words.Length == 0) Util.ThrowBadRequestException("內容長度過短。");
		}
		private static void checkEditFlags(string delta_flags, bool is_heading)
		{
			string[] allowed_flags = null;
			string[] allowed_meta_titles = null;

			if (is_heading)
			{
				allowed_flags = new string[] {	SandFlags.MT_LAYOUT + SandFlags.MTV_SEPARATOR + "0",
												SandFlags.MT_LAYOUT + SandFlags.MTV_SEPARATOR + "1",
												SandFlags.MT_LAYOUT + SandFlags.MTV_SEPARATOR + "3",
												SandFlags.MT_LAYOUT + SandFlags.MTV_SEPARATOR + "4",
												SandFlags.MT_REPLY_TO + SandFlags.MTV_SEPARATOR + "0",
												SandFlags.MT_VIEW + SandFlags.MTV_SEPARATOR + "2",
												SandFlags.MT_VIEW + SandFlags.MTV_SEPARATOR + "3",
												SandFlags.MT_VIEW + SandFlags.MTV_SEPARATOR + "4"};

				allowed_meta_titles = new string[] { SandFlags.MT_COORDINATE };
			}
			else
			{
				allowed_flags = new string[] {	SandFlags.MT_LAYOUT + SandFlags.MTV_SEPARATOR + "0",
												SandFlags.MT_LAYOUT + SandFlags.MTV_SEPARATOR + "1",
												SandFlags.MT_LAYOUT + SandFlags.MTV_SEPARATOR + "2",
												SandFlags.MT_LAYOUT + SandFlags.MTV_SEPARATOR + "3",
												SandFlags.MT_LAYOUT + SandFlags.MTV_SEPARATOR + "4",
												SandFlags.MT_AUTHORIZATION + SandFlags.MTV_SEPARATOR + "2",
												SandFlags.MT_LETTER_TYPE + SandFlags.MTV_SEPARATOR + "1" };

				allowed_meta_titles = new string[] { SandFlags.MT_REPLY_TO, SandFlags.MT_COORDINATE };
			}

			checkFlags(delta_flags, allowed_meta_titles, allowed_flags);
		}
		private static void checkControlFlags(string delta_flags, bool is_heading)
		{
			string[] allowed_flags = null;

			if (is_heading)
			{
				allowed_flags = new string[] {	SandFlags.MT_DELETED + SandFlags.MTV_SEPARATOR + "-1",
												SandFlags.MT_DELETED + SandFlags.MTV_SEPARATOR + "1",
												SandFlags.MT_REPORT + SandFlags.MTV_SEPARATOR + "1",
												SandFlags.MT_REPORT + SandFlags.MTV_SEPARATOR + "0" };
			}
			else
			{
				allowed_flags = new string[] {	SandFlags.MT_DELETED + SandFlags.MTV_SEPARATOR + "-1",
												SandFlags.MT_DELETED + SandFlags.MTV_SEPARATOR + "1",
												SandFlags.MT_PERMANENT_DELETE + SandFlags.MTV_SEPARATOR + "1",
												SandFlags.MT_REPORT + SandFlags.MTV_SEPARATOR + "1",
												SandFlags.MT_REPORT + SandFlags.MTV_SEPARATOR + "0" };
			}
			checkFlags(delta_flags, null, allowed_flags);
		}
		private static void checkFlags(string delta_flags, string[] allowed_meta_titles, params string[] allowed_flags)
		{
			if (delta_flags.Length > 0)
			{
				if (delta_flags[0] != SandFlags.FLAGS_SEPARATOR || delta_flags[delta_flags.Length - 1] != SandFlags.FLAGS_SEPARATOR)
					Util.ThrowBadRequestException("Flags格式不正確。");

				if (!SandFlags.CheckWithinAllowed(delta_flags, allowed_flags, allowed_meta_titles))
					Util.ThrowBadRequestException("未允許的Flags。");
			}
		}
		private static void checkBoardName(string board_name)
		{
			if (board_name.Length < HeartsConfiguration.BOARD_NAME_FIELD_MIN_LEN) Util.ThrowBadRequestException("留言板名長度過短。");
			else if (board_name.Length > HeartsConfiguration.BOARD_NAME_FIELD_MAX_LEN) Util.ThrowBadRequestException("留言板名長度過長。");
			else if (!Util.WithinCharSetUserName(board_name)) Util.ThrowBadRequestException("留言板名含有不合法的字元。");
			else if (board_name == "RETURN_FAIL") Util.ThrowBadRequestException("留言板名為RETURN_FAIL。");
		}
		private static void checkHeading(string heading)
		{
			if (heading.Length < HeartsConfiguration.HEADING_FIELD_MIN_LEN) Util.ThrowBadRequestException("標題長度過短。");
			else if (heading.Length > HeartsConfiguration.HEADING_FIELD_MAX_LEN) Util.ThrowBadRequestException("標題長度過長。");
			else if (heading == "RETURN_FAIL") Util.ThrowBadRequestException("標題為RETURN_FAIL。");
		}
		private string currentPageUrl()
		{
			return "http://www.hela.cc" + Url.Action();
			// this will include query string. ie. ?view=map&fb_action_ids=673715842705270&fb_action_types=og.shares&fb_source=aggregation&fb_aggregation_id=288381481237582
		}
		[HttpPost]
		public ActionResult CreateBoard(string board_name)
		{
			if (!Util.IsAjaxRequest(Request)) Util.ThrowBadRequestException("Not ajax post.");

			if (!ReCaptcha.Validate()) Util.ThrowBadRequestException("驗證碼不正確。");

			checkBoardName(board_name);

			string board_id = BoardInfoStore.CreateBoard(board_name + '板');
			return Json(new { board_id = board_id });
		}
		[HttpPost]
		public ActionResult CreateSelection(string selection_name)
		{
			if (!Util.IsAjaxRequest(Request)) Util.ThrowBadRequestException("Not ajax post.");

			if (!ReCaptcha.Validate()) Util.ThrowBadRequestException("驗證碼不正確。");

			if (selection_name.Length < HeartsConfiguration.BOARD_NAME_FIELD_MIN_LEN) Util.ThrowBadRequestException("區域名長度過短。");
			else if (selection_name.Length > HeartsConfiguration.BOARD_NAME_FIELD_MAX_LEN) Util.ThrowBadRequestException("區域名長度過長。");
			else if (!Util.WithinCharSetUserName(selection_name)) Util.ThrowBadRequestException("區域名含有不合法的字元。");
			else if (selection_name == "RETURN_FAIL") Util.ThrowBadRequestException("區域名為RETURN_FAIL。");

			string selection_id = SelectionInfoStore.CreateSelection(selection_name + '區');
			return Json(new { selection_id = selection_id });
		}
		[HttpPost]
		public ActionResult CreateDiscussion(string board_id, string creator, string words, string heading, string delta_flags, string heading_delta_flags)
		{
			if (!Util.IsAjaxRequest(Request)) Util.ThrowBadRequestException("Not ajax post.");

			object ret_obj = Warehouse.RateLimiter.Validate(CarryType.CreateDiscussion);
			if (ret_obj != null) return Json(ret_obj);

			checkHeading(heading);

			checkLetterInput(creator, words, false);

			delta_flags = delta_flags.Replace("\r\n", "\n");		// 跟圖片一起上傳時（使用multipart/form-data），Chrome會帶\r\n做為換行。其它時候都是\n。IE無此問題。
			heading_delta_flags = heading_delta_flags.Replace("\r\n", "\n");

			checkEditFlags(delta_flags, false);
			checkEditFlags(heading_delta_flags, true);

			string discussion_id = DiscussionListStore.CreateDiscussion(board_id, creator, words, heading, delta_flags, Request.Files, heading_delta_flags);
			return Json(new { discussion_id = discussion_id });
		}
		[HttpPost]
		public ActionResult CreateLetter(string board_id, string discussion_id, string creator, string words, string delta_flags)
		{
			if (!Util.IsAjaxRequest(Request)) Util.ThrowBadRequestException("Not ajax post.");

			object ret_obj = Warehouse.RateLimiter.Validate(CarryType.CreateLetter);
			if (ret_obj != null) return Json(ret_obj);

			checkLetterInput(creator, words, false);

			delta_flags = delta_flags.Replace("\r\n", "\n");
			checkEditFlags(delta_flags, false);

			Subtype subtype = Subtype.r;
			if (DiscussionLoadStore.IsCurrentUserDiscussionCreator(board_id, discussion_id))
				subtype = Subtype.s;

			string letter_id = DiscussionLoadStore.CreateLetter(board_id, discussion_id, creator, words, subtype, delta_flags, Request.Files);
			return Json(new { letter_id = letter_id });
		}
		[HttpPost]
		public ActionResult EditLetter(string board_id, string discussion_id, string letter_id, string creator,
										string words, string delta_flags)
		{
			if (!Util.IsAjaxRequest(Request)) Util.ThrowBadRequestException("Not ajax post.");

			object ret_obj = Warehouse.RateLimiter.Validate(CarryType.EditLetter);
			if (ret_obj != null) return Json(ret_obj);

			checkLetterInput(creator, words, true);

			delta_flags = delta_flags.Replace("\r\n", "\n");

			if (letter_id == SandId.HEADING_LETTER_ID)
			{
				checkHeading(words);
				checkEditFlags(delta_flags, true);
			}
			else
				checkEditFlags(delta_flags, false);

			DiscussionLoadStore.EditLetter(board_id, discussion_id, letter_id, creator, words, delta_flags, Request.Files);

			if (letter_id == SandId.HEADING_LETTER_ID)
				DiscussionListStore.EditHeading(board_id, discussion_id, words);

			return Json(new { letter_id = letter_id });
		}
		[HttpPost]
		public ActionResult ControlLetter(string board_id, string discussion_id, string letter_id, string delta_flags, string reason)
		{
			if (!Util.IsAjaxRequest(Request)) Util.ThrowBadRequestException("Not ajax post.");

			object ret_obj = Warehouse.RateLimiter.Validate(CarryType.ControlLetter);
			if (ret_obj != null) return Json(ret_obj);

			checkControlFlags(delta_flags, letter_id == SandId.HEADING_LETTER_ID);
			if (reason.Length > 500 * HeartsConfiguration.LENGTH_CHECK_MARGIN/*counting into foreword*/)
				Util.ThrowBadRequestException("理由長度過長。");

			ControlHistory mh = DiscussionLoadStore.ControlLetter(board_id, discussion_id, letter_id, delta_flags);
			if (letter_id == SandId.HEADING_LETTER_ID)
			{
				delta_flags = SandFlags.Remove(delta_flags, SandFlags.MT_REPORT);
				DiscussionListStore.OperateFlags(board_id, discussion_id, new FlagMergeOperation(delta_flags));
			}
			if (mh.ReportCount != 0)
				DiscussionListStore.OperateFlags(board_id, discussion_id, new FlagOperation
																			{
																				type = FlagOperation.Type.Add,
																				MetaTitle = SandFlags.MT_REPORT,
																				N = mh.ReportCount
																			});

			int id_num = SandId.ExtractIdNumber(letter_id);
			string remark_delta_flags = SandFlags.Add(string.Empty, SandFlags.MT_REPLY_TO, id_num);
			string remark_letter_id = DiscussionLoadStore.CreateLetter(board_id, discussion_id, null, reason, Subtype.d, remark_delta_flags, null);
			// while deleting/undeleting discussion, the remark_delta_flags is ,r0, and will be removed.

			return Json(new { ok = true });
		}
		[HttpPost]
		public ActionResult VoteLetter(string board_id, string discussion_id, string letter_id, string delta_flags)
		{
			if (!Util.IsAjaxRequest(Request)) Util.ThrowBadRequestException("Not ajax post.");

			object ret_obj = Warehouse.RateLimiter.Validate(CarryType.VoteLetter);
			if (ret_obj != null) return Json(ret_obj);

			//checkFlags(delta_flags, 2, letter_id == SandId.HEADING_LETTER_ID);
			checkFlags(delta_flags,
						null,
						SandFlags.MT_ILI + SandFlags.MTV_SEPARATOR + "1",
						SandFlags.MT_IDLI + SandFlags.MTV_SEPARATOR + "1");

			if (!DiscussionLoadStore.VoteLetter(board_id, discussion_id, letter_id, delta_flags))
				return Json(new { err_msg = "您已經投過票了。" });
				// return new ErrorResult("您已經投過票了。");

			return Json(new { ok = true });
		}
		[HttpPost]
		public ActionResult daUpgradeTable()
		{
			if (!Util.IsAjaxRequest(Request)) Util.ThrowBadRequestException("Not ajax post.");

			TableUpgrader.Upgrade();
			//System.Threading.Thread.Sleep(2000);
			//Util.ThrowBadRequestException("RETURN_FAIL");

			return Json(new { ok = true });
		}
		[HttpPost]
		public ActionResult SelectionSetting(string level_1_id/*selection_id*/, string board_list)
		{
			if (!Util.IsAjaxRequest(Request)) Util.ThrowBadRequestException("Not ajax post.");
			if (!ReCaptcha.Validate()) Util.ThrowBadRequestException("驗證碼不正確。");

			int cnt = SandId.CountBoardList(board_list);

			if (cnt == -1)
				Util.ThrowBadRequestException("留言板ID格式不正確。");
			else if (cnt == 0)
				Util.ThrowBadRequestException("未包含任何留言板ID。");
			else if (cnt > HeartsConfiguration.MAX_NUM_OF_BOARDS_IN_A_SELECTION)
				Util.ThrowBadRequestException("留言板數量超過" + HeartsConfiguration.MAX_NUM_OF_BOARDS_IN_A_SELECTION + "個。");

			SelectionInfoStore.SetSelectionSetting(level_1_id, board_list);

			return Json(new { ok = true });
		}
		[HttpPost]
		public ActionResult BoardSetting(string level_1_id/*board_id*/, string board_name, string group_id,
			string add_users, string remove_users, string delta_flags)
		{
			if (!Util.IsAjaxRequest(Request)) Util.ThrowBadRequestException("Not ajax post.");
			if (!ReCaptcha.Validate()) Util.ThrowBadRequestException("驗證碼不正確。");

			if (board_name != null)
			{
				if (!GroupStore.IsChairOwner(level_1_id))
					Util.ThrowUnauthorizedException("只有板主可以變更板名。");

				checkBoardName(board_name);

				BoardInfoStore.SetBoardSetting(level_1_id, board_name + '板');
			}
			else if (delta_flags != null)
			{
				if (GroupStore.HasChairOwner(level_1_id) && !GroupStore.IsChairOwner(level_1_id) && !GroupStore.IsSiteOwner())
					Util.ThrowUnauthorizedException("只有板主可以變更留言板設定。");

				checkFlags(delta_flags,
							null,
							SandFlags.MT_LOW_KEY + SandFlags.MTV_SEPARATOR + "0",
							SandFlags.MT_LOW_KEY + SandFlags.MTV_SEPARATOR + "1");

				BoardInfoStore.SetBoardFlags(level_1_id, delta_flags);
			}
			else if (group_id != null && add_users != null && remove_users != null)
			{
				if (GroupStore.HasChairOwner(level_1_id) && !GroupStore.IsChairOwner(level_1_id))
					Util.ThrowUnauthorizedException("只有板主可以變更板主、副板主、或內部群組列表。");

				if (group_id != GroupStore.ChairOwnerGroupName &&
					group_id != GroupStore.ViceOwnerGroupName &&
					group_id != GroupStore.InsiderGroupName)
					Util.ThrowBadRequestException("群組ID格式不正確。");

				int add_cnt = SandId.CountUserNameList(add_users);
				int remove_cnt = SandId.CountUserNameList(remove_users);

				if (!Warehouse.BsMapPond.Get().IsValidBoardId(level_1_id))
					Util.ThrowBadRequestException("Invalid board ID.");

				GroupStore.UpdateGroup(level_1_id, group_id, add_users, remove_users);
			}
			return Json(new { ok = true });
		}
		[OutputCache(Duration = HeartsConfiguration.SHORT_CACHE_SECONDS, VaryByParam = "none")]
		[HttpGet]
		public ActionResult Home()
		{
			return View();
		}
		[OutputCache(Duration = HeartsConfiguration.DEFAULT_CACHE_SECONDS, VaryByParam = "none")]
		[HttpGet]
		public ActionResult CreateBoard()
		{
			return View();
		}
		[OutputCache(Duration = HeartsConfiguration.DEFAULT_CACHE_SECONDS, VaryByParam = "none")]
		[HttpGet]
		public ActionResult CreateSelection()
		{
			return View();
		}
		[OutputCache(Duration = HeartsConfiguration.DEFAULT_CACHE_SECONDS/*, Location = OutputCacheLocation.Downstream*/, VaryByParam = "view")]		// no caching in server because there will be too many copies.
		[HttpGet]
		public ActionResult CreateDiscussion(string level_1_id/*board_id*/)
		{
			string query_view = Request.QueryString["view"];

			if (query_view == "map")
				return View("CreateDiscussionMap");
			else if (query_view == "sky")
				return View("CreateDiscussionSky");
			else
				return View();
		}
		[OutputCache(Duration = HeartsConfiguration.SHORT_CACHE_SECONDS, VaryByParam = "none")]
		[HttpGet]
		public ActionResult ViewBoard(string level_1_id/*board_id*/)
		{
			string board_name = Warehouse.BoardSettingPond.Get(level_1_id).BoardName;
			ViewBag.Title = board_name + " " + HeartsConfiguration.SITE_NAME;

			return View();
		}
		[OutputCache(Duration = HeartsConfiguration.SHORT_CACHE_SECONDS, VaryByParam = "none")]
		[HttpGet]
		public ActionResult ViewSelection(string level_1_id/*selection_id*/)
		{
			string selection_name = Warehouse.BsMapPond.Get().GetSelectionName(level_1_id);
			ViewBag.Title = selection_name + " " + HeartsConfiguration.SITE_NAME;

			return View();
		}
		[OutputCache(Duration = HeartsConfiguration.SHORT_CACHE_SECONDS, VaryByParam = "view")]		// 也會cache child action的結果，所以時間不能比child action長。
		[HttpGet]
		public ActionResult ViewDiscussion(string level_1_id/*board_id*/, string level_2_id/*discussion_id*/)
		{
			//ViewBag.CurrentPageUrl = currentPageUrl();
			ViewBag.CurrentPageUrl = "http://www.hela.cc/" + level_1_id + '/' + level_2_id;

			ViewBag.DiscussionLoadRoll = Warehouse.DiscussionLoadPond.Get(level_1_id, level_2_id);

			string board_name = Warehouse.BoardSettingPond.Get(level_1_id).BoardName;
			ViewBag.Title = ViewBag.DiscussionLoadRoll.Heading + " " + board_name + " " + HeartsConfiguration.SITE_NAME;

			string query_view = Request.QueryString["view"];

			if (query_view == "map")
			{
				ViewBag.CurrentPageUrl += "?view=map";
				return View("ViewDiscussionMap");
			}
			else if (query_view == "sky")
			{
				ViewBag.CurrentPageUrl += "?view=sky";
				return View("ViewDiscussionSky");
			}
			else if (query_view == "scb")
			{
				ViewBag.CurrentPageUrl += "?view=scb";
				return View("ViewDiscussionScribble");
			}
			else
				return View();
		}
		[OutputCache(Duration = HeartsConfiguration.DEFAULT_CACHE_SECONDS, VaryByParam = "none")]
		[HttpGet]
		public ActionResult ControlSite()
		{
			return View();
		}
		[OutputCache(Duration = HeartsConfiguration.DEFAULT_CACHE_SECONDS, VaryByParam = "none")]
		[HttpGet]
		public ActionResult ControlSelection(string level_1_id/*selection_id*/)
		{
			string selection_name = Warehouse.BsMapPond.Get().GetSelectionName(level_1_id);
			ViewBag.Title = selection_name + " 區域控制台 " + HeartsConfiguration.SITE_NAME;

			return View();
		}
		[OutputCache(Duration = HeartsConfiguration.SHORT_CACHE_SECONDS, VaryByParam = "none")]
		[HttpGet]
		public ActionResult ControlBoard(string level_1_id/*board_id*/)
		{
			string board_name = Warehouse.BoardSettingPond.Get(level_1_id).BoardName;
			ViewBag.Title = board_name + " 留言板控制台 " + HeartsConfiguration.SITE_NAME;

			return View();
		}
		[ChildActionOnly]
		[OutputCache(Duration = HeartsConfiguration.SHORT_CACHE_SECONDS/*, VaryByParam = "none"*//*, Location = OutputCacheLocation.Server*/)]
		[HttpGet]
		public ActionResult BoardList()
		{
			//Response.Cache.SetAllowResponseInBrowserHistory(true);		// with this line, the response header will not include "Expires: -1".
			return new SelectionBoardListResult();
		}
		[ChildActionOnly]
		[OutputCache(Duration = HeartsConfiguration.SHORT_CACHE_SECONDS/*, VaryByParam = "none"*//*, Location = OutputCacheLocation.Server*/)]
		// The VaryByParam = "none" causes level_1_id to be ignored regarding selecting cached version.
		// http://weblogs.asp.net/scottgu/announcing-asp-net-mvc-3-release-candidate-2 Output Caching Improvements
		[HttpGet]
		public ActionResult DiscussionList(string level_1_id/*board_id or selection_id*/)
		{
			//Response.Cache.SetAllowResponseInBrowserHistory(true);
#if OLD
			object result = DiscussionListStore.GetDiscussionList(level_1_id);
			return Json(result, JsonRequestBehavior.AllowGet);
#else
			return new DiscussionListResult(level_1_id);
#endif
		}
		//[ChildActionOnly]
		[OutputCache(Duration = HeartsConfiguration.DEFAULT_CACHE_SECONDS, VaryByParam = "none", Location = OutputCacheLocation.Server)]
		//[OutputCache(Duration = HeartsConfiguration.SHORT_CACHE_SECONDS, VaryByParam = "none")]
		[HttpGet]
		public ActionResult DiscussionLoad(string level_1_id/*board_id*/, string level_2_id/*discussion_id*/)
		{
			return new DiscussionLoadResult(level_1_id, level_2_id);
		}
		public ActionResult RefreshLoad(string level_1_id/*board_id*/, string level_2_id/*discussion_id*/, string lmt)
		{
			DiscussionLoadRoll dlr = Warehouse.DiscussionLoadPond.Get(level_1_id, level_2_id);

			if (lmt == Util.DateTimeToString(dlr.LastModifiedTime, 6))
				return Content("");
			else
				return Redirect("/discussionload/" + level_1_id + "/" + level_2_id);
		}
		[ChildActionOnly]
		[OutputCache(Duration = HeartsConfiguration.SHORT_CACHE_SECONDS/*, VaryByParam = "none"*/)]
		// The VaryByParam = "none" causes level_1_id to be ignored regarding selecting cached version.
		// http://weblogs.asp.net/scottgu/announcing-asp-net-mvc-3-release-candidate-2 Output Caching Improvements
		[HttpGet]
		public ActionResult BldSummary(string level_1_id/*board_id or selection_id*/)
		{
			BsMap bs_map = Warehouse.BsMapPond.Get();

			if (!bs_map.IsValidBoardId(level_1_id) && !bs_map.IsValidSelectionId(level_1_id))
				Util.ThrowBadRequestException("無效的board id或selection id。");

			return new LatestDiscussionSummaryResult(level_1_id);
		}
		[ChildActionOnly]
		[OutputCache(Duration = HeartsConfiguration.SHORT_CACHE_SECONDS/*, VaryByParam = "none"*/)]
		[HttpGet]
		public ActionResult SldSummary()
		{
			return new LatestDiscussionSummaryResult(null);
		}
		[HttpGet]
		public ActionResult DiscussionKey(string level_1_id/*board_id*/, string level_2_id/*discussion_id*/)
		{
			if (!GroupStore.IsInsider(level_1_id))
				Util.ThrowUnauthorizedException("只有內部群組成員可以看到不公開的留言。");

			string key = Warehouse.DiscussionKeyPond.Get(level_1_id, level_2_id, false);

			return Json(new { key = key }, JsonRequestBehavior.AllowGet);
		}
		[HttpGet]
		public ActionResult SelectionSetting(string level_1_id/*selection_id*/)
		{
			object result = SelectionInfoStore.GetSelectionSetting(level_1_id);
			return Json(result, JsonRequestBehavior.AllowGet);
		}
		[ChildActionOnly]
		[OutputCache(Duration = HeartsConfiguration.SHORT_CACHE_SECONDS/*, VaryByParam = "none"*/)]
		[HttpGet]
		public ActionResult BoardSetting(string level_1_id/*board_id*/)
		{
#if OLD
			object result = BoardInfoStore.GetBoardSetting(level_1_id);
			return Json(result, JsonRequestBehavior.AllowGet);
#endif
			// TODO: check if level_1_id is a valid board.

			return Content(Warehouse.BoardSettingPond.Get(level_1_id).Output);
		}
		[OutputCache(Duration = HeartsConfiguration.STATIC_CACHE_SECONDS, VaryByParam = "none")]
		[HttpGet]
		public ActionResult Images1(string level_1_id, string level_2_id)
		{
			Trace.TraceInformation("要求圖片。blobName={0}/{1}, UrlReferrer={2}.", level_1_id, level_2_id, Request.UrlReferrer);
			return new DownloadBlobResult(level_1_id, level_2_id);
		}
	}
}
