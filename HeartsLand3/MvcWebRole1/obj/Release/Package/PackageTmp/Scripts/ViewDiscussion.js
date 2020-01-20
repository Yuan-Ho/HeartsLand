var next_serial_number = 1;
var create_wizard_elt, edit_wizard_elt;
var editing_letter_id;
var have_reply_to_elts = [];

function pagePrepare() {
	g_image_limit = Infinity;

	var board_id = getboardid();
	var discussion_id = getdiscussionid();

	var board_name = pre_boardsetting.BoardName;
	var board_link = makeBoardLink(board_id, board_name);

	prepareShortcut(board_name);

	makeRoomBanner({
		words: "心船",
		position: "bottom",
		link: "/",
		insert_before: "insert_point"
	});
	makeRoomBanner({
		//id: "board_name",
		words: board_name/*"留言板名"*/,
		position: "bottom",
		link: board_link,
		insert_before: "insert_point"
	});
	create_wizard_elt = makeLetterEditor("create_wizard", "cw_insert_input_pt", "cw_insert_option_pt",
					{
						have_heading: false,
						have_reply_to: true,
						big: false,
						get_new_sn: function () { return next_serial_number; },
						can_split: true,
						is_edit: false
					});
	edit_wizard_elt = makeLetterEditor("edit_wizard", "ew_insert_input_pt", "ew_insert_option_pt",
					{
						have_heading: false,
						have_reply_to: true,
						big: false,
						can_split: false,
						is_edit: true
					});
	cancelCreateLetter();

	$("li[data-action] > a").on("click", onMenuClick);

	//modifyRoomBanner($("#board_name"), {
	//	words: board_name,
	//	link: board_link
	//});

	//getGoods("/discussionload/" + board_id + "/" + discussion_id, onDiscussionLoad);
	processDiscussionLoad($("#pre_discussionload"));

	facebookInit();
}
function onNewDiscussionLoad(data) {
	have_reply_to_elts.length = 0;

	cancelCreateLetter();

	clearLetterElements();

	processDiscussionLoad(data);
	//resizeHandler();
	onContentReady();
}
function processDiscussionLoad(data) {
	var letter_arr = [];

	data.children("section").each(function (idx, elt) {
		var letter = dataToLetter($(elt));

		var sn = letterIdToSerialNumber(letter.letter_id);

		letter_arr[sn] = letter;
	});
	var has_deleted_item = false;
	var has_insider_only_item = false;

	do {
		var n_inserted = 0;
		//for (var sn = 0; sn < letter_arr.length; sn++) {
		for (var sn = letter_arr.length - 1; sn >= 0; sn--) {
			if (!letter_arr[sn]) continue;
			var letter = letter_arr[sn];

			if (sn >= next_serial_number)
				next_serial_number = sn + 1;

			var reply_to_sn = flagsGetNumber(letter.flags, MT_REPLY_TO);
			var insert_after = "insert_point";

			if (reply_to_sn !== 0) {
				var reply_to_letter_id = makeLetterId(reply_to_sn);

				insert_after = $("#" + reply_to_letter_id);
				if (insert_after.length === 0) continue;
			}
			letter_arr[sn] = null;
			n_inserted++;

			var deleted = isLetterDeleted(letter.flags);	// flagsCheck(letter.flags, DELETED_FLAG_CHAR, 1);
			var insider_only = flagsCheck(letter.flags, MT_AUTHORIZATION, 2);
			if (deleted)
				has_deleted_item = true;
			if (insider_only)
				has_insider_only_item = true;

			var elt = insertLetter2(letter, undefined, deleted, insert_after);

			if (reply_to_sn !== 0)
				have_reply_to_elts.push(elt);

			//if (letter.subtype === "h") {
			//	elt.on("click", "a:has(header)", showRoomMenu);
			//}
		}
	} while (n_inserted > 0);

	//if (has_deleted_item && isBoardOwner())
	//	$("#show_deleted_btn").removeClass("not_show");

	var is_discussion_admin = isBoardOwner() || isDiscussionCreator();

	$("#show_deleted_btn").toggleClass("not_show", !(has_deleted_item && is_discussion_admin));

	if (has_insider_only_item && isInsider())
		prepareDecrypt();
	if (is_discussion_admin)
		$(".delete_remark").removeClass("not_show2");
}
function prepareDecrypt() {
	var board_id = getboardid();
	var discussion_id = getdiscussionid();
	var key = "discussionkey1" + "." + board_id + "." + discussion_id;

	if (!localStorage[key])
		getGoods("/discussionkey/" + board_id + "/" + discussion_id, onDiscussionKey);
	else
		doDecrypt(localStorage[key]);
}
function onDiscussionKey(data) {
	if (data !== null) {
		var board_id = getboardid();
		var discussion_id = getdiscussionid();
		var key = "discussionkey1" + "." + board_id + "." + discussion_id;

		localStorage[key] = data.key;
		doDecrypt(localStorage[key]);
	}
}
function doDecrypt(key) {
	var wa_key = CryptoJS.enc.Base64.parse(key);

	$("table.room.insider_only").each(function (idx, elt) {
		var $elt = $(elt);
		var params = getRoomParams($elt);

		var wa_words = CryptoJS.enc.Base64.parse(params.orig_words);

		var decrypted = CryptoJS.AES.decrypt({ ciphertext: wa_words }, wa_key, { iv: wa_key });
		var decrypted_words = CryptoJS.enc.Utf8.stringify(decrypted);

		var processed_words = preprocessWordsInData(decrypted_words);

		modifyRoomTable($elt, { words: processed_words });

		//$elt.removeClass("not_show2");
	});
	fitStoreyWidth(true);
}
function editLetter() {
	var target_elt = $(g_room_menu_target).closest("table.room");
	var letter_id = target_elt.attr("id");

	if (isLetterId(letter_id)) {
		editing_letter_id = letter_id;

		var sn = letterIdToSerialNumber(letter_id);

		edit_wizard_elt.showEdit(target_elt);
	}
}

function replyToLetter() {
	var target_elt = $(g_room_menu_target).closest("table.room");
	var letter_id = target_elt.attr("id");

	if (isLetterId(letter_id)) {
		var sn = letterIdToSerialNumber(letter_id);
		if (sn > 0) {
			create_wizard_elt.showCreate(target_elt, sn);
		}
	}
}
function doDrawHalf(ctx, from_pt, to_sn) {
	var theta = Math.PI / 3;
	var x = 100;
	var h = Math.tan(theta) * x / 2;
	var r = 1 / Math.cos(theta) * x / 2;

	ctx.beginPath();
	ctx.arc(from_pt.left + x / 2, from_pt.top + h, r, Math.PI + theta, Math.PI * 3 / 2, false);

	ctx.strokeText(to_sn, from_pt.left + x / 2 + 5, from_pt.top - (r - h));

	ctx.stroke();
}
function doDraw(ctx, from_pt, to_pt) {
	var theta = Math.PI / 3;
	var x = Math.abs(from_pt.left - to_pt.left);
	var h = Math.tan(theta) * x / 2;
	var r = 1 / Math.cos(theta) * x / 2;

	ctx.beginPath();
	ctx.arc((from_pt.left + to_pt.left) / 2, (from_pt.top + to_pt.top) / 2 + h, r, Math.PI + theta, 2 * Math.PI - theta, false);

	ctx.stroke();
}
function drawRelationship() {
	var ctx = document.getElementById("relationship_canvas").getContext("2d");

	ctx.strokeStyle = "darkcyan";
	ctx.lineWidth = 2;
	ctx.textBaseline = "middle";
	//ctx.font = "16px sans-serif";

	for (var i = 0; i < have_reply_to_elts.length; i++) {
		var from_elt = have_reply_to_elts[i];
		var params = getRoomParams(from_elt);
		var reply_to_letter_id = makeLetterId(params.reply_to_sn);

		if (isNotShown(from_elt))
			continue;

		var to_elt = $("#" + reply_to_letter_id);

		if (to_elt.length > 0) {
			from_elt = from_elt.find("td.room_body > section");

			var from_pt = from_elt.offset();
			from_pt.left += from_elt.outerWidth() - (from_elt.outerWidth() - from_elt.width()) / 2;
			from_pt.top -= 10;

			if (isNotShown(to_elt)) {
				doDrawHalf(ctx, from_pt, params.reply_to_sn);
			}
			else {
				to_elt = to_elt.find("td.room_body > section");

				var to_pt = to_elt.offset();
				to_pt.left += (to_elt.outerWidth() - to_elt.width()) / 2;
				to_pt.top -= 10;

				doDraw(ctx, from_pt, to_pt);
			}
		}
	}
}
function cancelCreateLetter() {
	editing_letter_id = null;
	create_wizard_elt.hide();
	edit_wizard_elt.hide();
}
