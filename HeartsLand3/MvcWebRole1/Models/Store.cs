using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure;
using Newtonsoft.Json;
using System.Text;
using System.IO;
using WebMatrix.WebData;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Text.RegularExpressions;

namespace MvcWebRole1.Models
{
	public enum Subtype
	{
		h,		// Heading
		s,		// Subject
		r,		// Reply
		d,		// Delete remark
	}
	public enum ViewType
	{
		horizontal = 0,
		map = 2,
		sky = 3,
		scb = 4,
	}
	public enum ActivityType
	{
		NONE,
		UpdateGroup,
	}
	public static class CreatorConverter
	{
		public enum Status
		{
			Creator,
			Editor,
			LastEditor,
		}
		private static string[,] names = new string[,]
		{
			{"creator", "createtime", "creatoruid", "creatormid", "creatoraddr", "creatorxff", "cun"},
			{"editor", "edittime", "editoruid", "editormid", "editoraddr", "editorxff", "eun"},
			{"lasteditor", "lastedittime", "lasteditoruid", "lasteditormid", "lasteditoraddr", "lasteditorxff", "lun"}
		};
		public static void FillEntity(DynamicTableEntity entity, Status status, string nickname/*null if no need*/)
		{
			entity[names[(int)status, 0]] = new EntityProperty(nickname);		// may be null.
			entity[names[(int)status, 1]] = new EntityProperty(DateTime.Now);

			entity[names[(int)status, 2]] = new EntityProperty(-1/*WebSecurity.CurrentUserId*/);		// -1 if not logged in.
			entity[names[(int)status, 3]] = new EntityProperty(UserStore.CurrentUserMId);		// when the value is null, the property will not be written into table.

			entity[names[(int)status, 4]] = new EntityProperty(HttpContext.Current.Request.UserHostAddress/*::1 if local*/);
			entity[names[(int)status, 5]] = new EntityProperty(HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"]/*null if not exist*/);		// if null, the property is not really written into table.

			entity[names[(int)status, 6]] = new EntityProperty(""/*WebSecurity.CurrentUserName*/);		// "" if not logged in.
		}
		public static bool IsCurrentUserCreator(DynamicTableEntity entity)
		{
			int cuid = -1/*WebSecurity.CurrentUserId*/;
			if (cuid == -1)
				return false;
			return entity[names[(int)Status.Creator, 2]].Int32Value == cuid;
		}
		public static void ChangeNickname(DynamicTableEntity entity, Status status, string nickname)
		{
			entity[names[(int)status, 0]] = new EntityProperty(nickname);
		}
		public static void CopyEntity(DynamicTableEntity src_entity, Status src_status,
			DynamicTableEntity dst_entity, Status dst_status)
		{
			try
			{
				dst_entity[names[(int)dst_status, 0]] = new EntityProperty(src_entity.GetString(names[(int)src_status, 0], null));
				dst_entity[names[(int)dst_status, 1]] = new EntityProperty(src_entity[names[(int)src_status, 1]].DateTimeOffsetValue);
				dst_entity[names[(int)dst_status, 2]] = new EntityProperty(src_entity[names[(int)src_status, 2]].Int32Value);
				dst_entity[names[(int)dst_status, 3]] = new EntityProperty(src_entity[names[(int)src_status, 3]].StringValue);

				dst_entity[names[(int)dst_status, 4]] = new EntityProperty(src_entity.GetString(names[(int)src_status, 4], null));
				dst_entity[names[(int)dst_status, 5]] = new EntityProperty(src_entity.GetString(names[(int)src_status, 5], null));

				dst_entity[names[(int)dst_status, 6]] = new EntityProperty(src_entity.GetString(names[(int)src_status, 6], null));
			}
			catch (KeyNotFoundException ex)
			{
				// TODO: inspect why this happens. this happens when a letter is 檢舉 then 先斬後奏.
				// version is not zero, but the entity is creator instead of last editor.
			}
		}
		public static void EntityWrite(JsonTextWriter writer, DynamicTableEntity entity, Status status)
		{
			// Keep order
			writer.WriteValue(entity.GetString(names[(int)status, 0], string.Empty));		// JsonTextWriter writes """" for empty string.
			writer.WriteValue(entity.GetDateTimeOffset(names[(int)status, 1], null));		// JsonTextWriter writes "null" for null.
		}
		public static void WriteForCrawler(TextWriter writer, DynamicTableEntity entity, Status status)
		{
			EntityProperty ep;

			if (entity.Properties.TryGetValue(names[(int)status, 0], out ep))
			{
				//writer.WriteForCrawler("address", ep.StringValue);
				string user_name = entity.GetString(names[(int)status, 6], null);
				string link = null;

				if (!SandId.IsLau(user_name))
				{
					int user_id = (int)entity[names[(int)status, 2]].Int32Value;		// may be -1.
					link = HtmlUtil.MakeUserLink(user_id);
				}
				writer.WriteStartTag("address");
				writer.WriteAnchor(link, ep.StringValue);
				writer.WriteEndTag("address");
			}

			if (entity.Properties.TryGetValue(names[(int)status, 1], out ep))
				writer.WriteForCrawler("time", Convertor.ToString(ep.DateTimeOffsetValue));
		}
	}
	public static class LetterConverter
	{
		public const int ABSTRACT_MAX_LEN = 200;

		public static Subtype GetSubtype(DynamicTableEntity entity)
		{
			string subtype = entity["subtype"].StringValue;
			Subtype st = (Subtype)Enum.Parse(typeof(Subtype), subtype);
			return st;
		}
		public static string GetAbstract(DynamicTableEntity entity)
		{
			return entity["abstract"].StringValue;
		}
		public static string GetWholeWords(DynamicTableEntity entity, bool summary_only)
		{
			if (!entity.IsPermanentlyDeleted())
			{
				string whole_words = entity["abstract"].StringValue;
				EntityProperty ep;

				if (entity.Properties.TryGetValue("words", out ep))
					whole_words += summary_only ? Environment.NewLine + "..." : ep.StringValue;
				//writer.WriteForCrawler("details", summary_only ? "..." : ep.StringValue);

				return whole_words;
			}
			else
				return ">>已被原作者刪除。";
		}
		private static void fillWords(DynamicTableEntity entity, string words, string board_id, string discussion_id)
		{
			string flags = entity.GetFlags();
			bool encrypt = SandFlags.Check(flags, SandFlags.MT_AUTHORIZATION, 2);

			if (encrypt)
			{
				entity["abstract"] = new EntityProperty(string.Empty);

				string key = Warehouse.DiscussionKeyPond.Get(board_id, discussion_id, true);
				string crypt = CryptUtil.Encrypt(words, key);

				entity["words"] = new EntityProperty(crypt);
#if DEBUG
				string plain = CryptUtil.Decrypt(crypt, key);
				if (words != plain)
					throw new ProgramLogicException();
#endif
			}
			else if (words.Length > ABSTRACT_MAX_LEN)
			{
				int p = words.LastIndexOfAny(Util.WordsSplitCharacters, ABSTRACT_MAX_LEN);
				if (p == -1)
					p = 0;

				entity["abstract"] = new EntityProperty(words.Substring(0, p));
				entity["words"] = new EntityProperty(words.Substring(p));
			}
			else
			{
				entity["abstract"] = new EntityProperty(words);
				entity.Properties.Remove("words");
			}
		}
		public static string LetterToJson(DynamicTableEntity entity, string board_id, string discussion_id, ViewType view_type)
		{
			StringWriter sw = new StringWriter();

			string link = HtmlUtil.MakeLetterLink(board_id, discussion_id, entity.RowKey, view_type);

			WriteForCrawler(sw, entity, true, link);

			return sw.ToString();
		}
		public static void LetterToJson(JsonTextWriter writer, DynamicTableEntity entity, bool summary_only)
		{
			writer.WritePropertyName(entity.RowKey);
			writer.WriteStartArray();

			// Keep order

			writer.WriteValue(entity["subtype"].StringValue);
			CreatorConverter.EntityWrite(writer, entity, CreatorConverter.Status.Creator);

			writer.WriteValue(entity["abstract"].StringValue);

			string words = entity.GetString("words", string.Empty);		// todo: should call GetWholeWords().

			if (summary_only)
				writer.WriteValue(words.Length);
			else
				writer.WriteValue(words);

			writer.WriteValue(entity.GetFlags());

			CreatorConverter.EntityWrite(writer, entity, CreatorConverter.Status.LastEditor);

			writer.WriteEndArray();
		}
		public static void WriteForCrawler(TextWriter writer, DynamicTableEntity entity, bool summary_only, string link)
		{
			writer.WriteStartTag("section");

			writer.WriteForCrawler("header", entity.RowKey);

			writer.WriteForCrawler("aside", entity["subtype"].StringValue);

			CreatorConverter.WriteForCrawler(writer, entity, CreatorConverter.Status.Creator);

			string whole_words = GetWholeWords(entity, summary_only);

			//HtmlUtil.WriteForCrawler(writer, "summary", entity["abstract"].StringValue);
			writer.WriteStartTag("summary");
			if (link != null)
				writer.WriteAnchor(link, whole_words);
			else
				writer.WriteRaw(whole_words);
			writer.WriteEndTag("summary");

			string flags = entity.GetFlags();

			if (flags.Length != 0)
				writer.WriteForCrawler("footer", flags);

			CreatorConverter.WriteForCrawler(writer, entity, CreatorConverter.Status.LastEditor);
			writer.WriteEndTag("section");
		}
		public static void CopyLetterRevision(DynamicTableEntity letter_entity, DynamicTableEntity revision_entity, int version)
		{
			CreatorConverter.CopyEntity(
										letter_entity,
										version == 0 ? CreatorConverter.Status.Creator : CreatorConverter.Status.LastEditor,
										revision_entity,
										CreatorConverter.Status.Editor);
			EntityProperty ep;

			string words = letter_entity["abstract"].StringValue;
			if (letter_entity.Properties.TryGetValue("words", out ep))
				words += ep.StringValue;

			revision_entity["words"] = new EntityProperty(words);
			revision_entity["flags2"] = new EntityProperty(letter_entity.GetString("flags2", null));
		}
		public static void EditLetterEntity(DynamicTableEntity entity, string editor, string words, string delta_flags, string board_id, string discussion_id)
		{
			ModifyLetterFlags(entity, editor, delta_flags);

			fillWords(entity, words, board_id, discussion_id);		// must be after flags modification so that encrypt flag is correct.
		}
		public static void ModifyLetterFlags(DynamicTableEntity entity, string editor, string delta_flags)
		{
			entity.OperateFlags(new FlagMergeOperation(delta_flags));

			// don't count into last edit for delete/report/permanent delete.
			if (editor != null)
			{
				CreatorConverter.FillEntity(entity, CreatorConverter.Status.LastEditor, editor);

				if (CreatorConverter.IsCurrentUserCreator(entity))
					CreatorConverter.ChangeNickname(entity, CreatorConverter.Status.Creator, editor);
			}
		}
		public static void CreateLetterEntity(DynamicTableEntity entity, string creator, string words,
												Subtype subtype, string delta_flags, string board_id, string discussion_id)
		{
			CreatorConverter.FillEntity(entity, CreatorConverter.Status.Creator, creator);

			entity["subtype"] = new EntityProperty(subtype.ToString());
#if OLD
			string flags = SandFlags.Merge("", delta_flags);

			if (flags.Length > 0)
				entity["flags"] = new EntityProperty(flags);
#else
			entity.OperateFlags(new FlagMergeOperation(delta_flags));
#endif
			fillWords(entity, words, board_id, discussion_id);		// must be after flags modification so that encrypt flag is correct.
		}
	}
	public static class DiscussionListStore
	{
		public static void GetLastDiscussions(string board_id, int cnt, Action<DynamicTableEntity> act)
		{
			string partition_key = board_id;

			int last_id = NextIdStore.GetLastId(Warehouse.DiscussionListTable, partition_key);

			if (last_id != -1)
				Warehouse.DiscussionListTable.EnumerateRowRange(partition_key, SandId.DISCUSSION_ID_PREFIX, last_id - cnt + 1, cnt, act);
		}
		public static object GetDiscussionList(string board_id)
		{
			List<string> discussion_id_list = new List<string>();
			List<string> discussion_heading_list = new List<string>();

			GetLastDiscussions(board_id, 100,
				entity =>
				{
					discussion_heading_list.Add(entity["heading"].StringValue);
					discussion_id_list.Add(entity.RowKey);
				});
			//
			string board_name = BoardInfoStore.GetBoardName(board_id);

			return new
			{
				discussion_id_list = discussion_id_list.ToArray(),
				discussion_heading_list = discussion_heading_list.ToArray(),
				board_name = board_name,
				gen_time = Util.DateTimeToString(DateTime.Now, 1)
			};
		}
		public static void CreateSkeleton(string board_id)
		{
			string partition_key = board_id;
			NextIdStore.Create(Warehouse.DiscussionListTable, partition_key, HeartsConfiguration.NEXT_ID_INITIAL_VALUE);
		}
		public static string/*discussion id*/ CreateDiscussion(string board_id, string creator, string words, string heading,
																string delta_flags, HttpFileCollectionBase files, string heading_delta_flags)
		{
			string partition_key = board_id;

			int next_id = NextIdStore.Next(Warehouse.DiscussionListTable, partition_key);
			string discussion_id = SandId.MakeDiscussionId(next_id);

			DynamicTableEntity entity = new DynamicTableEntity(partition_key, discussion_id);
			entity["heading"] = new EntityProperty(/*DiscussionLoadRoll.RemoveForeMeta*/(heading));
			entity.OperateFlags(new FlagMergeOperation(heading_delta_flags));

			Warehouse.DiscussionListTable.Execute(TableOperation.Insert(entity));

			DiscussionLoadStore.CreateSkeleton(board_id, discussion_id);
			DiscussionLoadStore.CreateLetter(board_id, discussion_id, creator, heading, Subtype.h, heading_delta_flags, null);
			DiscussionLoadStore.CreateLetter(board_id, discussion_id, creator, words, Subtype.s, delta_flags, files);

			Warehouse.DiscussionListPond.Get(board_id).AddDiscussion(entity);

			return discussion_id;
		}
		public static void EditHeading(string board_id, string discussion_id, string heading)
		{
			TableResult result = Warehouse.DiscussionListTable.Execute(TableOperation.Retrieve(board_id, discussion_id));
			DynamicTableEntity entity = (DynamicTableEntity)result.Result;

			entity["heading"].StringValue = /*DiscussionLoadRoll.RemoveForeMeta*/(heading);

			Warehouse.DiscussionListTable.Execute(TableOperation.Replace(entity));

			Warehouse.DiscussionListPond.Notify(board_id);
		}
		public static void OperateFlags(string board_id, string discussion_id, FlagOperation op)
		{
			TableResult result = Warehouse.DiscussionListTable.Execute(TableOperation.Retrieve(board_id, discussion_id));
			DynamicTableEntity entity = (DynamicTableEntity)result.Result;

			entity.OperateFlags(op);

			Warehouse.DiscussionListTable.Execute(TableOperation.Replace(entity));
			Warehouse.DiscussionListPond.Notify(board_id);
		}
	}
	public class RevisionStore
	{
		public static void CreateRevision(DynamicTableEntity letter_entity, string board_id, string discussion_id,
										string letter_id, int version)
		{
			string partition_key = SandId.CombineId(board_id, discussion_id, letter_id);

			DynamicTableEntity entity = new DynamicTableEntity(partition_key, SandId.MakeRevisionId(version));

			LetterConverter.CopyLetterRevision(letter_entity, entity, version);

			Warehouse.RevisionTable.Execute(TableOperation.Insert(entity));		// If an exception is thrown after revision created but before edit count in letter entity is updated, next time edit will fail because the revision already exists.
		}
		public static int IncreaseEditCount(DynamicTableEntity entity)
		{
			EntityProperty ep;
			int ec = 0;
			if (entity.Properties.TryGetValue("editcount", out ep))
			{
				ec = (int)ep.Int32Value;
				entity["editcount"].Int32Value = ec + 1;
			}
			else
				entity["editcount"] = new EntityProperty(ec + 1);
			return ec;
		}
		public static void CreateHistory(DynamicTableEntity src_entity, string partition_key, int version, params string[] properties_to_copy)
		{
			DynamicTableEntity dst_entity = new DynamicTableEntity(partition_key, SandId.MakeRevisionId(version));

			CreatorConverter.CopyEntity(src_entity,
										CreatorConverter.Status.Editor,
										dst_entity,
										CreatorConverter.Status.Editor);

			foreach (string property_name in properties_to_copy)
			{
				EntityProperty ep;
				if (src_entity.Properties.TryGetValue(property_name, out ep))
					dst_entity[property_name] = ep/*src_entity[property_name]*/;
			}

			Warehouse.HistoryTable.Execute(TableOperation.Insert(dst_entity));
		}
		public static void CreateActivity(string partition_key, string[] properties, object[] values)
		{
			int version = VersionCounterStore.Next(Warehouse.ActivityTable, partition_key);
			DynamicTableEntity entity = new DynamicTableEntity(partition_key, SandId.MakeRevisionId(version));

			for (int i = 0; i < properties.Length; i++)
			{
				if (values[i] is string)
					entity[properties[i]] = new EntityProperty((string)values[i]);
				else
					throw new ProgramLogicException();
			}
			Warehouse.ActivityTable.Execute(TableOperation.Insert(entity));		// 409 conflict exception if already exists.
		}
	}
	public struct ControlHistory
	{
		public int ReportCount;
	}
	public class DiscussionLoadStore
	{
		//public const string DISCUSSION_LOAD_PAR_KEY = "discussionload1";

		public static void GetFirstLetters(string board_id, string discussion_id, int cnt, Action<DynamicTableEntity> act)
		{
			string partition_key = SandId.CombineId(board_id, discussion_id);

			Warehouse.DiscussionLoadTable.EnumerateRowRange(partition_key, SandId.LETTER_ID_PREFIX, 0, cnt, act);
		}
		public static void GetLastLetters(string board_id, string discussion_id, int cnt, Action<DynamicTableEntity> act)
		{
			string partition_key = SandId.CombineId(board_id, discussion_id);

			int last_id = NextIdStore.GetLastId(Warehouse.DiscussionLoadTable, partition_key);

			if (last_id != -1)
				Warehouse.DiscussionLoadTable.EnumerateRowRange(partition_key, SandId.LETTER_ID_PREFIX, last_id - cnt + 1, cnt, act);
		}
		public static bool IsCurrentUserDiscussionCreator(string board_id, string discussion_id)
		{
			string partition_key = SandId.CombineId(board_id, discussion_id);
			TableResult result = Warehouse.DiscussionLoadTable.Execute(TableOperation.Retrieve(partition_key, SandId.HEADING_LETTER_ID));
			DynamicTableEntity entity = (DynamicTableEntity)result.Result;

			return CreatorConverter.IsCurrentUserCreator(entity);
		}
		public static void EditLetter(string board_id, string discussion_id, string letter_id, string editor, string words,
										string delta_flags, HttpFileCollectionBase files)
		{
			string partition_key = SandId.CombineId(board_id, discussion_id);

			TableResult result = Warehouse.DiscussionLoadTable.Execute(TableOperation.Retrieve(partition_key, letter_id));
			DynamicTableEntity entity = (DynamicTableEntity)result.Result;

			GroupStore.CheckEditRight(board_id, discussion_id, entity);

			HashSet<string> files_set = new HashSet<string>();
			words = processUploadFiles(words, files, files_set);
			words = processWords(words);
			claimFiles(words, SandId.CombineId(board_id, discussion_id, letter_id), files_set);

			// if (entity != null)		// let it throw null reference exception.
			int ec = RevisionStore.IncreaseEditCount(entity);

			RevisionStore.CreateRevision(entity, board_id, discussion_id, letter_id, ec);

			LetterConverter.EditLetterEntity(entity, editor, words, delta_flags, board_id, discussion_id);

			Warehouse.DiscussionLoadTable.Execute(TableOperation.Replace(entity));      // Throws StorageException ((412) Precondition Failed) if the entity is modified in between.

			Warehouse.DiscussionLoadPond.Notify(board_id, discussion_id);
			Warehouse.DiscussionSummaryPond.Notify(board_id, discussion_id);

			HttpResponse.RemoveOutputCacheItem("/discussionload/" + board_id + "/" + discussion_id);
		}
		public static ControlHistory ControlLetter(string board_id, string discussion_id, string letter_id, string delta_flags)
		{
			string partition_key = SandId.CombineId(board_id, discussion_id);

			// todo: select only needed properties.
			TableResult result = Warehouse.DiscussionLoadTable.Execute(TableOperation.Retrieve(partition_key, letter_id));
			DynamicTableEntity entity = (DynamicTableEntity)result.Result;

			// permanent deleting twice is not blocked.
			GroupStore.CheckControlRight(board_id, discussion_id, letter_id, ref delta_flags, entity);

			int ec = RevisionStore.IncreaseEditCount(entity);

			RevisionStore.CreateRevision(entity, board_id, discussion_id, letter_id, ec);

			int prev_report_cnt = SandFlags.GetNumber(entity.GetFlags(), SandFlags.MT_REPORT);

			LetterConverter.ModifyLetterFlags(entity, null, delta_flags);

			int new_report_cnt = SandFlags.GetNumber(entity.GetFlags(), SandFlags.MT_REPORT);

			Warehouse.DiscussionLoadTable.Execute(TableOperation.Replace(entity));      // Throws StorageException ((412) Precondition Failed) if the entity is modified in between.

			Warehouse.DiscussionLoadPond.Notify(board_id, discussion_id);
			Warehouse.DiscussionSummaryPond.Notify(board_id, discussion_id);
			HttpResponse.RemoveOutputCacheItem("/discussionload/" + board_id + "/" + discussion_id);

			return new ControlHistory { ReportCount = new_report_cnt - prev_report_cnt };
		}
		public static bool VoteLetter(string board_id, string discussion_id, string letter_id, string delta_flags)
		{
			try
			{
				SandFlags.SplitFlags(delta_flags, (meta_title, meta_value) =>
														{
															VoteBookStore.Vote(board_id, discussion_id, letter_id, meta_title);
														});
			}
			catch (StorageException ex)
			{
				return false;
			}

			string partition_key = SandId.CombineId(board_id, discussion_id);

			string pkFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partition_key);
			string rkFilter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, letter_id);
			string combinedFilter = TableQuery.CombineFilters(pkFilter, TableOperators.And, rkFilter);

			TableQuery query = new TableQuery().Where(combinedFilter).Select(new string[] { "flags2" });

			foreach (DynamicTableEntity entity in Warehouse.DiscussionLoadTable.ExecuteQuery(query))
			{
				entity.OperateFlags(new FlagOperation
									{
										type = FlagOperation.Type.AddMultiple,
										DeltaFlags = delta_flags,
									});

				if (entity.Properties.Count == 0)		// TableOperation.Merge cannot be used to remove property. if "flags" becomes empty, it will be removed from properties and the merge will have no effect.
					throw new ProgramLogicException();

				Warehouse.DiscussionLoadTable.Execute(TableOperation.Merge(entity));
			}
			Warehouse.DiscussionLoadPond.Notify(board_id, discussion_id);
			Warehouse.DiscussionSummaryPond.Notify(board_id, discussion_id);
			HttpResponse.RemoveOutputCacheItem("/discussionload/" + board_id + "/" + discussion_id);
			// Cache of ViewDiscussion is not removed so may serve old version.
			return true;
		}
		public static string/*letter id*/ CreateLetter(string board_id, string discussion_id, string creator, string words,
														Subtype subtype, string delta_flags, HttpFileCollectionBase files)
		{
			HashSet<string> files_set = new HashSet<string>();
			if (files != null)
				words = processUploadFiles(words, files, files_set);
			words = processWords(words);

			string letter_id = null;
			string[] split_arr = Util.SplitLongWords(words);
			if (split_arr.Length == 0)
				split_arr = new string[] { string.Empty };

			for (int i = 0; i < split_arr.Length; i++)
			{
				string lid = CreateLetterInternal(board_id, discussion_id, creator, split_arr[i], subtype, delta_flags);
				// if subject is split into multiple segments, all will have subtype "s" and only the first segment gets into summary.

				if (i == 0)
					letter_id = lid;

				claimFiles(split_arr[i], SandId.CombineId(board_id, discussion_id, lid), files_set);
			}
			HttpResponse.RemoveOutputCacheItem("/discussionload/" + board_id + "/" + discussion_id);
			if (subtype != Subtype.d)
				Warehouse.HotDiscussionRollPond.Put(board_id, discussion_id);

			return letter_id;
		}
		public static void CreateSkeleton(string board_id, string discussion_id)
		{
			string partition_key = SandId.CombineId(board_id, discussion_id);

			NextIdStore.Create(Warehouse.DiscussionLoadTable, partition_key, 0);
		}
		private static string/*letter id*/ CreateLetterInternal(string board_id, string discussion_id,
																string creator, string words, Subtype subtype, string delta_flags)
		{
			string partition_key = SandId.CombineId(board_id, discussion_id);

			if (NextIdStore.GetLastId(Warehouse.DiscussionLoadTable, partition_key) >= HeartsConfiguration.MAX_NUM_OF_LETTERS_IN_A_DISCUSSION)
				Util.ThrowBadRequestException("留言數超過上限（" + HeartsConfiguration.MAX_NUM_OF_LETTERS_IN_A_DISCUSSION + "）則。");

			int next_id = NextIdStore.Next(Warehouse.DiscussionLoadTable, partition_key);		// null reference exception for nonexistent board id.
			string letter_id = SandId.MakeLetterId(next_id);
			// if exception is thrown after getting id, the id is lost.

			DynamicTableEntity entity = new DynamicTableEntity(partition_key, letter_id);
			LetterConverter.CreateLetterEntity(entity, creator, words, subtype, delta_flags, board_id, discussion_id);
			//
			Warehouse.DiscussionLoadPond.Get(board_id, discussion_id).AddLetter(entity);
			Warehouse.DiscussionSummaryPond.Get(board_id, discussion_id).AddLetter(entity);

			Warehouse.DiscussionLoadTable.Execute(TableOperation.Insert(entity));
			//
			return letter_id;
		}
		private static void claimFiles(string words, string combined_id, HashSet<string> files_set)
		{
			foreach (string blob_name in files_set)
			{
				if (words.IndexOf(blob_name, StringComparison.Ordinal) != -1)
				{
					CloudBlockBlob block_blob = Warehouse.ImagesContainer.GetBlockBlobReference(blob_name);
					block_blob.Metadata["upload_letter_id"] = combined_id;
					block_blob.SetMetadata();
				}
			}
		}
		private static string uploadFile(HttpPostedFileBase file, string folder_name, string file_name, HashSet<string> files_set)
		{
			// shrunk image files have file name "blob".
			string ext = Path.GetExtension(file.FileName);
			if (ext.Length == 0)
				if (file.ContentType == "image/jpeg")
					ext = ".jpg";
				else if (file.ContentType == "image/png")
					ext = ".png";

			string blob_name = file_name + ext;
			blob_name = folder_name + "/" + blob_name;

			CloudBlockBlob block_blob = Warehouse.ImagesContainer.GetBlockBlobReference(blob_name);

			block_blob.Properties.ContentType = file.ContentType;
			block_blob.Properties.CacheControl = "public, max-age=2592000";		// 30 days
			block_blob.UploadFromStream(file.InputStream);		// if the same file.InputStream is uploaded twice, the latter one gets zero bytes of data.
			//block_blob.SetProperties();

			files_set.Add(blob_name);
#if OLD
				// https://hl1.blob.core.windows.net/images1/20140228/elaUbz2I.jpg
				string uri = block_blob.Uri.ToString();
				if (uri[4] == 's')		// https://
					uri = "http" + uri.Substring(5);
#else
			string uri = @"http://i.hela.cc" + block_blob.Uri.PathAndQuery;
#endif
			return uri;
		}
		private static string processUploadFiles(string words, HttpFileCollectionBase files, HashSet<string> files_set)
		{
			StringBuilder builder = new StringBuilder(words);

			foreach (string key in files.AllKeys)
			{
				string prefix = key.Substring(0, 3/*length of "n1/"*/);
				string prefix_thumbnail = null;
				string key_body = key.Substring(3/*length of "n1/"*/);

				if (prefix == "n3/")
					prefix_thumbnail = "n2/";
				else if (prefix == "n5/")
					prefix_thumbnail = "n4/";
				else if (prefix != "n1/" && prefix != "n6/")
					continue;

				string folder_name = Util.DateTimeToString(DateTime.Now, 4);
				string file_name = Util.RandomAlphaNumericString(8);
				string thumbnail_uri = null;

				if (prefix_thumbnail != null)
				{
					HttpPostedFileBase thumbnail_file = files[prefix_thumbnail + key_body];
					thumbnail_uri = uploadFile(thumbnail_file, folder_name, prefix_thumbnail + file_name, files_set);
				}
				HttpPostedFileBase file = files[prefix + key_body];
				string uri = uploadFile(file, folder_name, prefix + file_name, files_set);

				string html = string.Format(@"<a href=""{0}""><img src=""{1}""></a>", uri, thumbnail_uri != null ? thumbnail_uri : uri);

				builder.Replace(key_body, html);
			}
			return builder.ToString();
		}
		private static Regex helaImageUrlRegex = new Regex(@"^http(s?)://i\.hela\.cc[^'""<>\s]+\.(jpg|gif|png|jpeg)$",
														RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant);
		private static Regex imageUrlRegex = new Regex(@"^http(s?)://[0-9a-zA-Z][^'""<>\s]+\.(jpg|gif|png|jpeg)$",
														RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant);
		private static string processWords(string words)
		{
			words = words.Trim();		// the new lines between foremeta and words escape this trimming.
			words = words.Replace("\r\n", "\n");		// when there is upload file, chrome send \r\n instead of \n.

			// when a letter is edited, words contain only the image url but not html tags.
			// ie. "http://i.hela.cc/images1/20140903/n4/HIuWrNto.jpg", "http://i.imgur.com/bwmsg7t.jpg".
			words = helaImageUrlRegex.Replace(words, (Match match) =>
			{
				string thumbnail_uri = match.Value;
				string uri = thumbnail_uri;

				uri = uri.Replace("/n2/", "/n3/");
				uri = uri.Replace("/n4/", "/n5/");

				string html = string.Format(@"<a href=""{0}""><img src=""{1}""></a>", uri, thumbnail_uri);
				return html;
			});

			// for Google crawler.
			words = imageUrlRegex.Replace(words, @"<img src=""$0"">");

			return words;
		}
	}
}