var WORDS_FIELD_MAX_LEN = 1000;
var WORDS_FIELD_INPUT_LIMIT = 5000;
var WORDS_FIELD_EDIT_MAX_LEN = WORDS_FIELD_MAX_LEN * 1.2;

var CREATOR_FIELD_MIN_LEN = 2;
var CREATOR_FIELD_MAX_LEN = 15;
var HEADING_FIELD_MIN_LEN = 2;
var HEADING_FIELD_MAX_LEN = 100;
var BOARD_NAME_FIELD_MIN_LEN = 2;
var BOARD_NAME_FIELD_MAX_LEN = 18;
var USER_NAME_FIELD_MIN_LEN = 3;
var USER_NAME_FIELD_MAX_LEN = 24;

var MAX_NUM_OF_BOARDS_IN_A_SELECTION = 100;
var MAX_NUM_OF_USERS_IN_A_GROUP = 100;
var NICKNAME_MEMORY_CNT = 10;

var ReCaptchaPublicKey = "6Lc2Q_ESAAAAAEXsE6Nylerro5zugfa1XGpC72PU";
var ChairOwnerGroupName = "g10";
var ViceOwnerGroupName = "g11";
var InsiderGroupName = "g12";

var MT_DELETED = "d";
var MT_LAYOUT = "l";
var MT_AUTHORIZATION = "a";
var MT_LOW_KEY = 'k';
var MT_REPLY_TO = 'r';
var MT_PERMANENT_DELETE = 'p';
var MT_VIEW = 'v';
var MT_REPORT = 'o';
var MT_ILI = 'i';
var MT_IDLI = 's';
var MT_COORDINATE = "co";
var MT_LETTER_TYPE = "lt";

var g_room_menu_target;
var g_reverse_insert = true;
var g_image_cnt = 0;
var g_image_limit = 40;
var g_no_tbrl = false;

var g_last_mousedown_xy = { x: 0, y: 0 };
var MOUSE_MOVE_THRESHOLD = 5;

var g_image_thumbnail_map = {};

function strcat(s1, s2) {
	if (!s1)
		s1 = "";
	if (!s2)
		s2 = "";
	return s1 + s2;
}
function splitAndRemoveEmpty(text, delimiter) {
	var arr = text.split(delimiter);
	var arr2 = arr.filter(function (x) {
		return x.length > 0;
	});
	return arr2;
}
function isChrome() {
	if (window.chrome)
		return true;
	return false;
}
var FLAGS_SEPARATOR = "\n";
var MTV_SEPARATOR = "=";

function flagsAdd(flags, meta_title, num) {
	if (!flagsCheck(flags, meta_title, num)) {
		if (flags.length === 0)
			flags += FLAGS_SEPARATOR;
		flags += meta_title + MTV_SEPARATOR;
		flags += String(num);
		flags += FLAGS_SEPARATOR;
	}
	return flags;
}
function flagsCheck(flags, meta_title, num) {
	var check_flag = FLAGS_SEPARATOR + meta_title + MTV_SEPARATOR + String(num) + FLAGS_SEPARATOR;

	return flags.indexOf(check_flag) !== -1;
}
function flagsGet(flags, meta_title) {
	var check_flag = FLAGS_SEPARATOR + meta_title + MTV_SEPARATOR;

	var start = flags.indexOf(check_flag);
	if (start === -1)
		return null;

	var end = flags.indexOf(FLAGS_SEPARATOR, start + 1);
	start += check_flag.length;

	return flags.substr(start, end - start);

	//var regex = new RegExp(FLAGS_SEPARATOR + meta_title + MTV_SEPARATOR + "([-\\d]+)" + FLAGS_SEPARATOR);
	//var result = flags.match(regex);
	//if (result)
	//	return Number(result[1]);
	//else
	//	return 0;		// 0=absence=default.
}
function flagsGetNumber(flags, meta_title) {
	var number_text = flagsGet(flags, meta_title);

	if (number_text === null)
		return 0;		// 0=absence=default.

	return Number(number_text);
}
function isLetterDeleted(flags) {
	var delete_num = flagsGetNumber(flags, MT_DELETED);
	if (delete_num > 0)
		return true;
	if (flagsCheck(flags, MT_PERMANENT_DELETE, 1))
		return true;
	return false;
}
function fileExtension(filename) {
	var idx = filename.lastIndexOf(".");
	if (idx === -1)
		return "";
	return filename.substr(idx);		// includes ".".
}
function lastIndexOfAny(text, substring_arr, start) {
	var pos = -1;

	for (var i = 0; i < substring_arr.length; i++) {
		var p = text.lastIndexOf(substring_arr[i], start);
		if (p !== -1 && p > pos)
			pos = p;
	}
	return pos;
}
function splitLongWords(text) {
	var arr = [];
	var idx = 0;
	while (true) {
		if (text.length - idx > WORDS_FIELD_MAX_LEN) {
			//var p = text.lastIndexOf("\n", idx + WORDS_FIELD_MAX_LEN);
			var p = lastIndexOfAny(text, ['\n', '」', '。'], idx + WORDS_FIELD_MAX_LEN);

			if (p === -1 || p - idx < (WORDS_FIELD_MAX_LEN/* * 3 / 4*/ / 10))
				p = idx + WORDS_FIELD_MAX_LEN;
			//else
			//	p++;

			arr.push(text.substr(idx, p - idx));
			idx = p;
		}
		else {
			if (idx < text.length)
				arr.push(text.substr(idx));
			return arr;
		}
	}
}
function getselectionid() {
	var result = (location.pathname + "/").match(/\/(s[0-9]+)\//);
	if (result != null)
		return result[1];
}
function getboardid() {
	//var result = location.pathname.match(/\/(b[0-9]+)$/);
	//if (result != null)
	//	return result[1];
	var result = (location.pathname + "/").match(/\/(b[0-9]+)\//);
	if (result != null)
		return result[1];
}
function getBoardName() {
	var text = decodeURIComponent(location.pathname);

	//var result = text.match(/\/([^\/]+板)$/);
	//if (result != null)
	//	return result[1];
	var result = (text + "/").match(/\/([^\/]+板)\//);
	if (result != null)
		return result[1];
}
function getdiscussionid() {
	//var result = location.pathname.match(/\/(d[0-9]+)$/);
	//if (result != null)
	//	return result[1];
	var result = (location.pathname + "/").match(/\/(d[0-9]+)\//);
	if (result != null)
		return result[1];
}
function isDiscussion() {
	return getdiscussionid() !== undefined;
}
function isLetterId(text) {
	if (typeof text !== "string")
		return false;

	var result = text.match(/^e[0-9]+$/);
	return result != null;
}
function makeLetterId(num) {
	return "e" + String(num);
}
function letterIdToSerialNumber(text) {
	return Number(text.substr(1));
}
function isDiscussionId(text) {
	var result = text.match(/^d[0-9]+$/);
	return result != null;
}
function isBoardId(text) {
	var result = text.match(/^b[0-9]+$/);
	return result != null;
}
function isBoardList(text) {
	var boards = text.split(",");
	for (var i = 0; i < boards.length; i++) {
		if (!isBoardId(boards[i])) {
			return boards[i];
		}
	}
	return null;
}
function isUserNameList(text) {
	if (text.length > 0) {
		var names = text.split(",");
		for (var i = 0; i < names.length; i++) {
			if (names[i].length === 0)
				return "空字串";
			var err_ch = checkCharSetUserName(names[i], true);
			if (err_ch !== "")
				return names[i];
		}
	}
	return null;
}
function splitId(text) {
	return text.split(".");
}
function extractBoardId(href) {
	if (href[0] === '/') {
		var board_id = href.substr(1);
		if (isBoardId(board_id))
			return board_id;
	}
}
function makeBoardLink(board_id, board_name) {
	return "/" + board_id/* + "/" + encodeURIComponent(board_name)*/;
}
function makeSandLink(board_id, discussion_id, letter_id) {
	var result = "/" + board_id;

	if (discussion_id) {
		result += "/" + discussion_id;

		if (letter_id)
			result += "#" + letter_id;
	}
	return result;
}
function makeUserLink(user_id) {
	return "/u" + user_id;
}
function printObject(obj, name, priority_arr) {
	console.log(name + ".")

	if (priority_arr !== undefined) {
		var text = "\t";
		for (var i = 0; i < priority_arr.length; i++) {
			var prop = priority_arr[i];
			if (obj[prop] !== undefined)
				text += prop + "=" + obj[prop] + ". ";
		}
		if (text !== "\t")
			console.log(text);
	}
	var text = "\t\t";
	var not_printed = "";

	for (var prop in obj) {
		if (typeof (obj[prop]) === "function" ||
			typeof (obj[prop]) === "object" ||
			obj[prop] === undefined ||
			prop.match(/^[A-Z_]+$/))		// Skip constants.
			not_printed += prop + ". ";
		else if (!priority_arr || priority_arr.indexOf(prop) === -1) {
			text += prop + "=" + obj[prop] + ". ";

			if (text.length >= 120) {
				console.log(text);
				text = "\t\t";
			}
		}
	}
	if (text !== "\t\t")
		console.log(text);
	if (not_printed !== "")
		console.log("\tNot printed: " + not_printed);
}

function escapeHTML(str) {
	var ret = str.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
				.replace(/\"/g, '&quot;').replace(/\'/g, '&#x27;').replace(/\//g, '&#x2F;');
	// will become longer if replace do occurs.
	//if (ret.length > str.length)
	//	history.changed = true;
	return ret;
}
function unescapeHTML(str) {
	return str.replace(/&amp;/g, "&").replace(/&lt;/g, "<").replace(/&gt;/g, ">")
				.replace(/&quot;/g, "\"").replace(/&#x27;/g, "\'").replace(/&#x2F;/g, "/");
}
function findInfoElement(origin_elt, up_step, next_step, filter, down_step) {
	var elt = origin_elt;
	if (up_step === 1)
		elt = elt.parent();

	if (next_step === 1)
		elt = elt.next();
	else if (next_step === -1)
		elt = elt.prev();
	else if (next_step === -2)
		elt = elt.prev().prev();

	if (down_step === 1)
		elt = elt.children();

	return elt.filter(filter);
}
function showCharSetError(input_elt, err_ch) {
	findInfoElement(input_elt, 1, 1, ".error_info").show().children("span").text("無法包含特殊字元\"" + err_ch + "\"");
}
function checkPlainInput(e) {
	var input_elt = (e instanceof jQuery.Event) ? $(e.target) : e;

	var min_len = input_elt.attr("data-min-len") || 1;
	var max_len = input_elt.attr("maxlength") || 100;
	var text = input_elt.val();

	var err_info = findInfoElement(input_elt, 1, 1, ".error_info");
	var err_msg = err_info.children("span");

	err_info.hide();

	if (text.length < min_len) {
		err_msg.text("長度過短");
		err_info.show();
		return false;
	}
	else if (text.length > max_len) {
		err_msg.text("長度過長");
		err_info.show();
		return false;
	}
	var validate_type = input_elt.attr("data-validate");		// undefined for those do not need validation.
	if (validate_type) {
		var err_ch = checkCharSetUserName(text, true, validate_type);
		if (err_ch !== "") {
			showCharSetError(input_elt, err_ch);
			return false;
		}
	}
	return true;
}
function onKeyPressUserName(event) {
	var input_elt = $(event.target);
	if (event.which < 32 || event.charCode == 0 || event.ctrlKey || event.altKey)
		return;

	var text = String.fromCharCode(event.which);

	var err_ch = checkCharSetUserName(text, false, input_elt.attr("data-validate"));
	if (err_ch !== "") {
		showCharSetError(input_elt, err_ch);
		return false;
	}
}
function onInputUserName(event) {
	var input_elt = $(event.target);

	findInfoElement(input_elt, 1, 1, ".error_info").hide();

	var text = input_elt.val();
	var err_ch = checkCharSetUserName(text, false, input_elt.attr("data-validate"));
	if (err_ch !== "") {
		showCharSetError(input_elt, err_ch);
	}
}
function checkCharSetUserName(text, check_end, validate_type) {
	for (var i = 0; i < text.length; i++) {

		var ch = text.charAt(i);
		var code = text.charCodeAt(i);

		if (validate_type === "number") {
			if (!withinCharSetNumber(ch, code))
				return ch;
		}
		else {
			if (!withinCharSetUserName(ch, code, check_end && (i == 0 || i == text.length - 1)))
				return ch;
		}
	}
	return "";
}
function withinCharSetUserName(ch, code, is_end) {
	if (code >= 0x80) return true;

	if (code >= 0x61 && code <= 0x7A) return true;		// a..z
	if (code >= 0x41 && code <= 0x5A) return true;		// A..Z
	if (code >= 0x30 && code <= 0x39) return true;		// 0..9

	if (is_end) return false;

	// if (code <= 0x1F) return false;

	if (" .-_".indexOf(ch) !== -1) return true;

	// if ("\"\'\\*+?|^$#&<>/%~`;:@=[]{}().!".indexOf(ch) !== -1)		// Allow only " ,-_".
	// if ("\"\'\\*+?|^$#&<>/%~`;:@=[]{}()!,".indexOf(ch) !== -1)		// Allow only " .-_".
	//	return false;

	return false;
}
function withinCharSetNumber(ch, code) {
	if (code >= 0x30 && code <= 0x39) return true;		// 0..9
	return false;
}
////////////////////////////////////////////////////////////////////////////////
function askCaptchaAndPostGoods(url, post_data, callback, wait_seconds) {
	showCaptchaDialog(function (is_ok) {
		if (is_ok) {
			post_data.recaptcha_challenge_field = Recaptcha.get_challenge(),
			post_data.recaptcha_response_field = Recaptcha.get_response(),

			postGoods(url, post_data, callback);
		}
		else
			callback(false, "請輸入驗證碼" + (wait_seconds ? "或稍待" + wait_seconds + "秒" : "") + "後重試。");
	},
	{
		wait_seconds: wait_seconds,
	});
}
function postGoods(url, post_data, callback) {
	var data_to_send;
	var process_data = true;
	var content_type;

	if (post_data.upload_files) {
		data_to_send = post_data.upload_files;
		process_data = false;
		content_type = false;

		for (var name in post_data) {
			if (name === "upload_files" || !post_data.hasOwnProperty(name)) continue;
			var value = post_data[name];
			if (typeof value === "function" || typeof value === "undefined") continue;
			data_to_send.append(name, value);		// This seems changing \n to \r\n in words (on Chrome but not on IE).
		}
	}
	else
		data_to_send = post_data;

	//$.post(url, data_to_send)
	$.ajax({
		type: "POST",
		url: url,
		data: data_to_send,
		processData: process_data,
		contentType: content_type
	})
	.done(function (data, textStatus, jqXHR) {
		//console.log("data=" + data + ". textStatus=" + textStatus
		//				+ ". jqXHR.readyState=" + jqXHR.readyState + ". jqXHR.status=" + jqXHR.status
		//				+ ". jqXHR.statusText=" + jqXHR.statusText
		//				+ ". jqXHR.responseText=" + jqXHR.responseText
		//				+ ".");
		if (data.err_msg)
			callback(false, data.err_msg);
		else if (data.captcha_or_wait_seconds) {
			askCaptchaAndPostGoods(url, post_data, callback, data.captcha_or_wait_seconds);
		}
		else
			callback(true, data);
	})
	.fail(function (jqXHR, textStatus, errorThrown) {
		console.log("errorThrown=" + errorThrown + ". textStatus=" + textStatus
                        + ". jqXHR.readyState=" + jqXHR.readyState + ". jqXHR.status=" + jqXHR.status
                        + ". jqXHR.statusText=" + jqXHR.statusText + ". jqXHR.responseText=" + jqXHR.responseText + ".");
		// For net::ERR_CONNECTION_RESET type error, it will be:
		// errorThrown=. textStatus=error. jqXHR.readyState=0. jqXHR.status=0. jqXHR.statusText=error. jqXHR.responseText=.
		var rep;
		var m = jqXHR.responseText.match(/<title>(.+)<\/ ?title>/i);
		if (!m)
			rep = jqXHR.responseText.substr(0, 250);
		else
			rep = m[1];
		callback(false, rep);
	});
}
function getGoods(url, callback) {
	$.get(url)
	.done(function (data, textStatus, jqXHR) {
		//console.log("data=" + data + ". textStatus=" + textStatus
		//				+ ". jqXHR.readyState=" + jqXHR.readyState + ". jqXHR.status=" + jqXHR.status
		//				+ ". jqXHR.statusText=" + jqXHR.statusText
		// + ". jqXHR.responseText=" + jqXHR.responseText		// too big to print every time.
		//+ ".");
		callback(data);
	})
	.fail(function (jqXHR, textStatus, errorThrown) {
		console.log("errorThrown=" + errorThrown + ". textStatus=" + textStatus
                        + ". jqXHR.readyState=" + jqXHR.readyState + ". jqXHR.status=" + jqXHR.status
                        + ". jqXHR.statusText=" + jqXHR.statusText + ". jqXHR.responseText=" + jqXHR.responseText + ".");
		// For net::ERR_CONNECTION_RESET type error, it will be:
		// errorThrown=. textStatus=error. jqXHR.readyState=0. jqXHR.status=0. jqXHR.statusText=error. jqXHR.responseText=.
		callback(null);
	});
}
////////////////////////////////////////////////////////////////////////////////
function wheelHandler(event) {
	if (event.target.tagName === "TEXTAREA" || event.target.tagName === "INPUT")
		return;

	var e = event || window.event;
	/*
    var deltaX = e.deltaX * -30 ||
                        e.wheelDeltaX / 4 ||
                        0;

    var deltaY = e.deltaY * -30 ||
                        e.wheelDeltaY / 4 ||
                        (e.wheelDeltaY === undefined && e.wheelDelta / 4) ||
                        e.detail * -10 ||
                        0;
    */
	if (!e.altKey && /*!e.shiftKey && */!e.ctrlKey && !e.metaKey) {

		// wheelDelta for Chrome and IE. deltaY for FireFox.
		var final_delta = e.wheelDelta || e.deltaY * -30;

		if (isViewSky())
			wheelStickerPane(final_delta, e.shiftKey);
		else if (e.shiftKey)
			window.scrollBy(0, -final_delta);
		else
			window.scrollBy(final_delta, 0);

		//console.log("final_delta=" + final_delta + ", wheelDelta=" + e.wheelDelta
		//			+ ", detail=" + e.detail + ", deltaY=" + e.deltaY + ", deltaX=" + e.deltaX + ".");

		if (e.preventDefault) e.preventDefault();
		if (e.stopPropagation) e.stopPropagation();
		e.cancelBubble = true;
		e.returnValue = false;
		return false;
	}
}
function registerShortcutKey() {
	if (document.addEventListener) {
		document.addEventListener("keydown", keyhandler, false);
		document.addEventListener("keyup", keyhandler, false);
	}
	else if (document.attachEvent) {
		document.attachEvent("onkeydown", keyhandler);
		document.attachEvent("onkeyup", keyhandler);
	}
	var last_key = null;

	function keyhandler(event) {
		if (event.altKey || event.ctrlKey/* || event.shiftKey*/ || event.metaKey)
			return;
		if (event.target.tagName === "TEXTAREA" || event.target.tagName === "INPUT")
			return;
		//console.log("repeat: " + event.repeat);		// not work in IE.

		var keyname = null;

		if (event.key) keyname = event.key;
		else if (event.keyIdentifier && event.keyIdentifier.substring(0, 2) !== "U+")
			keyname = event.keyIdentifier;
		else
			keyname = keyCodeToKeyName[event.keyCode];

		if (!keyname) return;

		var stop_propagation = false;

		if (event.type === "keydown") {
			var repeat = last_key === keyname;
			last_key = keyname;

			if (typeof onKeyDown === "function") {
				if (onKeyDown(keyname, repeat))
					stop_propagation = true;
			}
		}
		if (event.type === "keyup") {
			last_key = null;
			if (typeof onKeyUp === "function") {
				if (onKeyUp(keyname))
					stop_propagation = true;
			}
		}
		//
		if (stop_propagation) {
			if (event.stopPropagation) event.stopPropagation();
			else event.cancelBubble = true;
			if (event.preventDefault) event.preventDefault();
			else event.returnValue = false;

			return false;
		}
	}
}
var keyCodeToKeyName = {
	32: "Spacebar",
	33: "PageUp", 34: "PageDown", 35: "End", 36: "Home",
	37: "Left", 38: "Up", 39: "Right", 40: "Down",

	65: "A", 66: "B", 67: "C", 68: "D", 69: "E", 70: "F", 71: "G", 72: "H", 73: "I",
	74: "J", 75: "K", 76: "L", 77: "M", 78: "N", 79: "O", 80: "P", 81: "Q", 82: "R",
	83: "S", 84: "T", 85: "U", 86: "V", 87: "W", 88: "X", 89: "Y", 90: "Z",

	107: "Add", 109: "Subtract",
	61: "=", 173: "-", 187: "=", 189: "-",
};

var g_document_width = 0;

function fitStoreyWidth(anchor_at_right) {
	//console.log("fitStoreyWidth(" + anchor_at_right + "). g_pending_fsw=" + g_pending_fsw + ".");
	var storey_table_width = fitStoreyWidthInternal();

	var w = $(window);
	var new_width = Math.max(w.width(), storey_table_width);

	if (anchor_at_right) {
		window.scrollBy(new_width - g_document_width, 0);
		//console.log("window.scrollBy().");
	}
	g_document_width = new_width;

	setTimeout(onDimensionChanged, 0);
	g_pending_fsw = false;
}
function fitStoreyWidthElement(elt) {
	elt.find(".tbrl").parent().css("min-width", function () {
		var width = $(this).children().outerWidth();		// will be a (wrong) small value for hidden elements.
		//console.log("width=" + width + ". clientWidth=" + this.clientWidth + ". offsetWidth=" + this.offsetWidth + ". scrollWidth=" + this.scrollWidth + ".");
		return width;
	})
	.css("max-width", function () {
		var width = $(this).children().outerWidth();
		return width;
	});		// for Chrome.
	elt.find(".hori_tb").parent().css("min-width", function () {
		//console.log("hori_tb's parent clientWidth=" + this.clientWidth + ", offsetWidth=" + this.offsetWidth
		//			+ ", scrollWidth=" + this.scrollWidth + ".");

		var outer_elt = $(this).children(".hori_tb").append('<div></div>');
		var inner_elt = outer_elt.children('div:last-of-type');

		var inner = inner_elt[0].getBoundingClientRect();
		var outer = outer_elt[0].getBoundingClientRect();
		var diff = inner.right - outer.left;
		var padding = (outer.width - inner.width) / 2;

		inner_elt.remove();

		//console.log("hori_tb's outer.left=" + outer.left.toFixed(0) + ", inner.right=" + inner.right.toFixed(0) +
		//	", diff=" + diff.toFixed(0) + ", padding=" + padding.toFixed(0) + ".");

		//if (diffLargerThan(this.scrollWidth, diff + padding, 1))
		//	console.log("hori_tb width mismatches. (" + this.scrollWidth + "). (" + (diff + padding) + ")!");

		return diff + padding;
	})
	.css("max-width", "");		// after changing from .tbrl to .hori_tb, max-width must be cleared.
}
function fitStoreyWidthInternal() {
	fitStoreyWidthElement($(document));

	var largest_width = 0;

	// div.storey is position absolute so that width will automatically size.
	$("td.storey").css("min-width", function () {		// will not enter for view=sky.
		var margin = 0/*32*//*8*/;		// margin for long discussion.
		var sum = 0;
		var info = "";
		$(this).children().each(function () {
			var width = $(this).outerWidth(true);

			if ($(this).css("display") === "none")
				width = 0;

			if (info.length !== 0)
				info += ", ";
			info += width;

			sum += width;
			margin += Math.max(0.75, width * 0.002);
		});
		margin = Math.max(40, margin);
		sum += margin;
		// console.log("td.storey's children width are [" + info + "].");

		if (sum > largest_width)
			largest_width = sum;

		return sum;
	});
	$("body").css("min-width", function () {
		//var width = $(this).children().outerWidth();
		//return width;
		return largest_width;		// will be 0 for view=sky.
	});		// for white border at right side of large page.
	return largest_width;		// will be 0 for view=sky.
}
var g_user_image_max_height;
var g_base_height;
function adjustElementHeight() {
	if (isViewSquare())
		return;

	var win_h = $(window).height();
	var other_h = 80;
	var base_h = win_h / 2 - other_h;
	var one_em = 16;

	if (base_h < 20 * one_em)
		base_h = 20 * one_em;

	g_user_image_max_height = base_h - 0.2 * one_em;
	g_base_height = base_h;

	$("td.room_body > section, td.room_body form, td.room_body > nav, td.room_side > figure, td.room_head > footer").css("height", base_h).css("max-height", base_h);
	$("td.room_banner").css("height", base_h + 1.5 * one_em);
	$("td.room_banner header").css("max-height", base_h - 5 * one_em);
	$("img.user_obj").css("max-height", g_user_image_max_height);
	$("a.passage").css("max-height", g_user_image_max_height - 1.5 * one_em);
	// * 16 to translate from em to px.
}
function restoreElementTbrl(elt) {
	nodeToTateChuYoko(elt[0]);
	elt.addClass("tbrl");
	if (g_base_height)
		elt.css("height", g_base_height).css("max-height", g_base_height);
}
function removeElementTbrl(elt) {
	deNodeToTateChuYoko(elt[0]);
	elt.removeClass("tbrl").css("height", "").css("max-height", "");
	elt.parent().css("min-width", "").css("max-width", "");
}
function adjustSkyHeight() {
	//console.log("document: (" + $(document).width() + ", " + $(document).height() + ").");
	//console.log("window: (" + $(window).width() + ", " + $(window).height() + ").");

	var window_height = $(window).height();
	var table_height = $("body > table").height();
	var sky_height = $("#storey_sky > td").height();
	var crust_height = $("#storey_crust > td").height();

	var h = window_height - (table_height - sky_height - crust_height);

	//console.log("table_height=" + table_height + ", sky height=" + sky_height
	//			+ ", crust height=" + crust_height + ", h=" + h + ".");

	h = h / 2 - 17/*10*//*margin*/;
	//h -= 20;
	if (h < 0) h = 0;
	$("#storey_sky > td").height(h);
	$("#storey_crust > td").height(/*0*/h);
}

function collapseMenu() {
	$(".hearts_menu").menu("collapseAll")
						.offset({ left: 0, top: 0 })
						.css("visibility", "collapse");
	// move to (0, 0) to prevent scroll bar from emerging.
}
function showRoomMenu(event) {
	collapseMenu();

	g_room_menu_target = event.target;

	//if (typeof updateRoomMenu === "function")
	if (!updateRoomMenu())
		return;

	$("#room_menu").position({
		of: event/*.target*/,
		my: "right top",
		at: "right bottom+8",
		collision: "flipfit flipfit"
	});

	$("#room_menu").css("visibility", "visible");
	return false;
}
function showGlobalMenu(event) {
	collapseMenu();

	$("#global_menu").position({
		of: event/*.target*/,
		my: "right top",
		at: "right bottom+8",
		collision: "flipfit flipfit"
	});

	$("#global_menu").css("visibility", "visible");
	return false;
}
////////////////////////////////////////////////////////////////////////////////

function onReady(layout_horizontal) {
	establishSession();

	var $document = $(document);

	//if (isChrome())
	//	$("body").addClass("is_chrome");

	//$("td > form").parent().addClass("wizard");		// restricting form width.
	$("table.room td > form").parent().addClass("f_wizard")		// restricting form width.
										.closest("table.room").addClass("t_wizard");

	$("#captcha_dialog").removeClass("not_show");
	$("#room_menu").removeClass("not_show");
	$("#global_menu").removeClass("not_show");

	$("#captcha_dialog").hide();

	if (isUserLoggedIn()) {
		$("#registerLink").parent("li").hide();
		$("#loginLink").parent("li").hide();

		var user_name = getUserName();

		$("#manageLink").text("管理我的帳戶" + user_name);
		$("#logoutLink").text("登出" + user_name);
	}
	else {
		$("#manageLink").parent("li").hide();
		$("#logoutLink").parent("li").hide();
	}

	$("#room_menu").menu();
	$("#global_menu").menu();

	$(".ui-menu").width(function () {
		var menuwidth = $(this).outerWidth();
		return menuwidth;
	});

	collapseMenu();
	//
	$document.on("click", "td.room_base, div.sticker > strong, td.room_banner > a:not([href]), td.room_head strong", showRoomMenu);
	//$document.on("click", "td.room_base", showRoomMenu);
	//$document.on("click", "div.sticker > strong", showRoomMenu);
	//$document.on("click", "td.room_banner > a:not([href])", showRoomMenu);

	$document.on("click", "#storey_ground, .glbl_menu", showGlobalMenu);
	$document.on("click", "[data-href]", function (event) {
		var $target = $(event.target);

		//var last_mousedown_xy = $target.data("last_mousedown_xy");

		//if (g_last_mousedown_xy)
		//if (g_last_mousedown_xy.x !== event.clientX || g_last_mousedown_xy.y !== event.clientY)
		if (fartherThan(g_last_mousedown_xy.x, event.clientX, g_last_mousedown_xy.y, event.clientY, MOUSE_MOVE_THRESHOLD))
			return;

		var href = $target.attr("data-href");
		// when an anchor in the section is clicked, event.targt is "a" and event.currentTarget is "section".
		// the "a" does not have data-href attribute.
		if (href) {
			//location = href;

			var window_name = hrefToWindowName(href);
			window.open(href, window_name);
			return false;
		}
	});
	$document.on("mousedown"/*, "[data-href]"*/, function (event) {
		//$target = $(event.target);
		//$target.data("last_mousedown_xy", { x: event.clientX, y: event.clientY });
		// on map view, click event happens even after drag and link follows. use mouse down event to identify occurrence of dragging.

		g_last_mousedown_xy.x = event.clientX;
		g_last_mousedown_xy.y = event.clientY;
	});

	$document.click(function (event) {
		//console.log("document click");
		var $target = $(event.target);

		if (!$target.closest(".ui-menu-item").has(".ui-menu").length) {
			setTimeout(function () {
				collapseMenu();
			}, 300/*should be same as menu delay*/);
		}
		if (event.target.id === "help_btn")
			showHelpDialog(event);
		else
			hideHelpDialog(event);

		releaseContextDrag();
	});

	//
	if (window.addEventListener) {
		window.addEventListener("resize", resizeHandler, false);
	}
	else if (window.attachEvent) {
		window.attachEvent("onresize", resizeHandler);
	}
	//window.addEventListener("scroll", scrollHandler, false);

	//if (layout_horizontal) {
	document.onwheel = wheelHandler;
	document.onmousewheel = wheelHandler;
	//}
	registerShortcutKey();

	//console.log("Cookie: " + document.cookie);
}
function checkNavigator() {
	var ret = true;

	//if (typeof console !== "undefined")
	//	console.log("User agent: " + navigator.userAgent);
	// IE11: Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; SLCC2; .NET CLR 2.0.50727; .NET CLR 3.5.30729; .NET CLR 3.0.30729; Media Center PC 6.0; HPDTDF; .NET4.0C; BRI/2; .NET4.0E; rv:11.0) like Gecko
	// Chrome: Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/31.0.1650.63 Safari/537.36
	// Firefox: User agent:Mozilla/5.0 (Windows NT 6.1; WOW64; rv:24.0) Gecko/20100101 Firefox/24.0

	var result = navigator.userAgent.match(/\bMSIE (\d+)\./);
	if (result != null) {
		var ver = Number(result[1]);
		ret = ver >= 9;
	}
	else {
		var can_use = typeof CSS;		// Only IE does not support CSS.supports().

		if (can_use !== "undefined") {
			var s1 = CSS.supports("writing-mode", "tb-rl");
			var s2 = CSS.supports("-webkit-writing-mode", "vertical-rl");
			var s3 = CSS.supports("-moz-writing-mode", "tb-rl");

			//console.log("CSS support of writing-mode tb-rl = " + s1);
			//console.log("CSS support of -webkit-writing-mode vertical-rl = " + s2);
			//console.log("CSS support of -moz-writing-mode tb-rl = " + s3);

			ret = s1 || s2 || s3;
		}
	}
	if (!ret) {
		g_no_tbrl = true;

		if (Math.random() < 0.2)
			alert("本網站使用直排文字。您目前所用的瀏覽器似乎並不支援直排文字。\r\n建議改用Chrome或Internet Explorer 10以上瀏覽本網站。造成不便敬請見諒。");

		ret = true;		// work around for Firefox.
	}
	if (!ret) {
		// alert("本網站使用直排文字。您目前所用的瀏覽器似乎並不支援直排文字。\r\n建議改用Chrome或Internet Explorer 10以上瀏覽本網站。造成不便敬請見諒。");
		$(".preload_not_show").removeClass("preload_not_show");
	}
	return ret;
}
function initialScroll() {
	if (!location.hash)
		window.scrollTo(document.documentElement.scrollWidth, document.documentElement.scrollHeight);
	else if (location.hash === "#last")
		window.scrollTo(0, document.documentElement.scrollHeight);
	else
		location.assign(location.hash);		// make closure compiler happy.
}
var g_pending_fsw = false;
function onImageReady(anchor_at_right, elt) {
	if (typeof onImageReadySpecific === "function") {
		if (onImageReadySpecific(elt) === false)
			return;
	}

	//console.log("onImageReady(" + anchor_at_right + "). g_pending_fsw=" + g_pending_fsw + ".");
	if (!g_pending_fsw) {
		g_pending_fsw = true;

		if (isViewSky())
			setTimeout(resizeHandler, 0);
		else
			setTimeout(fitStoreyWidth, 0, anchor_at_right);
	}
	//fitStoreyWidth(true);
}
function doWrap() {
	wrapRoomTable("room_table",
								"<table class='room'>\
									<tr>\
										<td class='room_body'>\
										</td>\
									</tr>\
									<tr>\
										<td class='room_base'></td>\
									</tr>\
								</table>");
	wrapRoomTable("room_table_interval",
								"<table class='room'>\
									<tr>\
										<td class='room_body'>\
										</td>\
									</tr>\
								</table>");
}
function wrapRoomTable(type, html) {
	$("[data-wrap='" + type + "']").each(function () {
		var elt = $(html);
		var content = $(this);
		content.replaceWith(elt);

		var body = elt.find("td.room_body");
		body.append(content);

		var id = content.attr("data-wrap-id");
		if (id)
			elt.attr("id", id);

		var addi_cls = content.attr("data-addi-cls");
		if (addi_cls)
			elt.addClass(addi_cls);

		//if (this.tagName === "FORM")
		//	body.addClass("wizard");
	});
	$("[data-wrap='" + type + "']").removeAttr("data-wrap");
}
function embedYoutube(elt, code) {
	var $elt = $(elt);
	var $link = $elt.next();

	var html = "<iframe class='user_obj' width='560' height='315' src='//www.youtube.com/embed/" +
				code +
				"' frameborder='0' allowfullscreen></iframe>";
	var embed = $(html);
	$link.after(embed);
	$elt.remove();
	fitStoreyWidth(true);
}
function fileSizeHint(file) {
	if (file.size >= 1000 * 1000)
		return (file.size / 1024 / 1024).toPrecision(3) + " MB";
	else if (file.size >= 1000)
		return (file.size / 1024).toPrecision(3) + " KB";
	else
		return file.size + " Bytes";
}
function makeImage(src_url) {
	/*if (isViewMap()) {
		var text = "<img src='" + src_url + "' class='user_obj' onload='onImageReady();' onerror='onImageReady();' onclick='collapseImage(event, this);'/>";
		//var text = "<a data-href='" + src_url + "' class='in_words_link'>" + src_url + "</a>"
		//			+ "<img src='" + src_url + "' class='user_obj' onload='onImageReady();' onerror='onImageReady();'/>";
	}
	else {
		var text = "<a href='" + src_url + "' target='_blank'>"
					+ "<img src='" + src_url + "' class='user_obj' onload='onImageReady();' onerror='onImageReady();'/></a>";
	}*/
	var link_url = g_image_thumbnail_map[unescapeHTML(src_url)];
	if (!link_url)
		link_url = src_url;

	var text = "<img data-href='" + link_url + "' title='" + link_url + "' src='" + src_url +
				"' class='user_obj' onload='onImageReady(true, this);' onerror='onImageReady(true, this);' draggable='false'/>";
	return text;
}
function embedImage(elt) {
	if (elt.id.substr(0, 4) === "iex_") {
		var idx = Number(elt.id.substr(4));

		for (var i = 0; i < 20; i++) {
			var e = document.getElementById("iex_" + (idx + i));

			if (e != null) {
				var $e = $(e);
				var $link = $e.next();
				var src_url = $link.attr("href");

				var html = makeImage(src_url);

				var embed = $(html);
				if (g_user_image_max_height)
					embed.children("img").css("max-height", g_user_image_max_height);

				$link.after(embed);

				$e.remove();
				$link.remove();
			}
		}
		fitStoreyWidth(true);
	}
}
function expandImage(event, elt) {
	//if (g_last_mousedown_xy.x !== event.clientX || g_last_mousedown_xy.y !== event.clientY)
	if (fartherThan(g_last_mousedown_xy.x, event.clientX, g_last_mousedown_xy.y, event.clientY, MOUSE_MOVE_THRESHOLD))
		return;

	var $elt = $(elt);
	var $link = $elt.next();
	var src_url = $link.attr("data-href");

	var html = makeImage(src_url);

	$link.remove();
	$elt.replaceWith(html);
}
function collapseImage(event, elt) {
	//if (g_last_mousedown_xy.x !== event.clientX || g_last_mousedown_xy.y !== event.clientY)
	if (fartherThan(g_last_mousedown_xy.x, event.clientX, g_last_mousedown_xy.y, event.clientY, MOUSE_MOVE_THRESHOLD))
		return;

	var $elt = $(elt);
	var src_url = $elt.attr("src");

	var html = "<a class='passage in_words_btn' onclick='expandImage(event, this);'>展開圖片</a> " +
				"<a data-href='" + src_url + "' class='in_words_link'>" + src_url + "</a>";

	$elt.replaceWith(html);
}
var g_image_embed_idx = 0;
function insertImage(src_url) {
	var text;
	if (g_image_cnt < g_image_limit) {
		text = makeImage(src_url);
		g_image_cnt++;
	}
	else {
		text = "<a class='passage in_words_btn' id='iex_" + g_image_embed_idx + "' onclick='embedImage(this);'>展開圖片</a> " +
				"<a href='" + src_url + "' class='in_words_link' target='_blank'>" + src_url + "</a>";

		g_image_embed_idx++;
	}
	return text;
}
function insertYoutube(match_text, sub1) {
	return "<a class='passage in_words_btn' onclick='embedYoutube(this, \"" + sub1 + "\");'>展開影片</a> " +
		"<a data-href='" + match_text + "' class='in_words_link'>" + match_text + "</a>";
}
function embedElements(text) {
	text = escapeHTML(text);

	// Warning: don't allow ' or ", otherwise the attribute ending will be escaped.
	text = text.replace(/^http\:&#x2F;&#x2F;i\.imgur\.com&#x2F;[\w]+\.(jpg|gif|png|jpeg)$/igm, insertImage);

	// http://hl1.blob.core.windows.net/images1/20140227/YbW5dwXD.jpg
	text = text.replace(/^http\:&#x2F;&#x2F;i\.hela\.cc&#x2F;images1&#x2F;[\d]+&#x2F;([\w]+&#x2F;)?[\w]+\.(jpg|gif|png|jpeg)$/igm, insertImage);

	text = text.replace(/^https?\:&#x2F;&#x2F;www\.youtube\.com&#x2F;watch\?v\=([\w\-]+)(&[^'"<>\s]*)?$/igm, insertYoutube);
	text = text.replace(/^http\:&#x2F;&#x2F;youtu\.be&#x2F;([\w\-]+)$/igm, insertYoutube);

	// Chrome: blob:http%3A//localhost%3A8353/4dc7b8b3-39f4-498d-a97e-cd25d64879f4
	// IE: blob:88C25EE3-83CB-4116-9080-6205E2DEAF07
	text = text.replace(/^blob\:[^'"<>\s]+$/igm, function (match_text) {
		var hint = "";
		var info = blob_store[unescapeHTML(match_text)];		// unescapeHTML for Chrome-type blob url.
		var blob_url = match_text;

		if (info) {
			hint = fileSizeHint(info.org_blob);

			if (info.shrunk_blob) {
				hint = "(原始大小: " + hint + ", 上傳大小: " + fileSizeHint(info.shrunk_blob) + ")";

				blob_url = info.shrunk_blob_url;
			}
			else
				hint = "(" + hint + ")";
		}
		return "<img src='" + blob_url
				+ "' class='user_obj' onload='onImageReady(false, this);' onerror='onImageReady(false, this);' draggable='false'/>" +
				"<p class='in_words_link'>" + hint + "</p>";
	});

	//text = text.replace(/^(ht|f)tp(s?)\:&#x2F;&#x2F;[0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*(\:[0-9]+)?(&#x2F;)?[a-zA-Z0-9\-\.\?\,\+&%\$#_;\=~]*$/igm,
	text = text.replace(/^(ht|f)tp(s?)\:&#x2F;&#x2F;[0-9a-zA-Z][^'"<>\s]+$/igm,
		"<a href='$&' class='in_words_link' target='_blank'>$&</a>");

	text = text.replace(/^&gt;&gt;([^'"<>\s]+)$/igm,
		"<a class='passage reply_to_btn'>$&</a>");

	return text;
}
function getRoomParams(elt) {
	return elt.data("params");
}
function saveParams(elt, params) {
	var params_data = elt.data("params");
	if (!params_data)
		elt.data("params", params);
	else {
		for (var prop in params) {
			if (params.hasOwnProperty(prop))
				params_data[prop] = params[prop];
		}
	}
}
function deNumberToTateChuYoko(text) {
	var code = text.charCodeAt(0);
	if (code == 9450)
		return "00";
	else if (code >= (0x245F + 1) && code <= (0x245F + 9))
		return "0" + (code - 0x245F).toString();
	else if (code >= (0x245F + 10) && code <= (0x245F + 99))
		return (code - 0x245F).toString();
	else
		return text;
}
function numberToTateChuYoko(text) {
	var num = Number(text);
	if (num >= 1 && num <= 99) {
		var code_num = 0x245F + num;
		//var code_text = "&#" + code_num + ";";
		var code_text = String.fromCharCode(code_num);
		return code_text;
	}
	else if (num === 0) {
		//return "&#9450;";
		var code_text = String.fromCharCode(9450);
		return code_text;
	}
	return text;
}
function alphaToFullWidth(text) {
	var code = text.charCodeAt(0);
	if ((code >= 97/*a*/ && code <= 122/*z*/) ||
		(code >= 65/*A*/ && code <= 90/*Z*/))
		return String.fromCharCode(0xFF21 + code - 65);
	return text;
}
function deTextToTateChuYoko(text) {
	text = text.replace(/[\u2460-\u24C2\u24EA]/g, function (match_text) {
		return deNumberToTateChuYoko(match_text);
	});
	return text;
}
function textToTateChuYoko(text, number_only) {
	//var orig_text = text;
	text = text.replace(/\d\d/g, function (match_text) {
		return numberToTateChuYoko(match_text);
	});
	//text = text.replace(/\d/g, function (match_text) {
	//	return numberToTateChuYoko(match_text);
	//});
	if (!number_only) {
		//text = text.replace(/[A-Za-z]/g, function (match_text) {
		//	return alphaToFullWidth(match_text);
		//});
	}
	//console.log("Changed \"" + orig_text + "\" to \"" + text + "\".");
	return text;
}
function deNodeToTateChuYoko(n) {
	if (n.nodeType == 3) {
		n.data = deTextToTateChuYoko(n.data);
	}
	else
		for (var i = 0; i < n.childNodes.length; i++)
			deNodeToTateChuYoko(n.childNodes[i]);
}
function nodeToTateChuYoko(n) {
	if (n.nodeType == 3) {
		if (!n.data.match(/^(ht|f)tp(s?)\:/i))
			n.data = textToTateChuYoko(n.data);
	}
	else
		for (var i = 0; i < n.childNodes.length; i++)
			nodeToTateChuYoko(n.childNodes[i]);
}
function applyEwrFont(elt, ewr_css_cls) {
	elt.removeClass("tbrl_ewr1 tbrl_ewr3 tbrl_ewr4");
	elt.addClass(ewr_css_cls);
}
function modifyRoomTableSide(elt, params) {
	var $tgt_elt = elt.find(/*"td.room_side > "*/"figure");
	var html = "";
	if (params.n_ili)
		html += "<img onclick='voteLetter(MT_ILI, event);' title='" + params.n_ili + "個人說讚。' src='/Images/light_white.png' draggable='false'/>"
				+ "<span title='" + params.n_ili + "個人說讚。'>" + textToTateChuYoko(params.n_ili.toString()) + "</span>";
	if (params.n_idli)
		html += "<img onclick='voteLetter(MT_IDLI, event);' title='" + params.n_idli + "個人說噓。' src='/Images/light_black.png' draggable='false'/>"
				+ "<span title='" + params.n_idli + "個人說噓。'>" + textToTateChuYoko(params.n_idli.toString()) + "</span>";

	$tgt_elt.html(html);
}
function justDummy(text) {
	return text;
}
function modifyRoomTable(elt, params) {
	// elt may have multiple elements. (ie. preview_room_elts)
	var $section = elt.find("section");

	if (params.css_cls != undefined) {
		$section.removeClass("tbrl hori_tb");
		$section.addClass(params.css_cls);

		elt.each(function (idx, b_elt) {
			var a_elt = $(b_elt);
			var a_section = a_elt.find("section");
			var a_figure = a_elt.find("figure");
			var a_footer = a_elt.find("footer");

			if (params.css_cls === "hori_tb") {
				removeElementTbrl(a_figure);
				removeElementTbrl(a_footer);

				a_section.append(a_figure);
				a_section.prepend(a_footer);
			}
			else {
				restoreElementTbrl(a_figure);
				restoreElementTbrl(a_footer);

				a_section.parent().prev().append(a_figure);
				a_section.parent().next().append(a_footer);
			}
		});
	}
	if (params.ewr_css_cls != undefined) {
		applyEwrFont($section, params.ewr_css_cls);
	}
	if (params.words != undefined) {
		var words = embedElements(params.words);

		$section.children("div").html(words);
	}
	var layout_horizontal = $section.hasClass("hori_tb");

	if (params.css_cls != undefined || params.words != undefined) {
		$section.children("div").each(function (idx, section_elt) {
			if (!layout_horizontal)
				nodeToTateChuYoko(section_elt);
			else
				deNodeToTateChuYoko(section_elt);
		});
	}
	if (params.link != undefined) {
		$section.children("div").attr("data-href", params.link).attr("title", params.link);
	}
	/////
	var tcy_f = layout_horizontal ? justDummy : textToTateChuYoko;

	if (params.serial_num != undefined)
		elt.find("strong").text(tcy_f(params.serial_num.toString()));

	if (params.creator != undefined) {		// test against undefined to cope with empty string.
		elt.find("footer > a:nth-of-type(1) > mark:nth-of-type(1)").text(params.creator.substr(0, 1));
		elt.find("footer > a:nth-of-type(1) > mark:nth-of-type(2)").text(params.creator.substr(1));
	}
	if (params.create_time != undefined) {
		elt.find("small:nth-of-type(1)").text(tcy_f(params.create_time.toLocaleDateString()));
		elt.find("small:nth-of-type(2)").text(tcy_f(params.create_time.toLocaleTimeString()));
	}
	if (params.editor != undefined) {
		elt.find("footer > a:nth-of-type(2) > mark:nth-of-type(1)").text(params.editor.substr(0, 1));
		elt.find("footer > a:nth-of-type(2) > mark:nth-of-type(2)").text(params.editor.substr(1));
	}
	if (params.edit_time != undefined) {
		elt.find("small:nth-of-type(3)").text(tcy_f(params.edit_time.toLocaleDateString()));
		elt.find("small:nth-of-type(4)").text(tcy_f(params.edit_time.toLocaleTimeString()));
	}
	if (params.creator_uid_link != undefined) {
		var a_elt = elt.find("footer > a:nth-of-type(1)").attr("href", params.creator_uid_link);
		if (isViewSky())
			a_elt.attr("draggable", false);
	}
	if (params.editor_uid_link != undefined) {
		var a_elt = elt.find("footer > a:nth-of-type(2)").attr("href", params.editor_uid_link);
		if (isViewSky())
			a_elt.attr("draggable", false);
	}
	//
	saveParams(elt, params);
}
function insertElement(elt, params, is_layout) {
	var item = params.insert_inside || params.insert_inside_rev || params.insert_before || params.insert_after;
	if (!item)
		return;		// happens when view=sky.
	var tgt_elt = item.jquery ? item : $("#" + item);

	if (params.insert_inside) {
		tgt_elt.append(elt);
	}
	else if (params.insert_inside_rev) {
		tgt_elt.prepend(elt);
	}
	else {
		if (g_reverse_insert && is_layout) {
			if (params.insert_before) tgt_elt.after(elt);
			else if (params.insert_after) tgt_elt.before(elt);
		}
		else {
			if (params.insert_before) tgt_elt.before(elt);
			else if (params.insert_after) tgt_elt.after(elt);
		}
	}
}
function makeRoomTable(params) {
	var html =
			"<table class='room " + (params.addi_cls ? params.addi_cls : "") + "' "
				+ (params.id ? ("id='" + params.id + "'") : "") + ">"
					+ "<tr>"
						+ "<td class='room_side'><figure class='tbrl'></figure></td>"
						+ "<td class='room_body'><section class='tbrl'><div>預覽內容將顯示在此處。</div></section></td>"
						+ "<td class='room_head'>"
							+ "<footer class='tbrl'>"
								+ "<strong>sn</strong><s>| </s>"
								+ "<a><mark>遊</mark><mark>民</mark></a><s>| </s>"
									+ "<small>date</small><s>| </s>"
									+ "<small>time</small><s>| </s>" +
								(params.show_edit ?
								"<a><mark>編</mark><mark>者</mark></a><s>| </s>"
									+ "<small>date</small><s>| </s>"
									+ "<small>time</small><s>| </s>" : "") +
							"</footer>"
						+ "</td>"
					+ "</tr>"
					+ "<tr>"
						+ "<td class='room_base' colspan='2'></td>"
						+ "<td></td>"
					+ "</tr>"
				+ "</table>";
	var elt = $(html);
	modifyRoomTable(elt, params);
	modifyRoomTableSide(elt, params);

	insertElement(elt, params, true);
	return elt;
}
function modifyRoomBanner(elt, params) {
	if (params.words != undefined)
		elt.find("a > header").text(textToTateChuYoko(params.words, true));
	if (params.link != undefined)
		writeAnchorHref(elt.find("a:has(header)"), params.link);
	//elt.find("a:has(header)").attr("href", params.link).attr("target", hrefToWindowName(params.link));
	if (params.ewr_css_cls != undefined) {
		var target = elt.find("a");
		applyEwrFont(target, params.ewr_css_cls);
	}

	saveParams(elt, params);
}
function makeRoomBanner(params) {
	var html =
		"<table class='room " + (params.addi_cls ? params.addi_cls : "") + "' "
			+ (params.id ? ("id='" + params.id + "'") : "") + ">\
				<tr>\
					<td class='room_banner" + (params.position === "bottom" ? " p_bottom" : "") + "'>\
						<a class='tbrl'><header>\
							留言預覽\
						</header></a>\
					</td>\
				</tr>\
			</table>";
	var elt = $(html);
	modifyRoomBanner(elt, params);

	insertElement(elt, params, true);
	return elt;
}
function makeDummyInsert(params) {
	var html = "<ins></ins>";
	var elt = $(html);
	insertElement(elt, params, true);
	return elt;
}
function makeNavigationBlock(params) {
	var html =
		"<nav class='tbrl' data-wrap='room_table'"
		+ (params.addi_cls ? " data-addi-cls='" + params.addi_cls + "'" : "")
		+ "></nav>";

	var elt = $(html);

	insertElement(elt, params, true);
	return elt;
}
function modifyOptionRadio(elts, val) {
	var cur_val = elts.filter(":checked").val();

	if (cur_val/*string*/ != val/*string*/) {
		var $label = elts.filter("[value='" + val + "']").next("label");

		$label.click();
	}
}
function modifyOptionCheckBox($checkbox, checked) {
	var cur_checked = $checkbox.prop("checked");

	if (cur_checked !== checked) {
		var $label = $checkbox.next("label");

		if ($label.length === 0)
			$label = $checkbox.prev("label");

		$label.click();
	}
}
function makeOptionRadio(params) {
	if (!params.name)
		params.name = randomAlphaNumericString(10);

	var html = "";

	for (var i = 0; i < params.options.length; i++) {
		var temp_id = randomAlphaNumericString(10);
		html += "<span class='nowrap'>";
		html += "<input class='opra' type='radio' id='" + temp_id + "' name='" + params.name + "' value='" + params.values[i] +
					"' " + (i === params.check_idx ? "checked" : "") + "/>";
		html += "<label class='opra' for='" + temp_id + "'>" + params.options[i] + "</label>";
		html += "</span> ";
	}
	var elt = $(html);

	insertElement(elt, params);

	return elt.children("input");
}
function makeOptionCheckBox(params) {
	if (!params.id)
		params.id = randomAlphaNumericString(10);

	var html = "<input type='checkbox' class='ui-helper-hidden-accessible' id='" + params.id + "' "
					+ (params.checked ? "checked" : "") + "/>\
				<label class='option" + (params.checked ? " onposition" : " invisible faraway")
					+ "' for='" + params.id
					+ "' data-text='" + params.text + "'>" + params.text + "</label>";

	var elt = $(html);

	insertElement(elt, params);
	//
	var $checkbox = elt.first()/*$("#" + params.id)*/;
	var $label = $checkbox.next("label");

	var token = 0;

	$label.on("mouseenter", function (event) {
		$label.removeClass("invisible");

		setTimeout(function (my_token) {
			if (token === my_token) {
				$label.removeClass("faraway onposition");
				$label.addClass("intermediate");
			}
		}, 0, ++token);
	}).on("mouseleave", function (event) {
		var checked = $checkbox.prop("checked");

		if (checked) {
			$label.removeClass("intermediate faraway");
			$label.addClass("onposition");
		}
		else {
			$label.removeClass("intermediate onposition");
			$label.addClass("faraway");
		}
		setTimeout(function (my_token) {
			if (token === my_token) {
				if (!checked)
					$label.addClass("invisible");
			}
		}, 500, ++token);
	}).on("click", function (event) {
		var checked = $checkbox.prop("checked");

		$label.removeClass("invisible");

		if (checked) {
			$label.removeClass("intermediate onposition");
			$label.addClass("faraway");
		}
		else {
			$label.removeClass("intermediate faraway");
			$label.addClass("onposition");
		}
		setTimeout(function (my_token) {
			if (token === my_token) {
				if (checked)
					$label.addClass("invisible");
			}
		}, 500, ++token);
	});
	return $checkbox;
}
function makeImageChooser(params) {
	if (!params.id)
		params.id = randomAlphaNumericString(10);

	var html = "<input type='file' class='ui-helper-hidden-accessible' id='" + params.id + "' accept='image/*' multiple />\
				<label class='passage click_btn' for='" + params.id + "'>附加圖片</label>";

	var elt = $(html);

	insertElement(elt, params);
	return elt.first();
}
function makeTextLabel(params) {
	var html = "<p>\
					<label class='wizard'>" + params.name + "</label><span></span>\
				</p>";
	var elt = $(html);

	insertElement(elt, params);
	return elt.children("span");
}
function makePlainInput(params) {
	if (!params.id)
		params.id = randomAlphaNumericString(10);

	var html =
		"<p>\
			<label class='wizard' for='" + params.id + "'>" + params.name + "：</label>\
			<input type='text' id='" + params.id + "' maxlength='" + params.max_len + "'" +
				(params.no_place_holder ? "" : " placeholder='請輸入" + params.name + "'") +
				(params.default_value ? " value='" + params.default_value + "'" : "") +
				" size='" + params.size + "' data-min-len='" + params.min_len + "'" +
				(params.validate ? " data-validate='" + params.validate + "'" : "") +
				(params.list ? " list='" + params.list + "'" : "") +
				"/>\
		</p>\
		<p class='error_info'>抱歉，" + params.name + "<span></span>。</p>";
	var elt = $(html);
	insertElement(elt, params);

	var input_elt = elt.children("input");

	if (params.validate/* === "user_name"*/) {
		input_elt.on("input", onInputUserName);
		input_elt.on("keypress", onKeyPressUserName);
	}
	input_elt.on("change", checkPlainInput);

	return input_elt;
}
function makePlainCheckBox(params) {
	if (!params.id)
		params.id = randomAlphaNumericString(10);

	var html =
		"<p>\
			<label class='wizard' for='" + params.id + "'>" + params.name + "：</label>\
			<input type='checkbox' id='" + params.id + "'/>\
		</p>";

	var elt = $(html);
	insertElement(elt, params);

	return elt.children("input");
}
function makeWordsTextArea(params) {
	if (!params.id)
		params.id = randomAlphaNumericString(10);

	var html =
		"<p><label class='wizard' for='" + params.id + "'>內容：</label>\
		<small class='small_info'></small>\
		<br/>\
		<textarea cols='" + params.cols + "' rows='" + params.rows + "'\
			id='" + params.id + "' maxlength='" + params.max_len + "'\
			placeholder='請輸入內容'></textarea></p>";

	var elt = $(html);
	insertElement(elt, params);

	return elt.children("textarea");
}
function insertLinkToList(params) {
	var window_name = hrefToWindowName(params.address);
	var html = "<a href='" + params.address + "' target='" + window_name + "'"
				+ (params.addi_cls ? " class='" + params.addi_cls + "'" : "") + ">"
				+ params.text + "<s>| </s></a>";		// put <b> inside <a> so that line breaks after <b>. otherwise beginning of a line may be <b>.

	insertElement(html, params);
}
////////////////////////////////////////////////////////////////////////////////

function updateTextAreaInfo(input_elt, seg_cnt) {
	var max_len = Number(input_elt.attr("maxlength"));
	var text = input_elt.val();

	var info_text = "共" + text.length + "字。";
	if (text.length === max_len)
		info_text += "達到上限。";

	if (seg_cnt > 1)
		info_text += "自動分為" + seg_cnt + "段。"

	var info_elt = findInfoElement(input_elt, 0, -2, ".small_info");
	info_elt.text(info_text);
}
function insertLetter2(letter, insert_before, deleted, insert_after) {
	var sn = letterIdToSerialNumber(letter.letter_id);

	var whole_words = /*letter.abstract + */letter.words;

	var layout_value = flagsGetNumber(letter.flags, MT_LAYOUT);
	var reply_to_sn = flagsGetNumber(letter.flags, MT_REPLY_TO);		// 0 for none.
	var n_ili = flagsGetNumber(letter.flags, MT_ILI);
	var n_idli = flagsGetNumber(letter.flags, MT_IDLI);

	var layout_horizontal = (layout_value === 2) || g_no_tbrl;
	var ewr_css_cls;
	if (!layout_horizontal)
		ewr_css_cls = layout_value === 0 ? "" : "tbrl_ewr" + layout_value;

	var insider_only = flagsCheck(letter.flags, MT_AUTHORIZATION, 2);
	var reported = flagsCheck(letter.flags, MT_REPORT, 1);

	if (letter.subtype === "h") {
		var params = {
			words: whole_words,
			insert_before: insert_before,
			insert_after: insert_after,

			id: letter.letter_id,
			link: letter.link,

			// following are needed only for editing.
			serial_num: sn,
			creator: letter.creator,
			create_time: new Date(letter.create_time),
			addi_cls: deleted ? "deleted not_show" : "",
			ewr_css_cls: ewr_css_cls,

			creator_uid_link: letter.creator_uid_link,
			editor_uid_link: letter.editor_uid_link,
		};
		if (reported)
			params.addi_cls += " reported";
		return makeRoomBanner(params);
	}
	else {
		var params = {
			serial_num: sn,
			creator: letter.creator,
			creator_uid_link: letter.creator_uid_link,
			create_time: new Date(letter.create_time),
			words: whole_words,
			insert_before: insert_before,
			insert_after: insert_after,

			css_cls: layout_horizontal ? "hori_tb" : "tbrl",
			id: letter.letter_id,
			link: letter.link,
			addi_cls: deleted ? "deleted not_show" : "",
			ewr_css_cls: ewr_css_cls,
			n_ili: n_ili,
			n_idli: n_idli,
		};
		if (insider_only) {
			params.orig_words = params.words;
			params.words = ">>不公開的留言";
			params.addi_cls += " insider_only";
		}
		if (letter.subtype === "s")
			params.addi_cls += " is_subject";
		else if (letter.subtype === "d")
			params.addi_cls += " delete_remark not_show2";
		if (reported)
			params.addi_cls += " reported";

		if (reply_to_sn)
			params.reply_to_sn = reply_to_sn;

		if (letter.edit_time.length > 0) {
			params.show_edit = true;
			params.editor = letter.editor;
			params.editor_uid_link = letter.editor_uid_link;
			params.edit_time = new Date(letter.edit_time);
		}
		return makeRoomTable(params);
	}
}
function preprocessWordsInData(data) {
	var node = $("<div>" + data + "</div>");

	return preprocessWordsInDataInternal(node);
}
function preprocessWordsInDataInternal(node) {
	node.find("a[href^='http://i.hela.cc']:has(img)").replaceWith(function (idx) {
		var img_child = this.firstChild;
		g_image_thumbnail_map[img_child.src] = this.href;
		return img_child.src;
	});

	node.find("img").replaceWith(function (idx) {
		return this.src;
	});
	return node.text();
}
function dataToLetter(data) {
	var letter = {};

	letter.letter_id = data.children("header").text();
	letter.subtype = data.children("aside").text();
	//letter.words = data.children("details").text();
	letter.flags = data.children("footer").text();
	letter.create_time = data.children("time").first().text();
	letter.edit_time = data.children("time").eq(1).text();

	var node = data.children("summary");
	
	letter.words/*abstract*/ = preprocessWordsInDataInternal(node);

	letter.link = node.children("a[href^='/']").attr("href");		// undefined if there is no <a> inside <summary>.

	node = data.children("address");

	letter.creator = node.first().text();
	letter.creator_uid_link = node.first().children("a").attr("href");		// undefined if the href attribute does not exist.

	letter.editor = node.eq(1).text();		// "" if not exist.
	letter.editor_uid_link = node.eq(1).children("a").attr("href");		// undefined if not exist.

	return letter;
}
var g_summary_inserted = 0;
function moreSummary(elt, data) {
	if (insertSummary2(data) === true)
		$(elt).text("沒有更多").removeAttr("onclick");
	resizeHandler();
}
function insertSummary2(data) {
	var cnt = 0;
	var articles = data.children("article");

	articles.each(function (idx, elt) {
		if (idx >= g_summary_inserted) {
			var article_data = $(elt);
			var discussion_deleted = false;

			article_data.children("section").each(function (idx2, elt2) {
				var section_data = $(elt2);
				var letter = dataToLetter(section_data);

				var deleted = isLetterDeleted(letter.flags);	// flagsCheck(letter.flags, DELETED_FLAG_CHAR, 1);

				if (letter.subtype === "h" && deleted)
					discussion_deleted = true;

				//letter.words = removeForeMeta(letter.words);

				insertLetter2(letter, "insert_summary_point", discussion_deleted || deleted);
			});
			cnt++;
			if (cnt == 10)
				return false;
		}
	});
	g_summary_inserted += cnt;
	if (g_summary_inserted === articles.length)
		return true;
}
function removeForeMeta(text) {
	return text.replace(/^>>\((.+?)\n/, "");
}
function insertDiscussionList(nav_data, insert_inside_rev, insert_deleted_inside, insert_reported_inside) {
	var has_deleted_item = false;

	nav_data.children("a").each(function (idx, a_elt) {
		var a_data = $(a_elt);
		var href = a_data.attr("href");
		var heading = a_data.text();

		var flags = a_data.next("footer").text();		// "" if footer does not exist.
		var deleted = isLetterDeleted(flags);	// flagsCheck(flags, DELETED_FLAG_CHAR, 1);
		var report_count = flagsGetNumber(flags, MT_REPORT);

		if (deleted) {
			has_deleted_item = true;

			if (insert_deleted_inside)
				insertLinkToList({
					address: href/*makeSandLink(board_id, discussion_id)*/,
					text: heading,
					insert_inside_rev: insert_deleted_inside,
					addi_cls: "deleted not_show",
				});
		}
		else {
			insertLinkToList({
				address: href/*makeSandLink(board_id, discussion_id)*/,
				text: heading,
				insert_inside_rev: insert_inside_rev,
			});
			if (report_count !== 0 && insert_reported_inside)
				insertLinkToList({
					address: href,
					text: heading + " (" + report_count + ")",
					insert_inside_rev: insert_reported_inside,
				});
		}
	});
	return has_deleted_item;
}
function urlArgs() {
	var args = {};
	var query = location.search.substring(1);
	var pairs = query.split("&");

	for (var i = 0; i < pairs.length; i++) {
		var pos = pairs[i].indexOf('=');
		if (pos == -1) continue;
		var name = pairs[i].substring(0, pos);
		var value = pairs[i].substring(pos + 1);
		value = decodeURIComponent(value);
		args[name] = value;
	}
	return args;
}
function isViewMap() {
	var args = urlArgs();
	return args["view"] == "map";
}
function isViewSquare() {
	var args = urlArgs();
	return args["view"] == "map" || args["view"] == "sky";
}
function isViewSky() {
	var args = urlArgs();
	return args["view"] == "sky";
}
function isViewScribble() {
	var args = urlArgs();
	return args["view"] == "scb";
}
function isMenuAllOn() {
	var args = urlArgs();
	return args["menu"] == "allon";
}
function getCookies() {
	var cookies = {};
	var all = document.cookie;
	if (all === "")
		return cookies;
	var list = all.split("; ");
	for (var i = 0; i < list.length; i++) {
		var cookie = list[i];
		var p = cookie.indexOf("=");
		var name = cookie.substring(0, p);
		var value = cookie.substring(p + 1);
		value = decodeURIComponent(value);
		cookies[name] = value;
	}
	return cookies;
}
function isLoggedIn() {
	var cookies = getCookies();
	return "hun" in cookies;
}
function isUserLoggedIn() {
	var cookies = getCookies();
	if ("hun" in cookies)
		return cookies.hun[0] != "_";
	return false;
}
function getUserName() {
	var cookies = getCookies();
	return cookies.hun;
}
function getUerNameOrDefault() {
	if (isUserLoggedIn()) {
		return getUserName();
	}
	return "船員";
}
function getUserId() {
	var cookies = getCookies();
	return Number(cookies.huid);		// if not logged in, cookies.huid is undefined. Number(cookies.huid) is NaN.
}
function randomPick(text) {
	var idx = Math.floor(Math.random() * text.length);
	return text[idx];
}
function randomAlphaNumericCharacter() {
	return randomPick("0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ");
}
function randomAlphaNumericString(len) {
	var text = "";
	for (var i = 0; i < len; i++)
		text += randomAlphaNumericCharacter();
	return text;
}
//document.cookie = "hmi=" + hmi + "; path=/";

function registerLau(lau_name, lau_pwd, m_id) {
	postGoods("/account/ajaxregister",
		{
			UserName: lau_name,
			Password: lau_pwd,
			ConfirmPassword: lau_pwd,
			m_id: m_id,
		},
		function (suc, response) {
			if (suc && response.ok) {
			}
			else {
				// don't remove if it is network problem.

				// localStorage.removeItem("lau_name");
				// localStorage.removeItem("lau_pwd");

				//alert("Lau register failed. " + response);
			}
		}
	);
}
function loginLau(lau_name, lau_pwd, m_id) {
	postGoods("/account/ajaxlogin",
		{
			UserName: lau_name,
			Password: lau_pwd,
			RememberMe: false,
			m_id: m_id,
		},
		function (suc, response) {
			if (suc && response.ok) {
			}
			else {
				//alert("Lau login failed. " + response);
				//20150924 stop using sql server //registerLau(lau_name, lau_pwd, m_id);
			}
		}
	);
}
function logout() {
	postGoods("/account/ajaxlogoff",
		{
		},
		function (suc, response) {
			if (suc && response.ok) {
				location.assign('/');
			}
			else {
				alert("登出失敗。" + response);
			}
		}
	);
}
function establishSession() {
	// if server replied with "please login", the cookie hun has been cleared before entering this function.

	if (!localStorage["m_id"])
		localStorage["m_id"] = randomAlphaNumericString(8);
	var m_id = localStorage["m_id"];

	var need_register = false;

	if (!localStorage["lau_name"]) {
		localStorage["lau_name"] = "_" + randomAlphaNumericString(8);
		need_register = true;
	}
	var lau_name = localStorage["lau_name"];

	if (!localStorage["lau_pwd"])
		localStorage["lau_pwd"] = randomAlphaNumericString(8);
	var lau_pwd = localStorage["lau_pwd"];

	//20150924 stop using sql server //if (need_register)
	//20150924 stop using sql server //registerLau(lau_name, lau_pwd, m_id);
	//20150924 stop using sql server //else if (!isLoggedIn()) {
	//20150924 stop using sql server //loginLau(lau_name, lau_pwd, m_id);
	//20150924 stop using sql server //}
}
function isNotShown(elt) {
	return elt.hasClass("not_show") || elt.hasClass("not_show2");
}
function showDeleted() {
	$(".deleted").removeClass("not_show");
	fitStoreyWidth(true);
}
function isBoardOwner() {
	if (pre_boardsetting) {
		var user_id = getUserId();
		if (user_id) {		// if not logged in, user_id is NaN. falsy.
			if (pre_boardsetting[ChairOwnerGroupName + "UserIds"].indexOf(user_id) != -1)
				return true;
			if (pre_boardsetting[ViceOwnerGroupName + "UserIds"].indexOf(user_id) != -1)
				return true;
		}
	}
	return false;
}
function isInsider() {
	if (isBoardOwner())
		return true;
	if (pre_boardsetting) {
		var user_id = getUserId();
		if (user_id) {		// if not logged in, user_id is NaN. falsy.
			if (pre_boardsetting[InsiderGroupName + "UserIds"].indexOf(user_id) != -1)
				return true;
		}
	}
	return false;
}
function lastUsedNickname() {
	var text = localStorage["nn_last"];
	return text ? text : "船員";
}
function rememberNickname(nn) {
	localStorage["nn_last"] = nn;

	if (nn !== "船員") {
		var text = localStorage["nn_mem"];
		var arr = text ? text.split(",") : [/*"遊民"*/];

		arr = arr.filter(function (x) {
			return x != nn;
		});
		if (arr.length >= NICKNAME_MEMORY_CNT) {
			arr.shift();
		}
		arr.push(nn);
		text = arr.join(",");
		localStorage["nn_mem"] = text;

		populateNicknameList();
	}
}
function populateNicknameList() {
	var text = localStorage["nn_mem"];
	var arr = text ? text.split(",") : [/*"遊民"*/];

	var elt = $("#remembered_nn");
	elt.empty();

	elt.prepend("<option value='船員'/>");
	for (var i = 0; i < arr.length; i++) {
		elt.prepend("<option value='" + arr[i] + "'/>");
	}
}
function onDimensionChanged() {
	if (typeof drawRelationship === "function") {
		var w = $(window);
		var canvas_elt = document.getElementById("relationship_canvas");

		canvas_elt.width = g_document_width;
		canvas_elt.height = w.height();

		drawRelationship();
	}
}
function hrefToWindowName(href) {
	var pos = href.indexOf('#');
	if (pos == -1)
		return href;
	return href.substring(0, pos);
}
function writeAnchorHref($anchor, href, use_data) {
	if (use_data)
		$anchor.attr("data-href", href).attr("title", href);
	else {
		var window_name = hrefToWindowName(href);
		$anchor.attr("href", href).attr("target", window_name);
	}
	return $anchor;
}
function facebookShare(url) {
	//var board_id = getboardid();

	//var discussion_id = getdiscussionid();
	//var discussion_link = makeSandLink(board_id, discussion_id);

	FB.ui({
		method: 'share',
		href: url,
		//href: "http://www.hela.cc" + discussion_link + "?view=map",
	}, function (response) {
	});
}
function facebookInit() {
	window.fbAsyncInit = function () {
		FB.init({
			appId: '788545127852625',
			xfbml: false,
			version: 'v2.0'
		});
		/*FB.Event.subscribe('xfbml.render', function () {
			setTimeout(function () {
				fitStoreyWidth();
			}, 0);
		});*/
	};
	(function (d, s, id) {
		var js, fjs = d.getElementsByTagName(s)[0];
		if (d.getElementById(id)) return;
		js = d.createElement(s); js.id = id;
		js.src = "//connect.facebook.net/zh_TW/sdk.js";
		//js.src = "//connect.facebook.net/zh_TW/sdk/debug.js";
		fjs.parentNode.insertBefore(js, fjs);
	}(document, 'script', 'facebook-jssdk'));
}
function prepareShortcut(board_name) {
	var board_id = getboardid();
	var board_link = makeBoardLink(board_id, board_name);

	writeAnchorHref($("a[data-link='board']"), board_link).text("回" + board_name);
	writeAnchorHref($("a[data-link2='board']"), board_link, true).text("回" + board_name);
	//
	var create_link = "/creatediscussion/" + board_id/* + "/" + encodeURIComponent(board_name)*/;

	writeAnchorHref($("a[data-link='create_discussion']"), create_link);
	writeAnchorHref($("a[data-link='create_map_discussion']"), create_link + "?view=map");
	writeAnchorHref($("a[data-link='create_sky_discussion']"), create_link + "?view=sky");
	writeAnchorHref($("a[data-link='create_scribble_discussion']"), create_link + "?view=scb");
	//
	var discussion_id = getdiscussionid();
	if (discussion_id) {
		var discussion_link = makeSandLink(board_id, discussion_id);

		writeAnchorHref($("a[data-link='discussion']"), discussion_link);
		writeAnchorHref($("a[data-link2='discussion']"), discussion_link, true);
	}
}
function diffLargerThan(a, b, th) {
	var diff = Math.abs(a - b);
	return diff > th;
}
function fartherThan(x1, x2, y1, y2, th) {
	return diffLargerThan(x1, x2, th) || diffLargerThan(y1, y2, th);
}
function isMobile() {
	return false;
}
var n_debug_lines = 0;
function debugLog(text) {
	return;
	if (isMobile()) {
		var elt = $("#debug_log");
		var all = elt.text();

		n_debug_lines++;
		if (n_debug_lines >= 20) {
			all = "";
			n_debug_lines = 0;
		}

		all += text + "\n";
		elt.text(all);
	}
	else
		console.log(text);
}
