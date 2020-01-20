using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.IO;
using System.Security.Cryptography;

namespace MvcWebRole1.Models
{
	public static class HeartsConfiguration
	{
		public const int WORDS_FIELD_MAX_LEN = 1000;
		public const int WORDS_FIELD_INPUT_LIMIT = 5000;
		public const int WORDS_FIELD_EDIT_MAX_LEN = (int)(WORDS_FIELD_MAX_LEN * 1.2);

		public const int CREATOR_FIELD_MIN_LEN = 2;
		public const int CREATOR_FIELD_MAX_LEN = 15;

		public const int HEADING_FIELD_MIN_LEN = 2;
		public const int HEADING_FIELD_MAX_LEN = 100;

		public const int BOARD_NAME_FIELD_MIN_LEN = 2;
		public const int BOARD_NAME_FIELD_MAX_LEN = 18;

		public const int USER_NAME_FIELD_MIN_LEN = 3;
		public const int USER_NAME_FIELD_MAX_LEN = 24;

		public const float LENGTH_CHECK_MARGIN = 1.1F;

		public const int MAX_NUM_OF_BOARDS_IN_A_SELECTION = 100;
		public const int MAX_NUM_OF_USERS_IN_A_GROUP = 100;
		public const int MAX_NUM_OF_LETTERS_IN_A_DISCUSSION = 1000;

		public const int NEXT_ID_INITIAL_VALUE = 1000;
		public const int DEFAULT_CACHE_SECONDS = 15 * 60;
		public const int SHORT_CACHE_SECONDS = 10;
		public const int STATIC_CACHE_SECONDS = 30 * 24 * 60 * 60;

		public const string SITE_NAME = "心船";
	}
	[Serializable]
	public class ProgramLogicException : ApplicationException
	{
		public ProgramLogicException() : base() { }
		public ProgramLogicException(string message) : base(message) { }
	}
	public class FlagOperation
	{
		public enum Type
		{
			Merge,
			Multiply,
			Add,
			AddMultiple,
		}
		public Type type;

		public string MetaTitle;
		public int N;

		public string DeltaFlags;
	}
	public class FlagMergeOperation : FlagOperation
	{
		public FlagMergeOperation(string delta_flags)
		{
			base.type = Type.Merge;
			base.DeltaFlags = delta_flags;
		}
	}
	public class SandFlags
	{
		public const char FLAGS_SEPARATOR = '\n';
		public const string FLAGS_SEPARATOR_STR = "\n";
		public const string MTV_SEPARATOR = "=";

		public const string MT_DELETED = "d";		// d1..d99=delete, d-1..d-99=undelete.
		public const string MT_LAYOUT = "l";		// layout: 0=absence=default=vertical, 2=horizontal, 1/3/4=vertical English 0/45/90 degrees.
		public const string MT_AUTHORIZATION = "a";
		public const string MT_LOW_KEY = "k";		// k1=low-key, k0=high-key.
		public const string MT_REPLY_TO = "r";		// r1..r1000. number is the sn of the letter being repled to.
		public const string MT_PERMANENT_DELETE = "p";		// p1=permanently delete.
		public const string MT_VIEW = "v";		// v2=map view. v3=sky view, v4=scribble view.
		public const string MT_REPORT = "o";		// o1=report. o0=unreport.

		public const string MT_ILI = "i";		// i1=I like it.
		public const string MT_IDLI = "s";		// s1=I don't like it.
		public const string MT_COORDINATE = "co";		// map:(48.4315783,79.4036865)+14. sky:(-14.5,370).
		public const string MT_LETTER_TYPE = "lt";		// lt1=image.

		public static bool Check(string flags, string meta_title, int num)
		{
			string check_flag = FLAGS_SEPARATOR_STR + meta_title + MTV_SEPARATOR + num.ToString() + FLAGS_SEPARATOR_STR;

			if (flags.IndexOf(check_flag) != -1)
				return true;
			return false;
		}
		public static bool Check(string flags, string meta_title)
		{
			string check_flag = FLAGS_SEPARATOR_STR + meta_title + MTV_SEPARATOR;

			if (flags.IndexOf(check_flag) != -1)
				return true;
			return false;
		}
		public static string Add(string flags, string meta_title, int num)
		{
			//string flag = meta_title + num.ToString();
			return Add(flags, meta_title, num.ToString());
		}
		public static string Add(string flags, string meta_title, string meta_value)
		{
			if (flags.Length == 0)
				flags += SandFlags.FLAGS_SEPARATOR;
			flags += meta_title + MTV_SEPARATOR + meta_value;
			flags += SandFlags.FLAGS_SEPARATOR;

			return flags;
		}
		public static string Remove(string flags, string meta_title)
		{
			string check_flag = FLAGS_SEPARATOR_STR + meta_title + MTV_SEPARATOR;

			int start = flags.IndexOf(check_flag);
			if (start == -1)
				goto LEAVE;

			int end = flags.IndexOf(FLAGS_SEPARATOR, start + 1);
			flags = flags.Substring(0, start) + flags.Substring(end);

		LEAVE:
			if (flags.Length == 1)
				flags = string.Empty;

			return flags;
		}
		public static int GetNumber(string flags, string meta_title)
		{
			string check_flag = FLAGS_SEPARATOR_STR + meta_title + MTV_SEPARATOR;

			int start = flags.IndexOf(check_flag);
			if (start == -1)
				return 0;

			int end = flags.IndexOf(FLAGS_SEPARATOR, start + 1);
			start += check_flag.Length;

			string number_text = flags.Substring(start, end - start);

			return int.Parse(number_text);
		}
		public static string Operate(string flags, FlagOperation op)
		{
			if (op.type == FlagOperation.Type.Merge)
			{
				SplitFlags(op.DeltaFlags, (meta_title, meta_value) =>
				{
					if (Check(flags, meta_title))
						flags = Remove(flags, meta_title);

					if (meta_value != "0")		// 0 = default = absence.
						flags = Add(flags, meta_title, meta_value);
				});
			}
			else if (op.type == FlagOperation.Type.AddMultiple)
			{
				SplitFlags(op.DeltaFlags, (meta_title, meta_value) =>
				{
					int num2 = int.Parse(meta_value);

					int num = GetNumber(flags, meta_title);
					flags = Remove(flags, meta_title);

					if (num + num2 != 0)
						flags = Add(flags, meta_title, num + num2);
				});
			}
			else if (op.type == FlagOperation.Type.Add)
			{
				int num = GetNumber(flags, op.MetaTitle);
				flags = Remove(flags, op.MetaTitle);

				if (num + op.N != 0)
					flags = Add(flags, op.MetaTitle, num + op.N);
			}
			else if (op.type == FlagOperation.Type.Multiply)
			{
				int num = GetNumber(flags, op.MetaTitle);
				if (num != 0)
				{
					flags = Remove(flags, op.MetaTitle);
					flags = Add(flags, op.MetaTitle, num * op.N);
				}
			}
			return flags;
		}
		public static void CheckLevel(string flags, string meta_title, string delta_flags)
		{
			int old_num = GetNumber(flags, meta_title);
			int new_num = GetNumber(delta_flags, meta_title);

			if (Math.Abs(new_num) < Math.Abs(old_num))
				Util.ThrowUnauthorizedException("權限不足。需要權限等級" + Math.Abs(old_num) +
												"，您的權限等級為" + Math.Abs(new_num) + "。");
		}
		public static bool CheckWithinAllowed(string delta_flags, string[] allowed_flags, string[] allowed_meta_titles)
		{
			bool ret = true;

			SplitFlags(delta_flags, (meta_title, meta_value) =>
			{
				if (Array.IndexOf<string>(allowed_flags, meta_title + MTV_SEPARATOR + meta_value) == -1 &&
					(allowed_meta_titles == null || Array.IndexOf<string>(allowed_meta_titles, meta_title) == -1))
					ret = false;
			});
			return ret;
		}
		public static void SplitFlags(string text, Action<string, string> callback)
		{
			SandId.SplitWithCallback2(text, FLAGS_SEPARATOR, flag =>
			{
				int start = flag.IndexOf(MTV_SEPARATOR);
				if (start != -1)
				{
					string meta_title = flag.Substring(0, start);
					string meta_value = flag.Substring(start + 1);
					callback(meta_title, meta_value);
				}
			});
		}
	}
	public class SandId
	{
		public const char SPECIAL_KEY_PREFIX = '!';
		public const char BOARD_ID_CHAR = 'b';
		public const char DISCUSSION_ID_CHAR = 'd';
		public const char SELECTION_ID_CHAR = 's';

		public const string BOARD_ID_PREFIX = "b";
		public const string DISCUSSION_ID_PREFIX = "d";
		public const string LETTER_ID_PREFIX = "e";
		public const string REVISION_ID_PREFIX = "v";
		public const string SELECTION_ID_PREFIX = "s";
		public const string GROUP_ID_PREFIX = "g";
		public const string USER_ID_PREFIX = "u";

		public const string HEADING_LETTER_ID = LETTER_ID_PREFIX + "0";
		public const string FIRST_SUBJECT_LETTER_ID = LETTER_ID_PREFIX + "1";

		public static string MakeDiscussionId(int id)
		{
			return DISCUSSION_ID_PREFIX + id.ToString();
		}
		public static string MakeBoardId(int id)
		{
			return BOARD_ID_PREFIX + id.ToString();
		}
		public static string MakeSelectionId(int id)
		{
			return SELECTION_ID_PREFIX + id.ToString();
		}
		public static string MakeLetterId(int id)
		{
			return LETTER_ID_PREFIX + id.ToString();
		}
		public static string MakeRevisionId(int id)
		{
			return REVISION_ID_PREFIX + id.ToString();
		}
		public static bool IsBoardId(string board_id)
		{
			return board_id.Length != 0 && board_id[0] == BOARD_ID_CHAR && Util.IsNumber(board_id, 1, board_id.Length);
		}
		public static bool IsSelectionId(string text, int start, int end/*exclusive*/)
		{
			return text[start] == SELECTION_ID_CHAR && Util.IsNumber(text, start + 1, end);
		}
		public static bool IsLau(string user_name)
		{
			return string.IsNullOrEmpty(user_name) || user_name[0] == '_';
		}
		public static int ExtractIdNumber(string text)
		{
			return int.Parse(text.Substring(1));
		}
		public static int/*count*/ CountUserNameList(string text)
		{
			int count = 0;

			SplitWithCallback(text, user_name =>
			{
				if (!UserStore.IsValidUserName(user_name))
					Util.ThrowBadRequestException("使用者(" + user_name + ")不存在。");

				count++;
				return false;
			});
			return count;
		}
		public static int/*count*/ CountBoardList(string text)
		{
#if OLD
			int p = 0, q;
			int count = 0;

			while (p < text.Length)
			{
				q = text.IndexOf(',', p);
				if (q == -1)
					q = text.Length;
				if (!IsBoardId(text, p, q) || !Warehouse.BsMapPond.Get().IsValidBoardId(text.Substring(p, q - p)))
					return -1;
				count++;
				p = q + 1;
			}
			return count;
#else
			int count = 0;

			if (SplitWithCallback(text, board_id =>
			{
				if (!IsBoardId(board_id) || !Warehouse.BsMapPond.Get().IsValidBoardId(board_id))
					return true;
				count++;
				return false;
			}))
				return -1;
			return count;
#endif
		}
		public static bool/*prematurely stopped*/ SplitWithCallback(string text,
																	Func<string, bool/*true to stop enumeration*/> callback)
		{
			int p = 0, q;
			int count = 0;

			while (p < text.Length)
			{
				q = text.IndexOf(',', p);
				if (q == -1)
					q = text.Length;
				if (callback(text.Substring(p, q - p)))
					return true;
				count++;
				p = q + 1;
			}
			return false;
		}
		public static void SplitWithCallback2(string text, char separator, Action<string> callback)
		{
			int p = 0, q;

			while (p < text.Length)
			{
				q = text.IndexOf(separator, p);
				if (q == -1)
					q = text.Length;

				if (q - p > 0)
					callback(text.Substring(p, q - p));
				
				p = q + 1;
			}
		}
		public static string CombineId(string part1, string part2)
		{
			return part1 + '.' + part2;
		}
		public static string CombineId(string part1, string part2, string part3)
		{
			return part1 + '.' + part2 + '.' + part3;
		}
		public static string CombineId(string part1, string part2, string part3, string part4)
		{
			return part1 + '.' + part2 + '.' + part3 + '.' + part4;
		}
		public static string SplitId(string id, out string part2)
		{
			string[] arr = id.Split('.');
			part2 = arr[1];
			return arr[0];
		}
		public static string SplitId(string id, out string part2, out string part3)
		{
			string[] arr = id.Split('.');
			part2 = arr[1];
			part3 = arr[2];
			return arr[0];
		}
	}
	public static class Convertor
	{
		public static string ToString(DateTimeOffset? dto)
		{
#if NOT_USED
			return dto.Value.ToOffset(new TimeSpan(8, 0, 0)).ToString("yyyy/MM/dd(ddd) HH:mm:ss.ff");
#else
			return dto.Value.ToOffset(new TimeSpan(8, 0, 0)).ToString("o");
#endif
		}
	}
	public static class CryptUtil
	{
		public static string GenerateKey()
		{
			byte[] kiv = new byte[16];
			RandomNumberGenerator.Create().GetBytes(kiv);

			return Convert.ToBase64String(kiv);
		}
		public static string Encrypt(string data, string key)
		{
			byte[] kiv = Convert.FromBase64String(key);
			return Convert.ToBase64String(Encrypt(Encoding.UTF8.GetBytes(data), kiv, kiv));
		}
		public static string Decrypt(string data, string key)
		{
			byte[] kiv = Convert.FromBase64String(key);
			return Encoding.UTF8.GetString(Decrypt(Convert.FromBase64String(data), kiv, kiv));
		}
		public static byte[] Encrypt(byte[] data, byte[] key, byte[] iv)
		{
			using (Aes algorithm = Aes.Create())
			using (ICryptoTransform encryptor = algorithm.CreateEncryptor(key, iv))
				return Crypt(data, encryptor);
		}
		public static byte[] Decrypt(byte[] data, byte[] key, byte[] iv)
		{
			using (Aes algorithm = Aes.Create())
			using (ICryptoTransform decryptor = algorithm.CreateDecryptor(key, iv))
				return Crypt(data, decryptor);
		}
		static byte[] Crypt(byte[] data, ICryptoTransform cryptor)
		{
			MemoryStream m = new MemoryStream();
			using (Stream c = new CryptoStream(m, cryptor, CryptoStreamMode.Write))
				c.Write(data, 0, data.Length);
			return m.ToArray();
		}
	}
	public static class HtmlUtil
	{
		public static void WriteRaw(this TextWriter writer, string content)
		{
			writer.Write(content);		// beware of xss attack.
		}
		public static void WriteStartTag(this TextWriter writer, string type)
		{
			writer.Write("<");
			writer.Write(type);
			writer.Write(">");
		}
		public static void WriteEndTag(this TextWriter writer, string type)
		{
			writer.Write("</");
			writer.Write(type);
			writer.Write(">");
		}
		public static void WriteForCrawler(this TextWriter writer, string type, string content)
		{
			WriteStartTag(writer, type);
			writer.Write(content);		// beware of xss attack.
			WriteEndTag(writer, type);
		}
		public static void WriteBoardAnchor(this TextWriter writer, string board_id, string board_name)
		{
			writer.Write("<a href='/");
			writer.Write(board_id);
			//writer.Write('/');
			//HttpContext.Current.Server.UrlEncode(board_name, writer);
			writer.Write("'>");
			writer.Write(board_name);		// beware of xss attack.
			writer.Write("</a>");
		}
		public static void WriteDiscussionAnchor(this TextWriter writer, string board_id, string discussion_id,
													string heading, ViewType view_type)
		{
			writer.Write("<a href='/");
			writer.Write(board_id);
			writer.Write('/');
			writer.Write(discussion_id);
			if (view_type != ViewType.horizontal)
				writer.Write("?view=" + view_type.ToString());
			writer.Write("'>");
			writer.Write(heading);		// beware of xss attack.
			writer.Write("</a>");
		}
		public static void WriteAnchor(this TextWriter writer, string link, string content)
		{
			if (link != null)
			{
				writer.Write("<a href='");
				writer.Write(link);		// beware of xss attack.
				writer.Write("'>");
			}
			else
				writer.Write("<a>");
			writer.Write(content);		// beware of xss attack.
			writer.Write("</a>");
		}
		public static string MakeLetterLink(string board_id, string discussion_id, string letter_id, ViewType view_type)
		{
			StringBuilder builder = new StringBuilder();
			builder.AppendFormat("/{0}/{1}", board_id, discussion_id);

			if (view_type != ViewType.horizontal)
				builder.Append("?view=" + view_type.ToString());

			if (letter_id != SandId.HEADING_LETTER_ID)
			{
				builder.Append('#');

				//if (view_is_map)
				if (view_type != ViewType.horizontal)
					builder.Append("g_");

				builder.Append(letter_id);
			}
			return builder.ToString();

			//return string.Format("/{0}/{1}{2}#{3}", board_id, discussion_id, view_is_map ? "?view=map" : string.Empty, letter_id);
		}
		public static string MakeUserLink(int user_id)
		{
			return "/" + SandId.USER_ID_PREFIX + user_id.ToString();
		}
	}
	public static class Util
	{
		public const string JsonValueDelimiter = ",";
		public static char[] WordsSplitCharacters = new char[] { '\n', '」', '。' };

		public static string DateTimeToString(DateTime dt, int type)
		{
			if (type == 1)
				return dt.ToString("yyyy/MM/dd HH:mm:ss");
			else if (type == 2)
				return dt.ToString("yyyy-MM-dd HH:mm:ss");
			else if (type == 3)
				return dt.ToString("yyyy-MM-dd");
			else if (type == 4)
				return dt.ToString("yyyyMMdd");
			else if (type == 5)
				return dt.ToString("yyyyMM");
			else if (type == 6)
				return dt.ToString("yyyyMMdd_HHmmss");
			else
				throw new ProgramLogicException();
		}
		public static string RandomAlphaNumericString(int len)
		{
			string text = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
			StringBuilder builder = new StringBuilder(len);
			//Random random = new Random();		// sometimes cause same random number sequence.

			for (int i = 0; i < len; i++)
				builder.Append(text[Warehouse.Random.Next(text.Length)]);
			return builder.ToString();
		}

		public static string Print<T>(IEnumerable<T> list)
		{
			StringBuilder builder = new StringBuilder();
			bool first = true;

			foreach (T t in list)
			{
				if (first)
					first = false;
				else
					builder.Append(", ");

				builder.Append(t.ToString());
			}
			return builder.ToString();
		}

		public static string Serialize(LinkedList<string> list)
		{
			StringBuilder builder = new StringBuilder();
			foreach (string s in list)
			{
				builder.Append(s);
				builder.Append(',');
			}
			return builder.ToString();
		}
		public static LinkedList<string> DeserializeToLinkedList(string text)
		{
			LinkedList<string> list = new LinkedList<string>();

			int start = 0;
			int i = text.IndexOf(',', start);

			while (i != -1)
			{
				list.AddLast(text.Substring(start, i - start));
				start = i + 1;
				i = text.IndexOf(',', start);
			}
			return list;
		}
		public static string ValidationSummary(ModelStateDictionary dict)
		{
			StringBuilder builder = new StringBuilder();

			foreach (ModelState ms in dict.Values)
				foreach (ModelError me in ms.Errors)
				{
					builder.Append(me.ErrorMessage);
				}
			return builder.ToString();
		}

		public static bool IsAjaxRequest(HttpRequestBase request)
		{
			// Used to prevent CSRF attack. Ajax post is subject to same-origin policy. Form post cannot add header.

			return request.Headers != null && request.Headers["X-Requested-With"] == "XMLHttpRequest";
		}
#if OLD
		public static bool WithinCharSetUserName(string text)
		{
			// throws null reference exception if text is null.
			foreach (char c in text)
			{
				if (c <= 0x1F || c == 0x7F)
					return false;
				
				// if ("\"\'\\*+?|^$#&<>/%~`;:@=[]{}().!".IndexOf(c) != -1)		// Allow only " ,-_".

				if ("\"\'\\*+?|^$#&<>/%~`;:@=[]{}()!,".IndexOf(c) != -1)		// Allow only " .-_".
					return false;
			}
			return true;
		}
#else
		public static bool WithinCharSetUserName(string text)
		{
			for (int i = 0; i < text.Length; i++)
			{
				char code = text[i];

				if (code >= 0x80) continue;

				if (code >= 0x61 && code <= 0x7A) continue;		// a..z
				if (code >= 0x41 && code <= 0x5A) continue;		// A..Z
				if (code >= 0x30 && code <= 0x39) continue;		// 0..9

				if (i == 0 || i == text.Length - 1) return false;

				if (" .-_".IndexOf(code) == -1) return false;
			}
			return true;
		}
#endif
		public static bool IsNumber(char ch)
		{
			return ch >= '0' && ch <= '9';
		}
		public static bool IsNumber(string str, int start)
		{
			return IsNumber(str, start, str.Length);
		}
		public static bool IsNumber(string str, int start, int end/*exclusive*/)
		{
			if (end > str.Length)
				end = str.Length;

			if (start >= end)
				return false;

			while (start < end)
			{
				char ch = str[start];
				if (ch < '0' || ch > '9')
					return false;
				start++;
			}
			return true;
		}

		public static void ThrowBadRequestException(string err_msg)
		{
			throw new HttpException((int)HttpStatusCode.BadRequest, err_msg);
		}
		public static void ThrowUnauthorizedException(string err_msg)
		{
			throw new HttpException((int)HttpStatusCode.Unauthorized, err_msg);		// it becomes 500 internal server error.
		}
		public static void ThrowAttackException(string err_msg)
		{
			throw new HttpException((int)HttpStatusCode.BadRequest, err_msg);
		}
		public static string[] SplitLongWords(string words)
		{
			List<string> arr = new List<string>();
			int idx = 0;

			while (true)
			{
				if (words.Length - idx > HeartsConfiguration.WORDS_FIELD_MAX_LEN)
				{
					int p = words.LastIndexOfAny(WordsSplitCharacters, idx + HeartsConfiguration.WORDS_FIELD_MAX_LEN);
					if (p == -1 || p - idx < (HeartsConfiguration.WORDS_FIELD_MAX_LEN/* * 3 / 4*/ / 10))
						Util.ThrowBadRequestException("單一段落過長。");

					//p++;		//p++的話，會切在符號後面，會讓句子看起來結束了，沒有硬被分段的感覺。
					//p = idx + HeartsConfiguration.WORDS_FIELD_MAX_LEN;

					arr.Add(words.Substring(idx, p - idx));
					idx = p;
				}
				else
				{
					if (idx < words.Length)
						arr.Add(words.Substring(idx));
					return arr.ToArray();
				}
			}
		}
	}
}