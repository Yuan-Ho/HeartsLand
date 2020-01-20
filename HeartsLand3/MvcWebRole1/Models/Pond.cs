using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Caching;

namespace MvcWebRole1.Models
{
	public class HotDiscussionRollPond
	{
		private BubblingRoll insertNew(string key, string id, int list_size)
		{
			BubblingRoll obj = new BubblingRoll(id, list_size);		// if concurrency happens, this will be done more than once.

			// todo: don't insert into cache for nonexistent board id (bad user request).

			HttpRuntime.Cache.Insert(key, obj, null, DateTime.Now.AddSeconds(HeartsConfiguration.DEFAULT_CACHE_SECONDS),
											Cache.NoSlidingExpiration, CacheItemPriority.Default, removedCallback);

			// expires after some time for multi-node synchronization (unreliable).
			return obj;
		}
		private void removedCallback(string key, Object value, CacheItemRemovedReason reason)
		{
			BubblingRoll obj = (BubblingRoll)value;
			obj.Save();		// if the application is shutdown before save, the change is lost. (ie. local test server)

			if (reason == CacheItemRemovedReason.Expired)
			{
				insertNew(key, obj.Id, obj.ListSize);
			}
		}
		private BubblingRoll get(string id, int list_size)
		{
			string key = SandId.CombineId(BubblingRoll.PAR_KEY_PREFIX, id);

			BubblingRoll obj = (BubblingRoll)HttpRuntime.Cache.Get(key);

			if (obj == null)
				obj = insertNew(key, id, list_size);

			return obj;
		}
		//
		public BubblingRoll Get(string board_id/*null for whole site*/)
		{
			return get(board_id == null ? "wholesite" : board_id, 10/*40*/);
		}
		public void Put(string board_id, string discussion_id)
		{
			string combined_id = SandId.CombineId(board_id, discussion_id);

			Get(board_id).Put(combined_id);

			if (!Warehouse.BoardSettingPond.Get(board_id).LowKey)
				Get(null).Put(combined_id);
			//
			List<string> selection_list = Warehouse.BsMapPond.Get().GetSelectionList(board_id);
			if (selection_list != null)
				foreach (string selection_id in selection_list)
				{
					Get(selection_id).Put(combined_id);
				}
		}
	}
	public class DiscussionSummaryPond
	{
		private DiscussionSummary insertNew(string key, string board_id, string discussion_id)
		{
			DiscussionSummary obj = new DiscussionSummary(board_id, discussion_id);		// if concurrency happens, this will be done more than once.

			HttpRuntime.Cache.Insert(key, obj, null, DateTime.Now.AddSeconds(HeartsConfiguration.DEFAULT_CACHE_SECONDS),
											Cache.NoSlidingExpiration, CacheItemPriority.Default, removedCallback);

			// expires after some time for multi-node synchronization (unreliable).

			return obj;
		}
		private void removedCallback(string key, Object value, CacheItemRemovedReason reason)
		{
			if (reason == CacheItemRemovedReason.Expired)
			{
				DiscussionSummary obj = (DiscussionSummary)value;
				insertNew(key, obj.BoardId, obj.DiscussionId);
			}
		}
		public void Notify(string board_id, string discussion_id)
		{
			string key = SandId.CombineId("summary1", board_id, discussion_id);
			DiscussionSummary obj = (DiscussionSummary)HttpRuntime.Cache.Remove(key);

			if (obj != null)
				insertNew(key, obj.BoardId, obj.DiscussionId);
			// todo: remove only if the edited letter is in the DiscussionSummary.
		}
		public DiscussionSummary Get(string board_id, string discussion_id)
		{
			string key = SandId.CombineId("summary1", board_id, discussion_id);

			DiscussionSummary obj = (DiscussionSummary)HttpRuntime.Cache.Get(key);

			if (obj == null)
				obj = insertNew(key, board_id, discussion_id);

			return obj;
		}
	}
	public class DiscussionListPond
	{
		private DiscussionList insertNew(string key, string board_id)
		{
			DiscussionList obj = new DiscussionList(board_id);		// if concurrency happens, this will be done more than once.

			HttpRuntime.Cache.Insert(key, obj, null, DateTime.Now.AddSeconds(HeartsConfiguration.DEFAULT_CACHE_SECONDS),
											Cache.NoSlidingExpiration, CacheItemPriority.Default, removedCallback);

			// expires after some time for multi-node synchronization (unreliable).

			return obj;
		}
		private void removedCallback(string key, Object value, CacheItemRemovedReason reason)
		{
			if (reason == CacheItemRemovedReason.Expired)
			{
				DiscussionList obj = (DiscussionList)value;
				insertNew(key, obj.BoardId);
			}
		}
		public void Notify(string board_id)
		{
			string key = SandId.CombineId("dlpond1", board_id);
			DiscussionList obj = (DiscussionList)HttpRuntime.Cache.Remove(key);

			if (obj != null)
				insertNew(key, obj.BoardId);
		}
		public DiscussionList Get(string board_id)
		{
			string key = SandId.CombineId("dlpond1", board_id);

			DiscussionList obj = (DiscussionList)HttpRuntime.Cache.Get(key);

			if (obj == null)
				obj = insertNew(key, board_id);

			return obj;
		}
	}
	public class BsMapPond
	{
		private BsMap insertNew(string key)
		{
			BsMap obj = new BsMap();		// if concurrency happens, this will be done more than once.

			HttpRuntime.Cache.Insert(key, obj, null, DateTime.Now.AddSeconds(HeartsConfiguration.DEFAULT_CACHE_SECONDS),
											Cache.NoSlidingExpiration, CacheItemPriority.Default, removedCallback);

			// expires after some time for multi-node synchronization (unreliable).

			return obj;
		}
		private void removedCallback(string key, Object value, CacheItemRemovedReason reason)
		{
			if (reason == CacheItemRemovedReason.Expired)
			{
				insertNew(key);
			}
		}
		public void Notify()
		{
			string key = "bsmap1";
			BsMap obj = (BsMap)HttpRuntime.Cache.Remove(key);

			if (obj != null)
				insertNew(key);
		}
		public BsMap Get()
		{
			string key = "bsmap1";

			BsMap obj = (BsMap)HttpRuntime.Cache.Get(key);

			if (obj == null)
				obj = insertNew(key);

			return obj;
		}
	}
	public class BoardSettingPond
	{
		private BoardSetting insertNew(string key, string board_id)
		{
			BoardSetting obj = new BoardSetting(board_id);		// if concurrency happens, this will be done more than once.

			HttpRuntime.Cache.Insert(key, obj, null, DateTime.Now.AddSeconds(HeartsConfiguration.DEFAULT_CACHE_SECONDS),
											Cache.NoSlidingExpiration, CacheItemPriority.Default, removedCallback);

			// expires after some time for multi-node synchronization (unreliable).

			return obj;
		}
		private void removedCallback(string key, Object value, CacheItemRemovedReason reason)
		{
			if (reason == CacheItemRemovedReason.Expired)
			{
				BoardSetting obj = (BoardSetting)value;
				insertNew(key, obj.BoardId);
			}
		}
		public void Notify(string board_id)
		{
			string key = SandId.CombineId("boardsetting1", board_id);
			BoardSetting obj = (BoardSetting)HttpRuntime.Cache.Remove(key);

			if (obj != null)
				insertNew(key, obj.BoardId);
		}
		public BoardSetting Get(string board_id)
		{
			string key = SandId.CombineId("boardsetting1", board_id);

			BoardSetting obj = (BoardSetting)HttpRuntime.Cache.Get(key);

			if (obj == null)
				obj = insertNew(key, board_id);

			return obj;
		}
	}
	public class DiscussionKeyPond
	{
		private const string EMPTY_ROW_KEY = "";

		private string insertNew(string key, string board_id, string discussion_id, bool insert_if_not_exist)
		{
			string obj = Warehouse.KeyStoreTable.GetStringProperty(key, EMPTY_ROW_KEY, GroupStore.InsiderGroupName);
			if (obj == null && insert_if_not_exist)
			{
				obj = CryptUtil.GenerateKey();
				Warehouse.KeyStoreTable.SetStringProperty(key, EMPTY_ROW_KEY, GroupStore.InsiderGroupName, obj);

				obj = Warehouse.KeyStoreTable.GetStringProperty(key, EMPTY_ROW_KEY, GroupStore.InsiderGroupName);
			}
			if (obj == null)
				Util.ThrowAttackException("不存在的discussion key。");

			HttpRuntime.Cache.Insert(key, obj, null, Cache.NoAbsoluteExpiration, TimeSpan.FromMinutes(20));

			return obj;
		}
		public string Get(string board_id, string discussion_id, bool insert_if_not_exist)
		{
			string key = SandId.CombineId("discussionkey1", board_id, discussion_id);

			string obj = (string)HttpRuntime.Cache.Get(key);

			if (obj == null)
				obj = insertNew(key, board_id, discussion_id, insert_if_not_exist);

			return obj;
		}
	}
	public class DiscussionLoadPond
	{
		private DiscussionLoadRoll insertNew(string key, string board_id, string discussion_id)
		{
			DiscussionLoadRoll obj = new DiscussionLoadRoll(board_id, discussion_id);		// if concurrency happens, this will be done more than once.

			HttpRuntime.Cache.Insert(key, obj, null, DateTime.Now.AddSeconds(HeartsConfiguration.DEFAULT_CACHE_SECONDS),
											Cache.NoSlidingExpiration, CacheItemPriority.Default, removedCallback);

			// expires after some time for multi-node synchronization (unreliable).

			return obj;
		}
		private void removedCallback(string key, Object value, CacheItemRemovedReason reason)
		{
			if (reason == CacheItemRemovedReason.Expired)
			{
				DiscussionLoadRoll obj = (DiscussionLoadRoll)value;
				//insertNew(key, obj.BoardId, obj.DiscussionId);
			}
		}
		public void Notify(string board_id, string discussion_id)
		{
			string key = SandId.CombineId("discussionload1", board_id, discussion_id);
			DiscussionLoadRoll obj = (DiscussionLoadRoll)HttpRuntime.Cache.Remove(key);

			if (obj != null)
				insertNew(key, board_id, discussion_id);
		}
		public DiscussionLoadRoll Get(string board_id, string discussion_id)
		{
			string key = SandId.CombineId("discussionload1", board_id, discussion_id);

			DiscussionLoadRoll obj = (DiscussionLoadRoll)HttpRuntime.Cache.Get(key);

			if (obj == null)
				obj = insertNew(key, board_id, discussion_id);

			return obj;
		}
	}
}