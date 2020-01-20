using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage.Table;
using System.IO;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Net;

namespace MvcWebRole1.Models
{
	public class ErrorResult : ActionResult
	{
		private string errorMessage;

		public ErrorResult(string error_message)
		{
			this.errorMessage = error_message;
		}
		public override void ExecuteResult(ControllerContext context)
		{
			HttpResponseBase response = context.HttpContext.Response;
			response.StatusCode = (int)HttpStatusCode.BadRequest;
			response.Write(this.errorMessage);
		}
		// On local the response body is error_message (no problem). On remote (Azure WebSites) the response body becomes "Bad Request".
	}
	public class DownloadBlobResult : ActionResult
	{
		private string blobName;

		public DownloadBlobResult(string folder_name, string file_name)
		{
			this.blobName = folder_name + "/" + file_name;
		}
		public override void ExecuteResult(ControllerContext context)
		{
			CloudBlockBlob block_blob = Warehouse.ImagesContainer.GetBlockBlobReference(blobName);

			block_blob.FetchAttributes();
			context.HttpContext.Response.ContentType = block_blob.Properties.ContentType;

			block_blob.DownloadToStream(context.HttpContext.Response.OutputStream);
		}
	}
	public class LatestDiscussionSummaryResult : ActionResult
	{
		private string boardId;		// Null for whole site.

		public LatestDiscussionSummaryResult(string board_id/*null for whole site*/)
		{
			//this.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
			this.boardId = board_id;
		}
		public override void ExecuteResult(ControllerContext context)
		{
			string[] ids = Warehouse.HotDiscussionRollPond.Get(boardId).ToArray();

			TextWriter writer = context.HttpContext.Response.Output;
			//using (JsonTextWriter writer = new JsonTextWriter(context.HttpContext.Response.Output))
			{
				//writer.WriteStartObject();
				foreach (string id in ids)
				{
					string discussion_id;
					string board_id = SandId.SplitId(id, out discussion_id);

					DiscussionSummary ds = Warehouse.DiscussionSummaryPond.Get(board_id, discussion_id);
					ds.Write(writer);
				}
				//writer.WriteEndObject();
			}
			//base.ExecuteResult(context);
		}
	}
	public class DiscussionLoadResult : ActionResult
	{
		private readonly string boardId;
		private readonly string discussionId;

		public DiscussionLoadResult(string board_id, string discussion_id)
		{
			this.boardId = board_id;
			this.discussionId = discussion_id;
		}
		public override void ExecuteResult(ControllerContext context)
		{
			//string board_name = Warehouse.BsMapPond.Get().GetBoardName(boardId);
			//string partition_key = SandId.CombineId(boardId, discussionId);

			TextWriter writer = context.HttpContext.Response.Output;
			//writer.WriteForCrawler("h1", board_name);

			DiscussionLoadRoll dlr = Warehouse.DiscussionLoadPond.Get(boardId, discussionId);

			writer.Write(dlr.Output);
			writer.WriteForCrawler("var", Util.DateTimeToString(dlr.LastModifiedTime, 6));
		}
	}
	public class SelectionBoardListResult : ActionResult
	{
		public SelectionBoardListResult()
		{
		}
		public override void ExecuteResult(ControllerContext context)
		{
			TextWriter writer = context.HttpContext.Response.Output;
			writer.Write(Warehouse.BsMapPond.Get().Output);
		}
	}
	public class DiscussionListResult : ActionResult
	{
		private readonly List<string> boardList;
		private readonly string selectionId;

		public DiscussionListResult(string board_id/*or selection id*/)
		{
			//this.JsonRequestBehavior = JsonRequestBehavior.AllowGet;

			if (SandId.IsSelectionId(board_id, 0, board_id.Length))
			{
				this.selectionId = board_id;
				this.boardList = Warehouse.BsMapPond.Get().GetBoardList(this.selectionId);		// null for nonexistent selection.
			}
			else
			{
				this.boardList = new List<string>(1);
				this.boardList.Add(board_id);
			}
		}
		public override void ExecuteResult(ControllerContext context)
		{
			TextWriter writer = context.HttpContext.Response.Output;
			//using (JsonTextWriter writer = new JsonTextWriter(context.HttpContext.Response.Output))
			//{

			if (selectionId != null)
			{
				//string selection_name = SelectionInfoStore.GetSelectionName(this.selectionId);
				string selection_name = Warehouse.BsMapPond.Get().GetSelectionName(this.selectionId);
				//writer.WritePropertyName("selection_name");
				//writer.WriteValue(selection_name);
				writer.WriteForCrawler("h1", selection_name);
			}
			foreach (string board_id in this.boardList)
			{
				DiscussionList dl = Warehouse.DiscussionListPond.Get(board_id);
				dl.Write(writer);
			}
			//}
			//base.ExecuteResult(context);
		}
	}
}