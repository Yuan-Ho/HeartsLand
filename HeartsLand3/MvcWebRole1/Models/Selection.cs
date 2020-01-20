using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.WindowsAzure.Storage.Table;

namespace MvcWebRole1.Models
{
	public static class BoardInfoStore
	{
		private const string EMPTY_ROW_KEY = "";

		public static void CreateSkeleton()
		{
			NextIdStore.CreateIfNotExists(Warehouse.BoardListTable, null, HeartsConfiguration.NEXT_ID_INITIAL_VALUE);
		}
		public static string/*board id*/ CreateBoard(string board_name)
		{
			int next_id = NextIdStore.Next(Warehouse.BoardListTable, null);
			string board_id = SandId.MakeBoardId(next_id);

			DynamicTableEntity entity = new DynamicTableEntity(board_id, EMPTY_ROW_KEY);
			entity["boardname"] = new EntityProperty(board_name);
			entity.OperateFlags(new FlagMergeOperation(SandFlags.MT_LOW_KEY + "1"));
#if OLD
			entity["createtime"] = new EntityProperty(DateTime.Now);

			entity["creatoruid"] = new EntityProperty(WebSecurity.CurrentUserId);
			entity["creatormid"] = new EntityProperty(UserStore.CurrentUserMId);
#else
			CreatorConverter.FillEntity(entity, CreatorConverter.Status.Editor, null);
#endif
			Warehouse.BoardListTable.Execute(TableOperation.Insert(entity));

			DiscussionListStore.CreateSkeleton(board_id);
			//don't work for child action. //HttpResponse.RemoveOutputCacheItem("/boardlist");
			Warehouse.BsMapPond.Notify();

			return board_id;
		}
		public static string/*null if not exist*/ GetBoardName(string board_id)
		{
			return Warehouse.BoardListTable.GetStringProperty(board_id, EMPTY_ROW_KEY, "boardname");
		}
		public static string GetBoardFlags(string board_id)
		{
			TableResult result = Warehouse.BoardListTable.Execute(TableOperation.Retrieve(board_id, EMPTY_ROW_KEY));
			DynamicTableEntity entity = (DynamicTableEntity)result.Result;

			return entity.GetFlags();
		}
		public static void GetBoards(Action<string/*board id*/, string/*board name*/> callback)
		{
			TableQuery query = new TableQuery();

			foreach (DynamicTableEntity entity in Warehouse.BoardListTable.ExecuteQuery(query))
			{
				if (entity.PartitionKey[0] != SandId.SPECIAL_KEY_PREFIX)
				{
					callback(entity.PartitionKey/*board id*/, entity["boardname"].StringValue);
				}
			}
		}
		public static void SetBoardSetting(string board_id, string board_name)
		{
			TableResult result = Warehouse.BoardListTable.Execute(TableOperation.Retrieve(board_id, EMPTY_ROW_KEY));
			DynamicTableEntity entity = (DynamicTableEntity)result.Result;

			// if (entity != null)		// let it throw null reference exception.
			int ec = RevisionStore.IncreaseEditCount(entity);

			string partition_key = SandId.CombineId(Warehouse.BoardListTable.Name, board_id);
			RevisionStore.CreateHistory(entity, partition_key, ec, "boardname");

			CreatorConverter.FillEntity(entity, CreatorConverter.Status.Editor, null);
			entity["boardname"].StringValue = board_name;

			Warehouse.BoardListTable.Execute(TableOperation.Replace(entity));      // Throws StorageException ((412) Precondition Failed) if the entity is modified in between.
			//don't work for child action. //HttpResponse.RemoveOutputCacheItem("/boardlist");

			//List<string> selection_list = SelectionBoardListResult.GetSelectionList(board_id);

			//if (selection_list != null)
				//foreach (string selection_id in selection_list)
					//don't work for child action. //HttpResponse.RemoveOutputCacheItem("/discussionlist/" + selection_id);

			Warehouse.BsMapPond.Notify();
			Warehouse.DiscussionListPond.Notify(board_id);
			Warehouse.BoardSettingPond.Notify(board_id);
		}
		public static void SetBoardFlags(string board_id, string delta_flags)
		{
			TableResult result = Warehouse.BoardListTable.Execute(TableOperation.Retrieve(board_id, EMPTY_ROW_KEY));
			DynamicTableEntity entity = (DynamicTableEntity)result.Result;

			int ec = RevisionStore.IncreaseEditCount(entity);

			string partition_key = SandId.CombineId(Warehouse.BoardListTable.Name, board_id);
			RevisionStore.CreateHistory(entity, partition_key, ec, "flags2");

			CreatorConverter.FillEntity(entity, CreatorConverter.Status.Editor, null);
			entity.OperateFlags(new FlagMergeOperation(delta_flags));

			Warehouse.BoardListTable.Execute(TableOperation.Replace(entity));      // Throws StorageException ((412) Precondition Failed) if the entity is modified in between.

			Warehouse.BoardSettingPond.Notify(board_id);
		}
	}
	public static class SelectionInfoStore
	{
		private const string EMPTY_ROW_KEY = "";

		public static void CreateSkeleton()
		{
			NextIdStore.CreateIfNotExists(Warehouse.SelectionListTable, null, HeartsConfiguration.NEXT_ID_INITIAL_VALUE);
		}
		public static string/*selection id*/ CreateSelection(string selection_name)
		{
			int next_id = NextIdStore.Next(Warehouse.SelectionListTable, null);
			string selection_id = SandId.MakeSelectionId(next_id);

			DynamicTableEntity entity = new DynamicTableEntity(selection_id, EMPTY_ROW_KEY);
			entity["selectionname"] = new EntityProperty(selection_name);
			entity["boardlist"] = new EntityProperty(string.Empty);

			CreatorConverter.FillEntity(entity, CreatorConverter.Status.Editor, null);

			Warehouse.SelectionListTable.Execute(TableOperation.Insert(entity));

			//don't work for child action. //HttpResponse.RemoveOutputCacheItem("/boardlist");
			Warehouse.BsMapPond.Notify();

			return selection_id;
		}
		public static string/*null if not exist*/ GetSelectionName(string selection_id)
		{
			return Warehouse.SelectionListTable.GetStringProperty(selection_id, EMPTY_ROW_KEY, "selectionname");
		}
		public static void GetSelections(Action<string/*selection id*/, string/*selection name*/, string/*board list*/> callback)
		{
			TableQuery query = new TableQuery();

			foreach (DynamicTableEntity entity in Warehouse.SelectionListTable.ExecuteQuery(query))
			{
				if (entity.PartitionKey[0] != SandId.SPECIAL_KEY_PREFIX)
				{
					string board_list = entity["boardlist"].StringValue;

					callback(entity.PartitionKey/*selection id*/,
							entity["selectionname"].StringValue,
							board_list);
				}
			}
		}
		public static object GetSelectionSetting(string selection_id)
		{
			TableResult result = Warehouse.SelectionListTable.Execute(TableOperation.Retrieve(selection_id, EMPTY_ROW_KEY));
			DynamicTableEntity entity = (DynamicTableEntity)result.Result;

			// not checking null, let it throw exception.

			string selection_name = entity["selectionname"].StringValue;
			string board_list = entity["boardlist"].StringValue;

			return new
			{
				selection_name = selection_name,
				board_list = board_list
			};
		}
		public static void SetSelectionSetting(string selection_id, string board_list)
		{
			TableResult result = Warehouse.SelectionListTable.Execute(TableOperation.Retrieve(selection_id, EMPTY_ROW_KEY));
			DynamicTableEntity entity = (DynamicTableEntity)result.Result;

			// if (entity != null)		// let it throw null reference exception.
			int ec = RevisionStore.IncreaseEditCount(entity);

			string partition_key = SandId.CombineId(Warehouse.SelectionListTable.Name, selection_id);
			RevisionStore.CreateHistory(entity, partition_key, ec, "selectionname", "boardlist");

			CreatorConverter.FillEntity(entity, CreatorConverter.Status.Editor, null);
			entity["boardlist"].StringValue = board_list;

			Warehouse.SelectionListTable.Execute(TableOperation.Replace(entity));      // Throws StorageException ((412) Precondition Failed) if the entity is modified in between.
			//don't work for child action. //HttpResponse.RemoveOutputCacheItem("/boardlist");
			//don't work for child action. //HttpResponse.RemoveOutputCacheItem("/discussionlist/" + selection_id);
			//SelectionBoardListResult.Invalidate();
			Warehouse.BsMapPond.Notify();
		}
	}
}