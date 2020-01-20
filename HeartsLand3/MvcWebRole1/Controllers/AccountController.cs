using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using DotNetOpenAuth.AspNet;
using Microsoft.Web.WebPages.OAuth;
using WebMatrix.WebData;
using MvcWebRole1.Filters;
using MvcWebRole1.Models;
using System.Diagnostics;
using Microsoft.Web.Helpers;

namespace MvcWebRole1.Controllers
{
	[Authorize]
	public class AccountController : Controller
	{
		internal void setCookie(string name, string value)
		{
			HttpCookie cookie = new HttpCookie(name, value);

			//cookie.Path = FormsAuthentication.FormsCookiePath;

			Response.Cookies.Add(cookie);
		}
		internal void deleteCookie(string name)
		{
			HttpCookie cookie = new HttpCookie(name);

			cookie.Expires = DateTime.Now.AddDays(-1);

			Response.Cookies.Add(cookie);
		}
		internal void deleteParaCookie(string name)
		{
			HttpCookie auth_cookie = Response.Cookies[".ASPXAUTH"];
			HttpCookie cookie = new HttpCookie(name);		// hearts user name

			cookie.Expires = auth_cookie.Expires;

			Response.Cookies.Add(cookie);
		}
		internal void setParaCookie(string name, string value)
		{
			HttpCookie auth_cookie = Response.Cookies[".ASPXAUTH"];
			HttpCookie cookie = new HttpCookie(name, value);		// hearts user name

			cookie.Expires = auth_cookie.Expires;

			Response.Cookies.Add(cookie);
		}
		private void onLogin(string user_name, string m_id, int user_id)
		{
			setParaCookie("hun", user_name);
			setParaCookie("huid", user_id.ToString());
			setParaCookie("m_id", m_id);
			//UserStore.OnLogin(user_name, m_id);
		}
		private void onLogout(string user_name)
		{
			deleteParaCookie("hun");
			deleteParaCookie("huid");
			deleteParaCookie("m_id");
			//UserStore.OnLogout(user_name);
		}
		[AllowAnonymous]
		public ActionResult Login(string returnUrl)
		{
			// don't cache a page that sets cookie.
			ViewBag.ReturnUrl = returnUrl;
			return View();
		}
		[HttpPost]
		[AllowAnonymous]
		public ActionResult AjaxLogin(LoginModel model)
		{
			if (!Util.IsAjaxRequest(Request)) Util.ThrowBadRequestException("Not ajax post.");

			// bool is_lau = model.UserName[0] == '_';
			bool is_lau = SandId.IsLau(model.UserName);

			if (!is_lau)
			{
				object ret_obj = Warehouse.RateLimiter.Validate(CarryType.Login);
				if (ret_obj != null) return Json(ret_obj);
			}

			if (!ModelState.IsValid)
			{
				Trace.TraceWarning("登入失敗。UserName={0}, m_id={1}.", model.UserName, model.m_id);
				Util.ThrowBadRequestException("Ajax login failed. " + Util.ValidationSummary(ModelState));
			}
			else if (WebSecurity.Login(model.UserName, model.Password, persistCookie: model.RememberMe))
			{
				int user_id = WebSecurity.GetUserId(model.UserName);

				onLogin(model.UserName, model.m_id, user_id);
				Trace.TraceInformation("登入成功。UserName={0}, m_id={1}.", model.UserName, model.m_id);
			}
			else
			{
				Trace.TraceWarning("登入失敗。UserName={0}, m_id={1}.", model.UserName, model.m_id);
				Util.ThrowBadRequestException("所提供的使用者名稱或密碼不正確。");
			}

			return Json(new { ok = true });
		}
		[HttpPost]
		[AllowAnonymous]
		public ActionResult RTH()
		{
			return RedirectToLocal("/");
		}
#if OLD
		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		public ActionResult Login(LoginModel model, string returnUrl)
		{
			if (ModelState.IsValid && WebSecurity.Login(model.UserName, model.Password, persistCookie: model.RememberMe))
			{
				setParaCookie("hun", model.UserName);

				return RedirectToLocal(returnUrl);
			}

			// 如果執行到這裡，發生某項失敗，則重新顯示表單
			ModelState.AddModelError("", "所提供的使用者名稱或密碼不正確。");
			return View(model);
		}
#endif
		// 當網站發行新版/重開，使用者瀏覽器不會知道，所以還認為自己是登入狀態。若此時執行登出，由於該login token cookie已失效，所以在server side會認為是未登入。
		// 如果沒有AllowAnonymous，則會被導到登入頁，傳回登入頁的html碼給瀏覽器。
		[HttpPost]
		[AllowAnonymous]
		public ActionResult AjaxLogOff()
		{
			if (!Util.IsAjaxRequest(Request)) Util.ThrowBadRequestException("Not ajax post.");

			WebSecurity.Logout();

			onLogout(User.Identity.Name);

			return Json(new { ok = true });
		}
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult LogOff()
		{
			WebSecurity.Logout();

			onLogout(User.Identity.Name);

			return RedirectToAction("Home", "Hearts");
		}
		[AllowAnonymous]
		public ActionResult Register()
		{
			// don't cache a page that sets cookie.
			return View();
		}
		[HttpPost]
		[AllowAnonymous]
		public ActionResult AjaxRegister(RegisterModel model)
		{
			if (!Util.IsAjaxRequest(Request)) Util.ThrowBadRequestException("Not ajax post.");

			// bool is_lau = model.UserName[0] == '_';
			bool is_lau = SandId.IsLau(model.UserName);

			if (!is_lau)
			{
				object ret_obj = Warehouse.RateLimiter.Validate(CarryType.Register);
				if (ret_obj != null) return Json(ret_obj);
			}

			string check_name = is_lau ? model.UserName.Substring(1) : model.UserName;

			if (!Util.WithinCharSetUserName(check_name)) Util.ThrowBadRequestException("使用者名稱含有不合法的字元。");

			if (ModelState.IsValid)
			{
				try
				{
					WebSecurity.CreateUserAndAccount(model.UserName, model.Password);
					WebSecurity.Login(model.UserName, model.Password);

					int user_id = WebSecurity.GetUserId(model.UserName);

					onLogin(model.UserName, model.m_id, user_id);
					Trace.TraceInformation("註冊成功。UserName={0}, m_id={1}.", model.UserName, model.m_id);
				}
				catch (MembershipCreateUserException e)
				{
					ModelState.AddModelError("", ErrorCodeToString(e.StatusCode));

					Trace.TraceWarning("註冊失敗。UserName={0}, m_id={1}.", model.UserName, model.m_id);

					// Util.ThrowBadRequestException("Ajax register failed. " + Util.ValidationSummary(ModelState));
					return new ErrorResult("Ajax register failed. " + Util.ValidationSummary(ModelState));
				}
			}
			else
				Util.ThrowBadRequestException("Ajax register failed. " + Util.ValidationSummary(ModelState));

			return Json(new { ok = true });
		}
#if OLD
		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		public ActionResult Register(RegisterModel model)
		{
			if (ModelState.IsValid)
			{
				// 嘗試註冊使用者
				try
				{
					WebSecurity.CreateUserAndAccount(model.UserName, model.Password);
					WebSecurity.Login(model.UserName, model.Password);

					setParaCookie("hun", model.UserName);

					return RedirectToAction("Home", "Hearts");
				}
				catch (MembershipCreateUserException e)
				{
					ModelState.AddModelError("", ErrorCodeToString(e.StatusCode));
				}
			}

			// 如果執行到這裡，發生某項失敗，則重新顯示表單
			return View(model);
		}
#endif
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Disassociate(string provider, string providerUserId)
		{
			string ownerAccount = OAuthWebSecurity.GetUserName(provider, providerUserId);
			ManageMessageId? message = null;

			// 只有在目前登入的使用者是擁有者時，才取消帳戶的關聯
			if (ownerAccount == User.Identity.Name)
			{
				// 使用交易，防止使用者刪除他們的最後一個登入認證
				using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.Serializable }))
				{
					bool hasLocalAccount = OAuthWebSecurity.HasLocalAccount(WebSecurity.GetUserId(User.Identity.Name));
					if (hasLocalAccount || OAuthWebSecurity.GetAccountsFromUserName(User.Identity.Name).Count > 1)
					{
						OAuthWebSecurity.DeleteAccount(provider, providerUserId);
						scope.Complete();
						message = ManageMessageId.RemoveLoginSuccess;
					}
				}
			}

			return RedirectToAction("Manage", new { Message = message });
		}
		public ActionResult Manage(ManageMessageId? message)
		{
			ViewBag.StatusMessage =
				message == ManageMessageId.ChangePasswordSuccess ? "您的密碼已變更。"
				: message == ManageMessageId.SetPasswordSuccess ? "已設定您的密碼。"
				: message == ManageMessageId.RemoveLoginSuccess ? "已移除外部登入。"
				: "";
			ViewBag.HasLocalPassword = OAuthWebSecurity.HasLocalAccount(WebSecurity.GetUserId(User.Identity.Name));
			ViewBag.ReturnUrl = Url.Action("Manage");
			return View();
		}
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Manage(LocalPasswordModel model)
		{
			bool hasLocalAccount = OAuthWebSecurity.HasLocalAccount(WebSecurity.GetUserId(User.Identity.Name));
			ViewBag.HasLocalPassword = hasLocalAccount;
			ViewBag.ReturnUrl = Url.Action("Manage");
			if (hasLocalAccount)
			{
				if (ModelState.IsValid)
				{
					// 在特定失敗狀況下，ChangePassword 會擲回例外狀況，而非傳回 false。
					bool changePasswordSucceeded;
					try
					{
						changePasswordSucceeded = WebSecurity.ChangePassword(User.Identity.Name, model.OldPassword, model.NewPassword);
					}
					catch (Exception)
					{
						changePasswordSucceeded = false;
					}

					if (changePasswordSucceeded)
					{
						return RedirectToAction("Manage", new { Message = ManageMessageId.ChangePasswordSuccess });
					}
					else
					{
						ModelState.AddModelError("", "目前密碼不正確或是新密碼無效。");
					}
				}
			}
			else
			{
				// 使用者沒有本機密碼，因此，請移除遺漏
				// OldPassword 欄位所導致的任何驗證錯誤
				ModelState state = ModelState["OldPassword"];
				if (state != null)
				{
					state.Errors.Clear();
				}

				if (ModelState.IsValid)
				{
					try
					{
						WebSecurity.CreateAccount(User.Identity.Name, model.NewPassword);
						return RedirectToAction("Manage", new { Message = ManageMessageId.SetPasswordSuccess });
					}
					catch (Exception)
					{
						ModelState.AddModelError("", String.Format("無法建立本機帳戶。名稱為 \"{0}\" 的帳戶可能已存在。", User.Identity.Name));
					}
				}
			}

			// 如果執行到這裡，發生某項失敗，則重新顯示表單
			return View(model);
		}
#if DISABLE_EXTERNAL_LOGIN
		#region ExternalLogin
		//
		// POST: /Account/ExternalLogin

		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		public ActionResult ExternalLogin(string provider, string returnUrl)
		{
			return new ExternalLoginResult(provider, Url.Action("ExternalLoginCallback", new { ReturnUrl = returnUrl }));
		}

		//
		// GET: /Account/ExternalLoginCallback

		[AllowAnonymous]
		public ActionResult ExternalLoginCallback(string returnUrl)
		{
			AuthenticationResult result = OAuthWebSecurity.VerifyAuthentication(Url.Action("ExternalLoginCallback", new { ReturnUrl = returnUrl }));
			if (!result.IsSuccessful)
			{
				return RedirectToAction("ExternalLoginFailure");
			}

			if (OAuthWebSecurity.Login(result.Provider, result.ProviderUserId, createPersistentCookie: false))
			{
				return RedirectToLocal(returnUrl);
			}

			if (User.Identity.IsAuthenticated)
			{
				// 如果目前的使用者已登入，即可新增帳戶
				OAuthWebSecurity.CreateOrUpdateAccount(result.Provider, result.ProviderUserId, User.Identity.Name);
				return RedirectToLocal(returnUrl);
			}
			else
			{
				// 使用者是新的使用者，請詢問他們想要使用什麼成員資格名稱
				string loginData = OAuthWebSecurity.SerializeProviderUserId(result.Provider, result.ProviderUserId);
				ViewBag.ProviderDisplayName = OAuthWebSecurity.GetOAuthClientData(result.Provider).DisplayName;
				ViewBag.ReturnUrl = returnUrl;
				return View("ExternalLoginConfirmation", new RegisterExternalLoginModel { UserName = result.UserName, ExternalLoginData = loginData });
			}
		}

		//
		// POST: /Account/ExternalLoginConfirmation

		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		public ActionResult ExternalLoginConfirmation(RegisterExternalLoginModel model, string returnUrl)
		{
			string provider = null;
			string providerUserId = null;

			if (User.Identity.IsAuthenticated || !OAuthWebSecurity.TryDeserializeProviderUserId(model.ExternalLoginData, out provider, out providerUserId))
			{
				return RedirectToAction("Manage");
			}

			if (ModelState.IsValid)
			{
				// 將新使用者插入資料庫
				using (UsersContext db = new UsersContext())
				{
					UserProfile user = db.UserProfiles.FirstOrDefault(u => u.UserName.ToLower() == model.UserName.ToLower());
					// 檢查使用者是否存在
					if (user == null)
					{
						// 將名稱插入設定檔表格
						db.UserProfiles.Add(new UserProfile { UserName = model.UserName });
						db.SaveChanges();

						OAuthWebSecurity.CreateOrUpdateAccount(provider, providerUserId, model.UserName);
						OAuthWebSecurity.Login(provider, providerUserId, createPersistentCookie: false);

						return RedirectToLocal(returnUrl);
					}
					else
					{
						ModelState.AddModelError("UserName", "使用者名稱已經存在。請輸入不同的使用者名稱。");
					}
				}
			}

			ViewBag.ProviderDisplayName = OAuthWebSecurity.GetOAuthClientData(provider).DisplayName;
			ViewBag.ReturnUrl = returnUrl;
			return View(model);
		}

		//
		// GET: /Account/ExternalLoginFailure

		[AllowAnonymous]
		public ActionResult ExternalLoginFailure()
		{
			return View();
		}

		[AllowAnonymous]
		[ChildActionOnly]
		public ActionResult ExternalLoginsList(string returnUrl)
		{
			ViewBag.ReturnUrl = returnUrl;
			return PartialView("_ExternalLoginsListPartial", OAuthWebSecurity.RegisteredClientData);
		}

		[ChildActionOnly]
		public ActionResult RemoveExternalLogins()
		{
			ICollection<OAuthAccount> accounts = OAuthWebSecurity.GetAccountsFromUserName(User.Identity.Name);
			List<ExternalLogin> externalLogins = new List<ExternalLogin>();
			foreach (OAuthAccount account in accounts)
			{
				AuthenticationClientData clientData = OAuthWebSecurity.GetOAuthClientData(account.Provider);

				externalLogins.Add(new ExternalLogin
				{
					Provider = account.Provider,
					ProviderDisplayName = clientData.DisplayName,
					ProviderUserId = account.ProviderUserId,
				});
			}

			ViewBag.ShowRemoveButton = externalLogins.Count > 1 || OAuthWebSecurity.HasLocalAccount(WebSecurity.GetUserId(User.Identity.Name));
			return PartialView("_RemoveExternalLoginsPartial", externalLogins);
		}
		#endregion
#endif
		#region Helper
		private ActionResult RedirectToLocal(string returnUrl)
		{
			if (Url.IsLocalUrl(returnUrl))
			{
				return Redirect(returnUrl);
			}
			else
			{
				return RedirectToAction("Home", "Hearts");
			}
		}

		public enum ManageMessageId
		{
			ChangePasswordSuccess,
			SetPasswordSuccess,
			RemoveLoginSuccess,
		}

		internal class ExternalLoginResult : ActionResult
		{
			public ExternalLoginResult(string provider, string returnUrl)
			{
				Provider = provider;
				ReturnUrl = returnUrl;
			}

			public string Provider { get; private set; }
			public string ReturnUrl { get; private set; }

			public override void ExecuteResult(ControllerContext context)
			{
				OAuthWebSecurity.RequestAuthentication(Provider, ReturnUrl);
			}
		}

		private static string ErrorCodeToString(MembershipCreateStatus createStatus)
		{
			// 請參閱 http://go.microsoft.com/fwlink/?LinkID=177550 了解
			// 狀態碼的完整清單。
			switch (createStatus)
			{
				case MembershipCreateStatus.DuplicateUserName:
					return "使用者名稱已經存在。請輸入不同的使用者名稱。";

				case MembershipCreateStatus.DuplicateEmail:
					return "該電子郵件地址的使用者名稱已經存在。請輸入不同的電子郵件地址。";

				case MembershipCreateStatus.InvalidPassword:
					return "所提供的密碼無效。請輸入有效的密碼值。";

				case MembershipCreateStatus.InvalidEmail:
					return "所提供的電子郵件地址無效。請檢查這項值，然後再試一次。";

				case MembershipCreateStatus.InvalidAnswer:
					return "所提供的密碼擷取解答無效。請檢查這項值，然後再試一次。";

				case MembershipCreateStatus.InvalidQuestion:
					return "所提供的密碼擷取問題無效。請檢查這項值，然後再試一次。";

				case MembershipCreateStatus.InvalidUserName:
					return "所提供的使用者名稱無效。請檢查這項值，然後再試一次。";

				case MembershipCreateStatus.ProviderError:
					return "驗證提供者傳回錯誤。請確認您的輸入，然後再試一次。如果問題仍然存在，請聯繫您的系統管理員。";

				case MembershipCreateStatus.UserRejected:
					return "使用者建立要求已取消。請確認您的輸入，然後再試一次。如果問題仍然存在，請聯繫您的系統管理員。";

				default:
					return "發生未知的錯誤。請確認您的輸入，然後再試一次。如果問題仍然存在，請聯繫您的系統管理員。";
			}
		}
		#endregion
	}
}
