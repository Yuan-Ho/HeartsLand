using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage.Table;
using System.Threading;
using System.Collections.Concurrent;
using WebMatrix.WebData;
using System.Text;
using System.Text.RegularExpressions;

namespace MvcWebRole1.Models
{
	public class BubblingRoll
	{
		public const string PAR_KEY_PREFIX = "bldr2";
		public readonly int ListSize;
		public readonly string Id;

		// First is latest. Last is oldest.
		private LinkedList<string/*board_id.discussion_id*/> list;
		private bool modified = false;

		public BubblingRoll(string id, int list_size)
		{
			this.ListSize = list_size;
			this.Id = id;
#if OLD
			DiscussionListStore.GetLastDiscussions(board_id, LIST_SIZE, entity => ll.AddFirst(entity.RowKey));
#else
			string key = SandId.CombineId(PAR_KEY_PREFIX, id);
			string serialized = Warehouse.TemporalTable.LoadLongString(key, "segment0");

			if (serialized != null)
				this.list = Util.DeserializeToLinkedList(serialized);
			else
				this.list = new LinkedList<string>();
#endif
		}
		public string[] ToArray()
		{
			// copy to avoid concurrency problem. inefficient.

			lock (this.list)
				return this.list.ToArray<string>();
		}
		public void Put(string combined_id)
		{
			this.modified = true;

			lock (this.list)
			{
				LinkedListNode<string> node = this.list.Find(combined_id);

				if (node == null)
				{
					this.list.AddFirst(combined_id);
					while (this.list.Count > this.ListSize)
						this.list.RemoveLast();
				}
				else
				{
					this.list.Remove(node);
					this.list.AddFirst(node);
				}
			}
		}
		public void Save()
		{
			// nodes overwrite each other.
			if (this.modified)
			{
				string serialized;
				lock (this.list)
					serialized = Util.Serialize(this.list);

				// different nodes override each other.
				string key = SandId.CombineId(PAR_KEY_PREFIX, this.Id);
				Warehouse.TemporalTable.SaveLongString(key, "segment0", serialized);

				// if Put() is called in another thread here, the modified flag is cleared wrongly.

				this.modified = false;
			}
		}
	}
	public class DiscussionLoadRoll
	{
		public string Output { get; private set; }
		public string Heading { get; private set; }

		public string Description { get; private set; }
		public List<string> OgImages { get; private set; }
		public DateTime LastModifiedTime { get; private set; }

		public DiscussionLoadRoll(string board_id, string discussion_id)
		{
			this.OgImages = new List<string>();

			StringWriter writer = new StringWriter();

			string partition_key = SandId.CombineId(board_id, discussion_id);

			TableQuery query = new TableQuery().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partition_key));

			foreach (DynamicTableEntity entity in Warehouse.DiscussionLoadTable.ExecuteQuery(query))
			{
				if (entity.RowKey[0] != SandId.SPECIAL_KEY_PREFIX/* &&
					!entity.IsPermanentlyDeleted()*/)
					addLetter(writer, entity);
			}
			this.Output = writer.ToString();
			this.LastModifiedTime = DateTime.Now;
		}
		public void AddLetter(DynamicTableEntity entity)
		{
			StringWriter writer = new StringWriter();
			writer.Write(this.Output);

			addLetter(writer, entity);

			this.Output = writer.ToString();		// one simple step to avoid trouble of multiple threads.
			this.LastModifiedTime = DateTime.Now;	// considering race condition, update Output before LastModifiedTime.
		}
		private void addLetter(TextWriter writer, DynamicTableEntity entity)
		{
			LetterConverter.WriteForCrawler(writer, entity, false, null);

			Subtype subtype = LetterConverter.GetSubtype(entity);

			switch (subtype)
			{
				case Subtype.h:
					if (this.Heading == null)
						this.Heading = /*RemoveForeMeta*/(LetterConverter.GetAbstract(entity));
					break;
				case Subtype.s:
					// when subject is split to multiple segments, subtype s will be met multiple times.
					if (this.Description == null)
						this.Description = extractOgImage(LetterConverter.GetWholeWords(entity, false));
					break;
			}
		}
		//private static Regex imageUrlRegex = new Regex(@"^<img src=""(.+?)"">$",
		private static Regex anchorImageUrlRegex = new Regex(@"<a href=""(.+?)""><img src=""(.+?)""></a>",
														RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant);
		private static Regex imageUrlRegex = new Regex(@"<img src=""(.+?)"">",
														RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant);
		private static Regex foreMetaRegex = new Regex(@"^>>(.+?)\n",
														RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant);
		private string extractOgImage(string whole_words)
		{
			string desc = anchorImageUrlRegex.Replace(whole_words, (Match match) =>
				{
					if (this.OgImages.Count < 10)
						this.OgImages.Add(match.Groups[1].Value);

					return string.Empty;
				});
			desc = imageUrlRegex.Replace(desc, (Match match) =>
				{
					if (this.OgImages.Count < 10)
						this.OgImages.Add(match.Groups[1].Value);

					return string.Empty;
				});
			//desc = foreMetaRegex.Replace(desc, string.Empty);
			//desc = RemoveForeMeta(desc);
			return desc;
		}
		public static string RemoveForeMeta(string text)
		{
			string ret = foreMetaRegex.Replace(text, string.Empty);
			return ret;
		}
	}
	public class DiscussionSummary
	{
		private const int REPLY_COUNT = 10;

		private string headingJson;
		private string subjectJson;
		private ViewType viewType = ViewType.horizontal;

		private ConcurrentQueue<string> replyQueue = new ConcurrentQueue<string>();
		public readonly string DiscussionId;
		public readonly string BoardId;

		public DiscussionSummary(string board_id, string discussion_id)
		{
			this.BoardId = board_id;
			this.DiscussionId = discussion_id;

			DiscussionLoadStore.GetFirstLetters(board_id, discussion_id, 2, entity => AddLetter(entity));
			DiscussionLoadStore.GetLastLetters(board_id, discussion_id, REPLY_COUNT, entity => AddLetter(entity));		// may duplicate with first 2.
		}
		public void AddLetter(DynamicTableEntity entity)
		{
			Subtype subtype = LetterConverter.GetSubtype(entity);
			if (subtype == Subtype.h)
				this.viewType = entity.GetViewType();

			string json = LetterConverter.LetterToJson(entity, this.BoardId, this.DiscussionId, this.viewType);

			if (subtype == Subtype.h)
			{
				if (this.headingJson == null)
					this.headingJson = json;
			}
			else if (entity.RowKey == SandId.FIRST_SUBJECT_LETTER_ID)
			{
				if (this.subjectJson == null)
					this.subjectJson = json;
			}
			else if (subtype == Subtype.s || subtype == Subtype.r)
			{
				this.replyQueue.Enqueue(json);

				if (this.replyQueue.Count > REPLY_COUNT)
				{
					string dummy;
					this.replyQueue.TryDequeue(out dummy);
				}
			}
			// subtype d is ignored. when there are lots of subtype d, replies will be pushed out of queue.
		}
		public void Write(TextWriter writer)
		{
			writer.WriteStartTag("article");

			writer.WriteRaw(headingJson);
			writer.WriteRaw(subjectJson);		// may be null if subject is permanently deleted.

			foreach (string json in replyQueue)
			{
				writer.WriteRaw(json);
			}
			writer.WriteEndTag("article");
		}
	}
	public class DiscussionList
	{
		private const int COUNT = 100;

		private ConcurrentQueue<string> queue = new ConcurrentQueue<string>();
		public readonly string BoardId;
		public readonly string BoardName;

		public DiscussionList(string board_id)
		{
			this.BoardId = board_id;
			this.BoardName = Warehouse.BsMapPond.Get().GetBoardName(board_id);
			//this.BoardName = BoardInfoStore.GetBoardName(board_id);

			DiscussionListStore.GetLastDiscussions(board_id, COUNT, entity => AddDiscussion(entity));
		}
		public void AddDiscussion(DynamicTableEntity entity)
		{
			string json = toJson(entity);
			this.queue.Enqueue(json);

			if (this.queue.Count > COUNT)
			{
				this.queue.TryDequeue(out json);
			}
		}
		private string toJson(DynamicTableEntity entity)
		{
			StringWriter sw = new StringWriter();

			ViewType view_type = entity.GetViewType();

			sw.WriteDiscussionAnchor(this.BoardId, entity.RowKey, entity["heading"].StringValue, view_type);

			string flags = entity.GetFlags();

			if (flags.Length != 0)
				sw.WriteForCrawler("footer", flags);

			return sw.ToString();
		}
		public void Write(TextWriter writer)
		{
			writer.WriteStartTag("nav");

			writer.WriteStartTag("h2");
			writer.WriteBoardAnchor(this.BoardId, this.BoardName);
			writer.WriteEndTag("h2");

			foreach (string json in this.queue)
				writer.WriteRaw(json);

			writer.WriteEndTag("nav");
		}
	}
	class BsInfo		// Board or selection info
	{
		public string Name;
		public List<string/*selection or board id*/> IdList = new List<string>();
	}
	public class BsMap
	{
		private Dictionary<string/*board id*/, BsInfo> boardInfoDict = new Dictionary<string, BsInfo>();
		private Dictionary<string/*selection id*/, BsInfo> selectionInfoDict = new Dictionary<string, BsInfo>();

		public string Output { get; private set; }

		public BsMap()
		{
			StringWriter writer = new StringWriter();

			writer.WriteStartTag("nav");
			BoardInfoStore.GetBoards((board_id, board_name) =>
			{
				if (uint.Parse(board_id.Substring(1)) >= 1149)
					writer.WriteBoardAnchor(board_id, board_name);

				getBoardInfo(board_id).Name = board_name;
			});
			writer.WriteEndTag("nav");
			//
			writer.WriteStartTag("aside");
			SelectionInfoStore.GetSelections((selection_id, selection_name, board_list) =>
			{
				if (uint.Parse(selection_id.Substring(1)) >= 1027)
				{
					writer.WriteStartTag("nav");

					writer.WriteBoardAnchor(selection_id, selection_name);
					writer.WriteForCrawler("footer", board_list);

					writer.WriteEndTag("nav");
				}
				//
				BsInfo sel_info = getSelectionInfo(selection_id);
				sel_info.Name = selection_name;

				SandId.SplitWithCallback2(board_list, ',', board_id =>
				{
					this.boardInfoDict[board_id].IdList.Add(selection_id);		// if the board id does not exist, throw exception.
					sel_info.IdList.Add(board_id);
				});
			});
			writer.WriteEndTag("aside");

			this.Output = writer.ToString();
		}
		private BsInfo getSelectionInfo(string selection_id)
		{
			BsInfo info;

			if (!this.selectionInfoDict.TryGetValue(selection_id, out info))
			{
				info = new BsInfo();
				this.selectionInfoDict.Add(selection_id, info);
			}
			return info;
		}
		private BsInfo getBoardInfo(string board_id)
		{
			BsInfo info;

			if (!this.boardInfoDict.TryGetValue(board_id, out info))
			{
				info = new BsInfo();
				this.boardInfoDict.Add(board_id, info);
			}
			return info;
		}
		public List<string/*selection id*/> GetSelectionList(string board_id)
		{
			BsInfo info;

			if (this.boardInfoDict.TryGetValue(board_id, out info))
				return info.IdList;

			return null;
		}
		public List<string/*board id*/> GetBoardList(string selection_id)
		{
			BsInfo info;

			if (this.selectionInfoDict.TryGetValue(selection_id, out info))
				return info.IdList;

			return null;
		}
		public bool IsValidBoardId(string board_id)
		{
			return this.boardInfoDict.ContainsKey(board_id);
		}
		public bool IsValidSelectionId(string selection_id)
		{
			return this.selectionInfoDict.ContainsKey(selection_id);
		}
		public string/*null if not exist*/ GetSelectionName(string selection_id)
		{
			BsInfo info;

			if (this.selectionInfoDict.TryGetValue(selection_id, out info))
				return info.Name;

			return null;
		}
		public string/*null if not exist*/ GetBoardName(string board_id)
		{
			BsInfo info;

			if (this.boardInfoDict.TryGetValue(board_id, out info))
				return info.Name;

			return null;
		}
	}
	public class BoardSetting
	{
		public readonly string BoardId;
		public readonly string BoardName;
		public readonly bool LowKey;

		public readonly string[] g10;
		public readonly string[] g11;
		public readonly string[] g12;

		public readonly int[] g10UserIds;
		public readonly int[] g11UserIds;
		public readonly int[] g12UserIds;

		public string Output { get; private set; }

		public BoardSetting(string board_id)
		{
			BoardId = board_id;
			BoardName = Warehouse.BsMapPond.Get().GetBoardName(board_id);

			string flags = BoardInfoStore.GetBoardFlags(board_id);

			this.LowKey = SandFlags.Check(flags, SandFlags.MT_LOW_KEY, 1);

#if ACCOUNT_USING_SQL
			g10 = GroupStore.GetBoardGroup(board_id, GroupStore.ChairOwnerGroupName, out g10UserIds);
			g11 = GroupStore.GetBoardGroup(board_id, GroupStore.ViceOwnerGroupName, out g11UserIds);
			g12 = GroupStore.GetBoardGroup(board_id, GroupStore.InsiderGroupName, out g12UserIds);
#endif
			//
			Output = JsonConvert.SerializeObject(this);		// the ","Output":null" will also be written into Output.
		}
	}
}