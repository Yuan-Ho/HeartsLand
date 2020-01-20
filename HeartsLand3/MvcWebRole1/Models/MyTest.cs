using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Diagnostics;

namespace MvcWebRole1.Models
{
	public static class MyTest
	{
		public static void TestMain()
		{
#if false
			Debug.Assert(SandFlags.CheckWithin("", 'd') == true);
			Debug.Assert(SandFlags.CheckWithin(",l2,", 'l') == true);
			Debug.Assert(SandFlags.CheckWithin(",l2,", 'd', 'l') == true);
			Debug.Assert(SandFlags.CheckWithin(",l2,", 'd') == false);
			Debug.Assert(SandFlags.CheckWithin(",d1,l2,", 'd') == false);
			Debug.Assert(SandFlags.CheckWithin(",d1,l2,", 'd', 'l') == true);
			Debug.Assert(SandFlags.CheckWithin(",d1,l2,", 'l') == false);
			Debug.Assert(SandFlags.CheckWithin(",d1,l2,", 'd', 'l', 'e') == true);
			Debug.Assert(SandFlags.CheckWithin(",d1,l2,", 'f', 'l', 'e') == false);
			Debug.Assert(SandFlags.CheckWithin(",d1,l2,", 'd', 'f', 'e') == false);

			Debug.Assert(SandFlags.Check("", 'c') == false);
			Debug.Assert(SandFlags.Check(",", 'c') == false);
			Debug.Assert(SandFlags.Check(",c0,", 'c') == true);
			Debug.Assert(SandFlags.Check(",d5,", 'c') == false);
			Debug.Assert(SandFlags.Check(",d55555,e6,", 'c') == false);
			Debug.Assert(SandFlags.Check(",d5,e6666,c7,", 'c') == true);
			Debug.Assert(SandFlags.Check(",d5,e6,c77,f8,", 'c') == true);
			Debug.Assert(SandFlags.Check(",d5,e6,f88,", 'c') == false);

			Debug.Assert(SandFlags.Add("", "c3") == ",c3,");
			Debug.Assert(SandFlags.Add(",", "c34") == ",c34,");
			Debug.Assert(SandFlags.Add(",d4,", "c355") == ",d4,c355,");
			Debug.Assert(SandFlags.Add(",d4,e5,", "c3666") == ",d4,e5,c3666,");

			Debug.Assert(SandFlags.Remove("", 'c') == "");
			Debug.Assert(SandFlags.Remove(",", 'c') == "");
			Debug.Assert(SandFlags.Remove(",d5,", 'c') == ",d5,");
			Debug.Assert(SandFlags.Remove(",d5,e6,", 'c') == ",d5,e6,");
			Debug.Assert(SandFlags.Remove(",c2,", 'c') == "");
			Debug.Assert(SandFlags.Remove(",c0,d5,", 'c') == ",d5,");
			Debug.Assert(SandFlags.Remove(",d5,c1,", 'c') == ",d5,");
			Debug.Assert(SandFlags.Remove(",d5,c18,f9,", 'c') == ",d5,f9,");
			Debug.Assert(SandFlags.Remove(",c199,d5,f9,", 'c') == ",d5,f9,");
			Debug.Assert(SandFlags.Remove(",d5,f9,c1777,", 'c') == ",d5,f9,");

			Debug.Assert(SandFlags.Merge("", "") == "");
			Debug.Assert(SandFlags.Merge(",g6,", "") == ",g6,");
			Debug.Assert(SandFlags.Merge("", ",c1,") == ",c1,");
			Debug.Assert(SandFlags.Merge(",g6,", ",c1,") == ",g6,c1,");
			Debug.Assert(SandFlags.Merge("", ",c1,d2,") == ",c1,d2,");
			Debug.Assert(SandFlags.Merge(",e3,", ",c1,d2,") == ",e3,c1,d2,");
			Debug.Assert(SandFlags.Merge(",e3,f4,", ",c1,d2,") == ",e3,f4,c1,d2,");
			Debug.Assert(SandFlags.Merge(",e3,f4,c99,", ",c1,d2,") == ",e3,f4,c1,d2,");
			Debug.Assert(SandFlags.Merge(",c500,e3,f4,", ",c1,d2,") == ",e3,f4,c1,d2,");
			Debug.Assert(SandFlags.Merge(",e3,f4,c88,", ",c1,d2,") == ",e3,f4,c1,d2,");
			Debug.Assert(SandFlags.Merge(",d2,e3,f4,c88,", ",c1,d2,") == ",e3,f4,c1,d2,");
			Debug.Assert(SandFlags.Merge(",e3,d77,f4,c88,", ",c1,d2,") == ",e3,f4,c1,d2,");
			Debug.Assert(SandFlags.Merge(",e3,f4,d555,c88,", ",c1,d2,") == ",e3,f4,c1,d2,");

			Debug.Assert(SandFlags.Merge("", ",h0,") == "");
			Debug.Assert(SandFlags.Merge(",g6,", ",h0,") == ",g6,");
			Debug.Assert(SandFlags.Merge(",g6,h0,", ",h0,") == ",g6,");
			Debug.Assert(SandFlags.Merge(",h1,g6,", ",h0,") == ",g6,");
			Debug.Assert(SandFlags.Merge(",k7,h335,g6,", ",h0,") == ",k7,g6,");
			Debug.Assert(SandFlags.Merge(",k7,g666,h335,", ",h0,") == ",k7,g666,");
			Debug.Assert(SandFlags.Merge(",h335,k77,g6,", ",h0,") == ",k77,g6,");

			Debug.Assert(SandFlags.Merge(",h335,k77,g6,", ",h0,g0,") == ",k77,");
			Debug.Assert(SandFlags.Merge(",h335,k77,g6,", ",k0,g0,") == ",h335,");
			Debug.Assert(SandFlags.Merge(",h3,k7,g6,", ",k0,g7,h4,") == ",g7,h4,");
			Debug.Assert(SandFlags.Merge(",h3,k7,g6,", ",k0,g7,h4,e9,") == ",g7,h4,e9,");
			Debug.Assert(SandFlags.Merge(",a1,b2,c3,d4,e5,", ",g7,e1,d0,b0,a5,f6") == ",c3,g7,e1,a5,f6,");

			Debug.Assert(SandId.CountBoardList("b3a") == -1);
			Debug.Assert(SandId.CountBoardList("ba") == -1);
			Debug.Assert(SandId.CountBoardList("c,b5") == -1);
			Debug.Assert(SandId.CountBoardList("b70,ba") == -1);
			Debug.Assert(SandId.CountBoardList("b70,ba,") == -1);
	
			Debug.Assert(SandId.CountBoardList("") == 0);
			Debug.Assert(SandId.CountBoardList(",") == -1);
			Debug.Assert(SandId.CountBoardList(",,") == -1);
			Debug.Assert(SandId.CountBoardList("c") == -1);
			Debug.Assert(SandId.CountBoardList("b") == -1);
			Debug.Assert(SandId.CountBoardList("b3") == 1);
			Debug.Assert(SandId.CountBoardList(",b3") == -1);
			Debug.Assert(SandId.CountBoardList("b3,") == 1);
			Debug.Assert(SandId.CountBoardList("b30") == 1);
			Debug.Assert(SandId.CountBoardList("b30,c") == -1);
			Debug.Assert(SandId.CountBoardList("b30,b") == -1);
			Debug.Assert(SandId.CountBoardList("b30,b50") == 2);
			Debug.Assert(SandId.CountBoardList(",b30,b50") == -1);
			Debug.Assert(SandId.CountBoardList("b30,b50,") == 2);
			Debug.Assert(SandId.CountBoardList("b30,,b50") == -1);
			Debug.Assert(SandId.CountBoardList("b30,b50,b800") == 3);

			LinkedList<string> list = new LinkedList<string>();

			string text = Util.Serialize(list);
			LinkedList<string> list2 = Util.DeserializeToLinkedList(text);

			list.AddFirst("d");

			text = Util.Serialize(list);
			list2 = Util.DeserializeToLinkedList(text);

			DiscussionSummary ds = new DiscussionSummary("b1026", "d11");

			StringWriter sw = new StringWriter();
			ds.Write(sw);
			string str = sw.ToString();

			LinkedList<string> q = RollManager.GetLatest("b1026");
			RollManager.PutLatest("b1026", "d90");
			RollManager.PutLatest("b1026", "d3");
			RollManager.PutLatest("b1026", "d77");
			RollManager.PutLatest("b1026", "d10");
			RollManager.PutLatest("b1026", "d77");

			q = RollManager.GetLatest("b1000");
			RollManager.PutLatest("b1000", "d1");
			RollManager.PutLatest("b1000", "d1");
			RollManager.PutLatest("b1000", "d10");
			RollManager.PutLatest("b1000", "d5");
			RollManager.PutLatest("b1000", "d1");


			List<string> list = MvcWebRole1.Models.Store.GetLastDiscussions("b1026", 5);
            string board_id = Store.CreateBoard("測試板");
#endif
		}
	}
}
