var next_serial_number = 1;
var create_wizard_elt, edit_wizard_elt;
var editing_letter_id;
var letter_elts = [];

function pagePrepare() {
	g_image_limit = Infinity;

	var board_name = pre_boardsetting.BoardName;
	prepareShortcut(board_name);

	$("#map_canvas").on("dblclick", function (event) {
		if ($(event.target).closest("div.storey").length)
			return;

		//console.log("dblclick.");
		var big_coord = offsetToBigCoord({ left: event.pageX, top: event.pageY });

		//var pos = { left: event.pageX, top: event.pageY };

		if (editing_letter_id)
			showEditWizard(big_coord);
		else
			showCreateWizard(big_coord);

		return false;
	});
	$("#ew_storey").removeClass("not_show");
	$("#cw_storey").removeClass("not_show");

	create_wizard_elt = makeLetterEditor("create_wizard", "cw_insert_input_pt", "cw_insert_option_pt",
				{
					have_heading: false,
					have_reply_to: true,
					big: false,
					get_new_sn: function () { return next_serial_number; },
					can_split: false,
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

	g_letter_coords = processDiscussionLoadSquare($("#pre_discussionload"));

	facebookInit();
}
function onNewDiscussionLoad(data) {
	letter_elts.length = 0;

	cancelCreateLetter();

	clearLetterElements();

	g_letter_coords = processDiscussionLoadSquare(data);
	//resizeHandler();
	onContentReady();
}
function insertHeading(letter) {
	var coord = {};
	/*var words = */extractLatLng(letter.flags, coord);		// e0 may or may not have foreword.

	if (coord.latlng) {
		// letter.words = words;
	}
	else
		coord.latlng = { x: 0, y: 0 };

	var elt = insertLetter2(letter, undefined, false);
	elt.data("latlng", coord.latlng);

	appendSticker(elt, false);
	coord.letter_id = letter.letter_id;

	//elt.on("click", "a:has(header)", showRoomMenu);
	$("a[data-link2='discussion']").text(letter.words);
}
function insertLetter(sn, letter) {
	var coord = {};
	/*var words = */extractLatLng(letter.flags, coord);

	if (coord.latlng) {
		//letter.words = words;

		var elt = insertLetter2(letter, undefined, false);
		elt.data("latlng", coord.latlng);
		//console.log("coord.latlng=(" + coord.latlng.x + ", " + coord.latlng.y + ").");

		appendSticker(elt, true);
		coord.letter_id = letter.letter_id;

		letter_elts.push(elt);		// width() may not be correct now so position again later.

		return coord;
	}
}
function goCoord(coord) {
	var w = g_panes.width();
	var h = g_panes.height();

	small_view_origin.x = coord.latlng.x - w / 2;
	small_view_origin.y = coord.latlng.y - h / 2;

	var $elt = $("#" + coord.letter_id);
	small_view_origin.x -= $elt.width() / 2;
	small_view_origin.y += $elt.height() / 2;

	//console.log("small_view_origin=(" + small_view_origin.x + ", " + small_view_origin.y + ").");

	moveStickerPane();
}
function appendSticker(elt, origin_at_right) {
	$("#insert_point").before(elt);
	//g_panes.append(elt);

	fitStoreyWidthElement(elt);		// call this to make width() correct.

	var big_coord = elt.data("latlng");		// don't modify latlng directly, it will reflect into the data.
	//var pos = { x: latlng.x, y: latlng.y };
	var off = bigCoordToOffset(big_coord);

	if (origin_at_right) {
		var width = elt.width();		// may temporarily be smaller than correct value for tds containing tbrl section.
		off.left/*pos.x*/ -= width;
		//console.log("(a) width=" + width + ".");
	}
	//elt[0].style.left = pos.x + 'px';
	//elt[0].style.top = pos.y + 'px';
	elt.offset(off);		// inner side of margin. outer side of border.
}
function positionSticker() {
	//console.log("positionSticker().");
	for (var i = 0; i < letter_elts.length; i++) {
		var elt = letter_elts[i];

		var big_coord = elt.data("latlng");		// don't modify latlng directly, it will reflect into the data.
		//var pos = { x: latlng.x, y: latlng.y };

		var width = elt.width();
		//pos.x -= width;
		//console.log("(p) width=" + width + ".");

		//elt[0].style.left = pos.x - parseFloat(elt.css("margin-left")) + 'px';		// outer side of margin.
		//elt[0].style.top = pos.y - parseFloat(elt.css("margin-top")) + 'px';
		//
		var off = bigCoordToOffset(big_coord);
		off.left -= width;
		elt.offset(off);		// inner side of margin. outer side of border.
		//
		checkPosition(elt, big_coord);
	}
}
function checkPosition(elt, big_coord) {
	var off = elt.offset();		// inner side of margin.
	var width = elt.width();
	off.left += width;		// width() does not include padding, border, or margin.
	//console.log("(c) width=" + width + ".");

	var big_coord2 = offsetToBigCoord(off);
	//if (diffLargerThan(big_coord2.x, big_coord.x, 1) || diffLargerThan(big_coord2.y, big_coord.y, 1))
	if (fartherThan(big_coord2.x, big_coord.x, big_coord2.y, big_coord.y, 1))
		console.log("big_coord mismatches. (" + big_coord2.x + ", " + big_coord2.y + "). (" + big_coord.x + ", " + big_coord.y + ")!");
}
function editLetter() {
	var target_elt = $(g_room_menu_target).closest("table.room");
	var letter_id = target_elt.attr("id");

	if (isLetterId(letter_id)) {
		var sn = letterIdToSerialNumber(letter_id);
		var tgt_params = getRoomParams(target_elt);

		cancelCreateLetter();

		//var coord = {};
		var big_coord = target_elt.data("latlng");

		//var words = extractLatLng(tgt_params.words, coord);

		//if (big_coord/*coord.latlng*/) {
			editing_letter_id = letter_id;

			//var pos = target_elt.offset();
			//pos.left += target_elt.width();		// width() does not include padding, border, or margin.
			//
			checkPosition(target_elt, big_coord/*coord.latlng*/);
			//
			showEditWizard(big_coord/*coord.latlng*/, target_elt);
		//}
	}
}
function onEditorContentChanged($wizard) {
	var big_coord = $wizard.data("latlng");
	if (big_coord)
		moveWizardInternal($wizard, big_coord);
}
function cancelCreateLetter() {
	editing_letter_id = null;
	$("#cw_storey").data("latlng", null).hide();
	$("#ew_storey").data("latlng", null).hide();
}
function showEditWizard(big_coord, target_elt) {
	showWizard("ew_storey", big_coord);

	if (target_elt)
		edit_wizard_elt.showEdit(target_elt);

	setTimeout(function () {
		moveWizard("ew_storey", big_coord);
	}, 0);		// to let width() become correct.
}
function showCreateWizard(big_coord) {
	showWizard("cw_storey", big_coord);

	create_wizard_elt.showCreate();

	setTimeout(function () {
		moveWizard("cw_storey", big_coord);
	}, 0);		// to let width() become correct.
}
