using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Configuration;
using System.Text;
using System.Text.RegularExpressions;

namespace MvcWebRole1.Models
{
	public static class VersionCounterStore
	{
		public static int Next(CloudTable table, string partition_key)
		{
			TableResult result = table.Execute(TableOperation.Retrieve(partition_key, SandId.MakeRevisionId(0)));
			DynamicTableEntity entity = (DynamicTableEntity)result.Result;
			if (entity == null)
				return 0;
			EntityProperty ep;

			int version = 1;
			if (entity.Properties.TryGetValue("nextid", out ep))
			{
				version = (int)ep.Int32Value;
				ep.Int32Value = version + 1;
			}
			else
				entity["nextid"] = new EntityProperty(version + 1);
			
			table.Execute(TableOperation.Merge(entity));

			return version;
		}
	}
	public static class NextIdStore
	{
		private const string INFO_ROW_KEY = "!info1";
		private const string INFO_PAR_KEY = INFO_ROW_KEY;
		private const string EMPTY_ROW_KEY = "";

		private static DynamicTableEntity retrieveEntity(CloudTable table, string partition_key)
		{
			string row_key = partition_key == null ? EMPTY_ROW_KEY : INFO_ROW_KEY;
			if (partition_key == null)
				partition_key = INFO_PAR_KEY;

			TableResult result = table.Execute(TableOperation.Retrieve(partition_key, row_key));
			return (DynamicTableEntity)result.Result;
		}
		public static int GetLastId(CloudTable table, string partition_key)
		{
			DynamicTableEntity entity = retrieveEntity(table, partition_key);

			if (entity == null) return -1;
			return (int)entity["nextid"].Int32Value - 1;
		}
		public static int Next(CloudTable table, string partition_key)
		{
			DynamicTableEntity entity = retrieveEntity(table, partition_key);		// null for nonexistent board.

			int n_value = (int)entity["nextid"].Int32Value;
			entity["nextid"].Int32Value = n_value + 1;

			table.Execute(TableOperation.Replace(entity));      // Throws StorageException ((412) Precondition Failed) if the entity is modified in between.
			return n_value;
		}
		public static void Create(CloudTable table, string partition_key, int initial_value)
		{
			string row_key = partition_key == null ? EMPTY_ROW_KEY : INFO_ROW_KEY;
			if (partition_key == null)
				partition_key = INFO_PAR_KEY;

			DynamicTableEntity entity = new DynamicTableEntity(partition_key, row_key);

			entity["nextid"] = new EntityProperty(initial_value);
			table.Execute(TableOperation.Insert(entity));
		}
		public static void CreateIfNotExists(CloudTable table, string partition_key, int initial_value)
		{
			if (retrieveEntity(table, partition_key) == null)
				Create(table, partition_key, initial_value);
		}
	}
	public static class TableEntityHelper
	{
		public static string GetString(this DynamicTableEntity entity, string key, string default_value)
		{
			EntityProperty ep;

			if (entity.Properties.TryGetValue(key, out ep))
			{
				// ep.StringValue may be null when using "TableQuery().Select(new string[] { "flags" })" and the property does not exist.
				return ep.StringValue ?? default_value;
			}
			else
				return default_value;
		}
		public static DateTimeOffset? GetDateTimeOffset(this DynamicTableEntity entity, string key, DateTimeOffset? default_value)
		{
			EntityProperty ep;

			if (entity.Properties.TryGetValue(key, out ep))
				return ep.DateTimeOffsetValue;
			else
				return default_value;
		}
		public static void OperateFlags(this DynamicTableEntity entity, FlagOperation op)
		{
			string flags = entity.GetFlags();

			//flags = SandFlags.Merge(flags, delta_flags);
			flags = SandFlags.Operate(flags, op);

			if (flags.Length > 0)
				entity["flags2"] = new EntityProperty(flags);
			else
				entity.Properties.Remove("flags2");
		}
		public static string GetFlags(this DynamicTableEntity entity)
		{
			return entity.GetString("flags2", "");
		}
		public static bool IsPermanentlyDeleted(this DynamicTableEntity entity)
		{
			string flags = entity.GetFlags();

			return SandFlags.Check(flags, SandFlags.MT_PERMANENT_DELETE, 1);
		}
		public static ViewType GetViewType(this DynamicTableEntity entity)
		{
			string flags = entity.GetFlags();

			//return SandFlags.Check(flags, SandFlags.VIEW_FLAG_CHAR, 2);

			int n = SandFlags.GetNumber(flags, SandFlags.MT_VIEW);
			return (ViewType)n;
		}
	}
	public static class CloudTableHelper
	{
		public static void SaveLongString(this CloudTable table, string partition_key, string row_key, string text)
		{
			DynamicTableEntity entity = new DynamicTableEntity(partition_key, row_key);

			entity["part0"] = new EntityProperty(text);		// todo: must split if larger than 64kB.

			table.Execute(TableOperation.InsertOrReplace(entity));
		}
		public static string LoadLongString(this CloudTable table, string partition_key, string row_key)
		{
			TableResult result = table.Execute(TableOperation.Retrieve(partition_key, row_key));
			DynamicTableEntity entity = (DynamicTableEntity)result.Result;

			if (entity != null)
				return entity["part0"].StringValue;		// todo: combine if more than one part.
			else
				return null;
		}
		public static string GetStringProperty(this CloudTable table, string partition_key, string row_key, string property_name)
		{
			TableResult result = table.Execute(TableOperation.Retrieve(partition_key, row_key));
			DynamicTableEntity entity = (DynamicTableEntity)result.Result;

			if (entity != null)
				return entity[property_name].StringValue;
			else
				return null;
		}
		public static void SetStringProperty(this CloudTable table, string partition_key, string row_key, string property_name, string value)
		{
			DynamicTableEntity entity = new DynamicTableEntity(partition_key, row_key);

			entity[property_name] = new EntityProperty(value);

			table.Execute(TableOperation.InsertOrReplace(entity));
		}
		public static void EnumerateRowRange(this CloudTable table, string par_key, string row_key_prefix, int start, int count, Action<DynamicTableEntity> act)
		{
			int end = start + count - 1;

			if (count <= 0 || end < 0) return;

			string pkFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, par_key);

			if (start < 0) start = 0;

			int st = start, ed = 10;

			while ((st /= 10) != 0) ed *= 10;

			st = start;
			ed--;

			for (; ; )
			{
				if (ed > end) ed = end;

				string beginning = row_key_prefix + st.ToString();
				int key_len = beginning.Length;

				string rkLowerFilter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThanOrEqual, beginning);
				string rkUpperFilter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThanOrEqual, row_key_prefix + ed.ToString());
				string combinedRowKeyFilter = TableQuery.CombineFilters(rkLowerFilter, TableOperators.And, rkUpperFilter);
				string combinedFilter = TableQuery.CombineFilters(pkFilter, TableOperators.And, combinedRowKeyFilter);

				TableQuery query = new TableQuery().Where(combinedFilter);

				foreach (DynamicTableEntity entity in table.ExecuteQuery(query))
					if (entity.RowKey.Length == key_len)		// 10-19, 100-199, 1000-1999, etc.. are all between 1 and 2. may become very inefficient as total number of entities grow. don't query too old entities by this method.
						act(entity);

				if (ed == end) break;
				st = ed + 1;
				ed = st * 10 - 1;
			}
		}
	}
	public static class Warehouse
	{
		public static CloudTable SiteTable { get; private set; }
		public static CloudTable TemporalTable { get; private set; }
		public static CloudTable RevisionTable { get; private set; }
		public static CloudBlobContainer ImagesContainer { get; private set; }
		public static CloudTable SelectionListTable { get; private set; }
		public static CloudTable HistoryTable { get; private set; }
		public static CloudTable BoardListTable { get; private set; }
		public static CloudTable DiscussionListTable { get; private set; }
		public static CloudTable KeyStoreTable { get; private set; }
		public static CloudTable ActivityTable { get; private set; }
		public static CloudTable DiscussionLoadTable { get; private set; }
		public static CloudTable VoteBookTable { get; private set; }

		public static HotDiscussionRollPond HotDiscussionRollPond { get; private set; }
		public static DiscussionSummaryPond DiscussionSummaryPond { get; private set; }
		public static DiscussionListPond DiscussionListPond { get; private set; }
		public static BsMapPond BsMapPond { get; private set; }
		public static BoardSettingPond BoardSettingPond { get; private set; }
		public static DiscussionKeyPond DiscussionKeyPond { get; private set; }
		public static DiscussionLoadPond DiscussionLoadPond { get; private set; }

		public static RateLimiter RateLimiter { get; private set; }
		public static Random Random { get; private set; }

		public static void Initialize()
		{
			InitializeTable();

			HotDiscussionRollPond = new HotDiscussionRollPond();
			DiscussionSummaryPond = new DiscussionSummaryPond();
			DiscussionListPond = new DiscussionListPond();
			BsMapPond = new BsMapPond();
			BoardSettingPond = new BoardSettingPond();
			DiscussionKeyPond = new DiscussionKeyPond();
			DiscussionLoadPond = new DiscussionLoadPond();

			RateLimiter = new RateLimiter();
			Random = new Random();

			//SelectionBoardListResult.Initialize();
		}
		public static void InitializeTable()
		{
			SiteTable = getTable("sites1");
			if (SiteTable != null)
				SiteTable.CreateIfNotExists();
			//
			TemporalTable = getTable("temporal1");
			if (TemporalTable != null)
				TemporalTable.CreateIfNotExists();

			RevisionTable = getTable("revision1");
			if (RevisionTable != null)
				RevisionTable.CreateIfNotExists();

			ImagesContainer = getContainer("images1");

			SelectionListTable = getTable("selectionlist1");
			if (SelectionListTable != null)
			{
				SelectionListTable.CreateIfNotExists();
				SelectionInfoStore.CreateSkeleton();
			}

			HistoryTable = getTable("history1");
			if (HistoryTable != null)
				HistoryTable.CreateIfNotExists();

			BoardListTable = getTable("boardlist1");
			if (BoardListTable != null)
			{
				BoardListTable.CreateIfNotExists();
				BoardInfoStore.CreateSkeleton();
			}

			DiscussionListTable = getTable("discussionlist1");
			if (DiscussionListTable != null)
				DiscussionListTable.CreateIfNotExists();

			KeyStoreTable = getTable("keystore1");
			if (KeyStoreTable != null)
				KeyStoreTable.CreateIfNotExists();

			ActivityTable = getTable("activity1");
			if (ActivityTable != null)
				ActivityTable.CreateIfNotExists();

			DiscussionLoadTable = getTable("discussionload1");
			if (DiscussionLoadTable != null)
				DiscussionLoadTable.CreateIfNotExists();

			VoteBookTable = getTable("votebook1");
			if (VoteBookTable != null)
				VoteBookTable.CreateIfNotExists();
		}
		private static CloudTable getTable(string table_name)
		{
			try
			{
#if CLOUD
				string conn_str = CloudConfigurationManager.GetSetting("StorageConnectionString");
#else
				string conn_str = ConfigurationManager.AppSettings["StorageConnectionString"];
#endif
				CloudStorageAccount storageAccount = CloudStorageAccount.Parse(conn_str);

				CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

				CloudTable table = tableClient.GetTableReference(table_name);
				return table;
			}
			catch (System.Configuration.ConfigurationErrorsException)        // table is not available while running as a website.
			{
				return null;
			}
			catch (ArgumentNullException)		// running unit test.
			{
				return null;
			}
		}
		private static CloudBlobContainer getContainer(string container_name)
		{
			string conn_str = ConfigurationManager.AppSettings["StorageConnectionString"];
			CloudStorageAccount storageAccount = CloudStorageAccount.Parse(conn_str);

			CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

			CloudBlobContainer container = blobClient.GetContainerReference(container_name);

			container.CreateIfNotExists();

			container.SetPermissions(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });
			return container;
		}
	}
	public static class TableUpgrader
	{
		public static void Upgrade()
		{
#if OLD
			copyDiscussionLoadToSeparateTable();
			deleteOldDiscussionList();
			copyDiscussionListToSeparateTable();
			deleteOldBoardList();
			copyBoardListToSeparateTable();
			upgradeTemporalAddBoardId();
			mendMissingCreatorMId();
			upgradeFlags();
			moveForeMeta();
#endif
		}
		private static void copyDiscussionLoadToSeparateTable()
		{
			string pkFilter = TableQuery.CombineFilters(
				TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.GreaterThanOrEqual, "discussionload1.b0"),
				TableOperators.And,
				TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.LessThanOrEqual, "discussionload1.b99999"));

			TableQuery query = new TableQuery().Where(pkFilter);

			foreach (DynamicTableEntity entity in Warehouse.SiteTable.ExecuteQuery(query))
			{
				string new_pk = entity.PartitionKey.Substring(entity.PartitionKey.IndexOf('.') + 1);

				DynamicTableEntity new_entity = new DynamicTableEntity(new_pk, entity.RowKey);

				foreach (KeyValuePair<string, EntityProperty> pair in entity.Properties)
				{
					new_entity.Properties.Add(pair);
				}

				Warehouse.DiscussionLoadTable.Execute(TableOperation.Insert(new_entity));
			}
		}
		private static void deleteOldDiscussionList()
		{
			string pkFilter = TableQuery.CombineFilters(
				TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.GreaterThanOrEqual, "discussionlist1.b0"),
				TableOperators.And,
				TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.LessThanOrEqual, "discussionlist1.b99999"));

			TableQuery query = new TableQuery().Where(pkFilter);

			foreach (DynamicTableEntity entity in Warehouse.SiteTable.ExecuteQuery(query))
			{
				TableOperation op = TableOperation.Delete(entity);

				Warehouse.SiteTable.Execute(op);
			}
		}
		private static void copyDiscussionListToSeparateTable()
		{
			string pkFilter = TableQuery.CombineFilters(
				TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.GreaterThanOrEqual, "discussionlist1.b0"),
				TableOperators.And,
				TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.LessThanOrEqual, "discussionlist1.b99999"));

			TableQuery query = new TableQuery().Where(pkFilter);

			foreach (DynamicTableEntity entity in Warehouse.SiteTable.ExecuteQuery(query))
			{
				string board_id = entity.PartitionKey.Split('.')[1];

				DynamicTableEntity new_entity = new DynamicTableEntity(board_id, entity.RowKey);
				
				if (entity.RowKey[0] == SandId.SPECIAL_KEY_PREFIX)
					new_entity["nextid"] = entity["nextid"];
				else
					new_entity["heading"] = entity["heading"];

				Warehouse.DiscussionListTable.Execute(TableOperation.Insert(new_entity));
			}
		}
		private static void deleteOldBoardList()
		{
			string pkFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "boardlist1");

			TableQuery query = new TableQuery().Where(pkFilter);

			foreach (DynamicTableEntity entity in Warehouse.SiteTable.ExecuteQuery(query))
			{
				TableOperation op = TableOperation.Delete(entity);

				Warehouse.SiteTable.Execute(op);
			}
		}
		private static void copyBoardListToSeparateTable()
		{
			string pkFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "boardlist1");

			TableQuery query = new TableQuery().Where(pkFilter);

			foreach (DynamicTableEntity entity in Warehouse.SiteTable.ExecuteQuery(query))
			{
				DynamicTableEntity new_entity = new DynamicTableEntity(entity.RowKey, "");
				if (entity.RowKey != "!info1")
				{
					new_entity["boardname"] = entity["boardname"];
					CreatorConverter.CopyEntity(entity, CreatorConverter.Status.Creator, new_entity, CreatorConverter.Status.Editor);

					Warehouse.BoardListTable.Execute(TableOperation.Insert(new_entity));
				}
			}
		}
		private static void upgradeTemporalAddBoardId()
		{
			string pkFilter = TableQuery.CombineFilters(
				TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.GreaterThanOrEqual, "bldr1.b0"),
				TableOperators.And,
				TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.LessThanOrEqual, "bldr1.b999"));

			TableQuery query = new TableQuery().Where(pkFilter);
			foreach (DynamicTableEntity entity in Warehouse.TemporalTable.ExecuteQuery(query))
			{
				string board_id;
				SandId.SplitId(entity.PartitionKey, out board_id);

				string[] parts = entity["part0"].StringValue.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

				StringBuilder builder = new StringBuilder();

				foreach (string part in parts)
				{
					builder.AppendFormat("{0}.{1},", board_id, part);
				}
				Warehouse.TemporalTable.SaveLongString(SandId.CombineId("bldr2", board_id), "segment0", builder.ToString());
			}
		}
		private static void mendMissingCreatorMId()
		{
			string pkFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.GreaterThanOrEqual, "");

			TableQuery query = new TableQuery().Where(pkFilter);

			foreach (DynamicTableEntity entity in Warehouse.SiteTable.ExecuteQuery(query))
			{
				if (entity.Properties.ContainsKey("creatoruid"))
					if (!entity.Properties.ContainsKey("creatormid"))
					{
						entity["creatormid"] = new EntityProperty(UserStore.MissingUserMId);

						Warehouse.SiteTable.Execute(TableOperation.Replace(entity));
					}
			}
		}
		private static void upgradeFlags()
		{
			string pkFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.GreaterThanOrEqual, "");

			TableQuery query = new TableQuery().Where(pkFilter).Select(new string[] { "flags" });

			int count = 0;

			foreach (DynamicTableEntity entity in Warehouse.DiscussionLoadTable.ExecuteQuery(query))
			{
				string flags = entity.GetFlags();
				StringBuilder builder = new StringBuilder("\n");

				SandFlags.SplitFlags(flags, (meta_title, meta_value) =>
					{
						builder.Append(meta_title + "=" + meta_value + "\n");
						count++;
					});
				if (builder.Length > 1)
				{
					entity["flags2"] = new EntityProperty(builder.ToString());
					Warehouse.DiscussionLoadTable.Execute(TableOperation.Merge(entity));
				}
			}
		}
		private static void moveForeMeta()
		{
			Regex regex = new Regex(@"^>>(.+?)\n", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant);
			//Regex regex = new Regex(@"^>>(.+?)$", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant);
			// some abstract contain only ">>(0,0)" and no "\n".

			string pkFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.GreaterThanOrEqual, "");

			TableQuery query = new TableQuery().Where(pkFilter).Select(new string[] { "flags2", "abstract" });

			int count = 0;

			foreach (DynamicTableEntity entity in Warehouse.DiscussionLoadTable.ExecuteQuery(query))
			{
				string flags = null;
				string ab = LetterConverter.GetAbstract(entity);

				if (ab != null)
					ab = regex.Replace(ab, (Match match) =>
					{
						if (match.Groups[1].Value[0] == '(')
						{
							flags = entity.GetFlags();
							flags = SandFlags.Add(flags, SandFlags.MT_COORDINATE, match.Groups[1].Value);
						}
						return string.Empty;
					});

				if (flags != null)
				{
					count++;

					entity["flags2"] = new EntityProperty(flags);
					entity["abstract"] = new EntityProperty(ab);

					Warehouse.DiscussionLoadTable.Execute(TableOperation.Merge(entity));

					//if (count > 0)
					//	break;
				}
			}
		}
	}
}