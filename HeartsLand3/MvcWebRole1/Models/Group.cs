using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WebMatrix.WebData;
using System.Web.Security;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage;

namespace MvcWebRole1.Models
{
	public static class UserStore
	{
#if OLD
		public static void OnLogin(string user_name, string m_id)
		{
			HttpContext.Current.Application["user1.m_id." + user_name] = m_id;
		}
		public static void OnLogout(string user_name)
		{
			HttpContext.Current.Application.Remove("user1.m_id." + user_name);
		}
		public static string CurrentUserMId
		{
			get
			{
				// When web application is re-started, the browser has no idea and will continue using the login token without logining again.
				// ASP.NET can accept this situation so CurrentUserId is ok, but CurrentUserMId is not.
				// HttpContext.Current.Application returns null for nonexistent key.

				string ret = (string)HttpContext.Current.Application["user1.m_id." + WebSecurity.CurrentUserName];

				if (ret == null)		// not exist
					return MissingUserMId;
				return ret;
			}
		}
#endif
		public static string CurrentUserMId
		{
			get
			{
				HttpCookie cookie = HttpContext.Current.Request.Cookies["m_id"];
				if (cookie != null)
					return cookie.Value;
				return MissingUserMId;
			}
		}
		public const string MissingUserMId = "!missing";

		public static bool IsValidUserName(string user_name)
		{
			return WebSecurity.GetUserId(user_name) != -1;
		}
	}
	public class VoteBookStore
	{
		public static void Vote(string board_id, string discussion_id, string letter_id, string meta_title)
		{
			string partition_key = SandId.CombineId(board_id, discussion_id, letter_id);
			string row_key = SandId.CombineId(meta_title, "uid", WebSecurity.CurrentUserId.ToString());		// CurrentUserId is -1 if not logged in.

			DynamicTableEntity entity = new DynamicTableEntity(partition_key, row_key);
			Warehouse.VoteBookTable.Execute(TableOperation.Insert(entity));		// (409) 衝突。 if already exists.
		}
	}
	public class GroupStore
	{
		public const string ChairOwnerGroupName = SandId.GROUP_ID_PREFIX + "10";
		public const string ViceOwnerGroupName = SandId.GROUP_ID_PREFIX + "11";
		public const string InsiderGroupName = SandId.GROUP_ID_PREFIX + "12";

		public static void UpdateGroup(string board_id, string group_id, string add_users, string remove_users)
		{
			string role_name = SandId.CombineId(board_id, group_id);

			if (!Roles.RoleExists(role_name))
				Roles.CreateRole(role_name);

			string[] ru = remove_users.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

			if (ru.Length > 0)
				Roles.RemoveUsersFromRole(ru, role_name);

			string[] au = add_users.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

			int cnt = Roles.GetUsersInRole(role_name).Length;
			
			int limit = group_id == GroupStore.ChairOwnerGroupName ? 1 : HeartsConfiguration.MAX_NUM_OF_USERS_IN_A_GROUP;
			if (cnt + au.Length > limit)
				Util.ThrowBadRequestException("使用者數量超過" + limit + "個。");

			if (au.Length > 0)
				Roles.AddUsersToRole(au, role_name);

			Warehouse.BoardSettingPond.Notify(board_id);

			string month = Util.DateTimeToString(DateTime.Now, 5);

			RevisionStore.CreateActivity(SandId.CombineId(ActivityType.UpdateGroup.ToString(), board_id, month),
				new string[] { "group_id", "add_users", "remove_users" },
				new object[] { group_id, add_users, remove_users });
		}
		public static string[] GetBoardGroup(string board_id, string group_id, out int[] user_ids)
		{
			string role_name = SandId.CombineId(board_id, group_id);

			string[] user_names = Roles.GetUsersInRole(role_name);		// zero length array for nonexistent role.

			user_ids = UserNamesToUserIds(user_names);

			return user_names;
		}
		private static int[] UserNamesToUserIds(string[] user_names)
		{
			int[] user_ids = new int[user_names.Length];

			for (int i = 0; i < user_names.Length; i++)
				user_ids[i] = WebSecurity.GetUserId(user_names[i]);

			return user_ids;
		}
		public static bool IsChairOwner(string board_id)
		{
			string role_name = SandId.CombineId(board_id, ChairOwnerGroupName);
			return HttpContext.Current.User.IsInRole(role_name);
		}
		public static bool HasChairOwner(string board_id)
		{
			string role_name = SandId.CombineId(board_id, ChairOwnerGroupName);
			return false;
			//2015-11-6 remove sql db. //return Roles.GetUsersInRole(role_name).Length > 0;
		}
		public static bool IsViceOwner(string board_id)
		{
			string role_name = SandId.CombineId(board_id, ViceOwnerGroupName);
			return HttpContext.Current.User.IsInRole(role_name);
		}
		public static bool IsSiteOwner()
		{
			return IsBoardOwner("b1000") || IsBoardOwner("b1012");
		}
		public static bool IsBoardOwner(string board_id)
		{
			if (IsChairOwner(board_id))
				return true;
			return IsViceOwner(board_id);
		}
		public static bool IsInsider(string board_id)
		{
			if (IsBoardOwner(board_id))
				return true;
			string role_name = SandId.CombineId(board_id, InsiderGroupName);
			return HttpContext.Current.User.IsInRole(role_name);
		}
		public static void CheckEditRight(string board_id, string discussion_id, DynamicTableEntity entity)
		{
			Subtype subtype = LetterConverter.GetSubtype(entity);

			if (subtype == Subtype.d)
				Util.ThrowUnauthorizedException("不能編輯的類型。");

			if (!CreatorConverter.IsCurrentUserCreator(entity) &&
				!DiscussionLoadStore.IsCurrentUserDiscussionCreator(board_id, discussion_id) &&
				!IsBoardOwner(board_id))
				Util.ThrowUnauthorizedException("沒有編輯權限。只有副板主以上、串主、或原作者可以編輯。");
		}
		public static void CheckControlRight(string board_id, string discussion_id, string letter_id, ref string delta_flags,
											DynamicTableEntity entity)
		{
			Subtype subtype = LetterConverter.GetSubtype(entity);

			if (subtype == Subtype.d)
				Util.ThrowUnauthorizedException("不能修改的類型。");

			bool auto_unreport = false;

			if (SandFlags.Check(delta_flags, SandFlags.MT_PERMANENT_DELETE, 1))
			{
				GroupStore.CheckPermanentDeleteRight(board_id, discussion_id, letter_id, entity);
				auto_unreport = true;
			}

			int delete_num = SandFlags.GetNumber(delta_flags, SandFlags.MT_DELETED);
			if (delete_num == 1 || delete_num == -1)
			{
				//bool is_undelete = SandFlags.Check(delta_flags, SandFlags.DELETED_FLAG_CHAR, -1);

				int user_level = GroupStore.CheckDeleteRight(board_id, discussion_id, letter_id, entity, delete_num == -1);

				//delta_flags = SandFlags.DELETED_FLAG_CHAR + (delete_num * user_level).ToString();
				//delta_flags = SandFlags.MultiplyNumber(delta_flags, SandFlags.DELETED_FLAG_CHAR, user_level);
				delta_flags = SandFlags.Operate(delta_flags, new FlagOperation
																{
																	type = FlagOperation.Type.Multiply,
																	MetaTitle = SandFlags.MT_DELETED,
																	N = user_level
																});

				string current_flags = entity.GetFlags();
				SandFlags.CheckLevel(current_flags, SandFlags.MT_DELETED, delta_flags);

				auto_unreport = true;
			}
			if (SandFlags.Check(delta_flags, SandFlags.MT_REPORT, 0))
				GroupStore.CheckUnreportRight(board_id);
			else if (SandFlags.Check(delta_flags, SandFlags.MT_REPORT, 1))
			{
			}
			else
			{
				if (auto_unreport)
					delta_flags = SandFlags.Add(delta_flags, SandFlags.MT_REPORT, 0);
			}
		}
		private static int userLevel(string board_id, string discussion_id, DynamicTableEntity entity, bool is_discussion,
									bool is_undelete)
		{
			if (!is_undelete && !is_discussion && CreatorConverter.IsCurrentUserCreator(entity))
				return 99;
			else if (IsSiteOwner())
				return 90;
			else if (IsChairOwner(board_id))
				return 80;
			else if (IsViceOwner(board_id))
				return 60;
			else if (DiscussionLoadStore.IsCurrentUserDiscussionCreator(board_id, discussion_id))
				return 30;
			else
				return 20;
		}
		public static int CheckDeleteRight(string board_id, string discussion_id, string letter_id, DynamicTableEntity entity,
											bool is_undelete)
		{
			bool is_discussion = letter_id == SandId.HEADING_LETTER_ID;
			string cmd_name = is_undelete ? "復原" : "刪除";

			int user_level = userLevel(board_id, discussion_id, entity, is_discussion, is_undelete);
			int required_level = is_discussion ? 50 : (is_undelete ? 30 : 20);

			if (user_level < required_level)
				Util.ThrowUnauthorizedException("沒有" + cmd_name + (is_discussion ? "討論串" : "留言") +
												"權限。需要權限等級" + required_level +
												"，您的權限等級為" + user_level + "。");
			return user_level;
#if OLD
			if (is_undelete || is_discussion)
			{
				if (!IsBoardOwner(board_id))
					Util.ThrowUnauthorizedException("沒有" + cmd_name + "權限。只有板主可以" + cmd_name + (is_discussion ? "討論串" : "留言") + "。");
			}
			else
			{
				if (!CreatorConverter.IsCurrentUserCreator(entity) &&
					!DiscussionLoadStore.IsCurrentUserDiscussionCreator(board_id, discussion_id) &&
					!IsBoardOwner(board_id))
				{
					// Util.ThrowUnauthorizedException("沒有刪除權限。只有板主、串主、或原作者可以刪除留言。");
				}
			}
#endif
		}
		public static void CheckPermanentDeleteRight(string board_id, string discussion_id, string letter_id, DynamicTableEntity entity)
		{
			bool is_discussion = letter_id == SandId.HEADING_LETTER_ID;

			if (is_discussion)
				throw new ProgramLogicException();

			if (!CreatorConverter.IsCurrentUserCreator(entity))
				Util.ThrowUnauthorizedException("沒有刪除權限。只有原作者可以永久刪除留言。");
		}
		public static void CheckUnreportRight(string board_id)
		{
			if (!IsBoardOwner(board_id))
				Util.ThrowUnauthorizedException("沒有取消檢舉權限。只有副板主以上可以取消檢舉留言。");
		}
	}
}