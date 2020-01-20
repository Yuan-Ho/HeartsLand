var next_serial_number = 1;
var create_wizard_elt, edit_wizard_elt;
var g_curr_discussion_load;

function pagePrepare() {
	g_image_limit = Infinity;

	var board_name = pre_boardsetting.BoardName;
	prepareShortcut(board_name);

	create_wizard_elt = makeLetterEditor("create_wizard", "cw_insert_input_pt", "cw_insert_option_pt",
					{
						have_heading: false,
						have_reply_to: false,
						big: false,
						can_split: false,
						is_edit: false,
						no_preview: true,
						simplified_option: true,
					});
	edit_wizard_elt = makeLetterEditor("edit_wizard", "ew_insert_input_pt", "ew_insert_option_pt",
					{
						have_heading: false,
						have_reply_to: false,
						big: false,
						can_split: false,
						is_edit: true,
						no_preview: true,
						simplified_option: true,
					});
	//$("#create_wizard").hide();
	cancelCreateLetter();

	$("li[data-action] > a").on("click", onMenuClick);
	g_curr_discussion_load = $("#pre_discussionload");

	var $letter_slider = $("#letter_slider");

	$letter_slider.slider(
		{
			min: 1,
			max: 1,
			value: 1,
			slide: onSlide,
			change: onSlide,
		});
	$("#letter_slider_box > button:first").button().click(function (e) {
		var min = $letter_slider.slider("option", "min");
		var val = $letter_slider.slider("value");
		if (val > min)
			$letter_slider.slider("value", val - 1);
	});
	$("#letter_slider_box > button:last").button().click(function (e) {
		var max = $letter_slider.slider("option", "max");
		var val = $letter_slider.slider("value");
		if (val < max)
			$letter_slider.slider("value", val + 1);
	});
	facebookInit();
}
function onSlide(event, ui) {
	//console.log("onSlide. value=" + ui.value + ".");
	var letter_id = makeLetterId(ui.value);
	var elt = $("#" + letter_id);
	if (elt.length) {
		var latlng = elt.data("latlng");
		g_map.setCenter(latlng);
		$("#letter_slider > a").text(ui.value);
	}
}
function onNewDiscussionLoad(data) {
	g_curr_discussion_load = data;
	g_overlay.setMap(null);
	g_overlay.setMap(g_map);
}

function goCoord(coord) {
	g_map.setCenter(coord.latlng);
	g_map.setZoom(coord.zoom);
}

function appendSticker(elt) {
	g_panes.floatPane.appendChild(elt[0]);		// originally overlayImage, which will have zoom effect and cause delay.
}

function insertHeading(letter) {
	var html = "<h3 id='" + letter.letter_id + "'></h3>";
	var elt = $(html);

	elt.text(letter.words);

	var params = {
		creator_uid_link: letter.creator_uid_link,
	};
	saveParams(elt, params);

	$("#insert_point").parent().before(elt);
}
function insertLetter(/*panes, */sn, letter) {
	var coord = {};

    /*var words = */extractLatLngMap(letter.flags, coord);

	if (coord.latlng) {
		var html = "<div class='sticker' id='" + letter.letter_id + "'>" +
						"<strong></strong><a><mark></mark></a><section></section><p></p>" +
					"</div>";
		var elt = $(html);

		elt.find("strong").text(sn + ". ");
		elt.find("mark").text((letter.editor ? letter.editor : letter.creator) + ": ");

		var words = embedElements(letter.words);
		elt.find("section").html(words);
		elt.data("latlng", coord.latlng);
		elt.data("zoom", coord.zoom);
		//
		var n_ili = flagsGetNumber(letter.flags, MT_ILI);
		var n_idli = flagsGetNumber(letter.flags, MT_IDLI);

		var side_html = "";
		if (n_ili) {
			side_html += "<a onclick='voteLetter(MT_ILI, event);' title='" + n_ili + "個人說讚。'>"
						+ "<svg width='24' height='24'>"
						+ "<image xlink:href='/images/light_white2.png' width='24' height='24' filter='url(#orange-light)' />"
						+ "</svg></a>"
						+ "<span title='" + n_ili + "個人說讚。'>" + n_ili + "</span>";
			//side_html += "<img src='/Images/light_white.png'/>" + n_ili.toString();
		}
		if (n_idli) {
			side_html += "<a onclick='voteLetter(MT_IDLI, event);' title='" + n_idli + "個人說噓。'>"
						+ "<svg width='24' height='24'>"
						+ "<image xlink:href='/images/light_black.png' width='24' height='24' filter='url(#gray-light)' />"
						+ "</svg></a>"
						+ "<span title='" + n_idli + "個人說噓。'>" + n_idli + "</span>";
			//side_html += "<img src='/Images/light_black.png'/>" + n_idli.toString();
		}
		//console.log("n_ili=" + n_ili + ", n_idli=" + n_idli + ".");
		elt.find("p").html(side_html);
		//
		var params = {
			words: letter.words,
			creator_uid_link: letter.creator_uid_link,
		};
		saveParams(elt, params);

		appendSticker(elt);
		coord.letter_id = letter.letter_id;
		return coord;
	}
}
HeartsOverlay.prototype.onAdd = function () {
	/*var panes*/g_panes = this.getPanes();
	//var map = this.getMap();

	var letter_coords = processDiscussionLoadSquare(g_curr_discussion_load);

	if (letter_coords.first) {
		var sn = letterIdToSerialNumber(letter_coords.first.letter_id);
		$("#letter_slider_box > span:first").text(sn);
		$("#letter_slider").slider("option", "min", sn);
	}
	if (letter_coords.last) {
		var sn = letterIdToSerialNumber(letter_coords.last.letter_id);
		$("#letter_slider_box > span:last").text(sn);
		$("#letter_slider").slider("option", "max", sn);
	}
	if (letter_coords.home) {
		var sn = letterIdToSerialNumber(letter_coords.home.letter_id);
		$("#letter_slider > a").text(sn);
		$("#letter_slider").slider("value", sn);
	}

	//resizeHandler();		// heading changed shortcut panel size.
	onContentReady();
};
HeartsOverlay.prototype.onRemove = function () {
	cancelCreateLetter();

	clearLetterElements();
};
HeartsOverlay.prototype.draw = function () {
	var overlayProjection = this.getProjection();

	$("div.sticker").each(function (idx, elt) {
		var $elt = $(elt);
		var latlng = $elt.data("latlng");

		if (latlng) {
			var pos = overlayProjection.fromLatLngToDivPixel(latlng);

			elt.style.left = pos.x + 'px';
			elt.style.top = pos.y + 'px';
		}
	});
};
function goGoogleMap() {
	var center = g_map.getCenter();
	var zoom = g_map.getZoom();

	// https://www.google.com/maps/@39.7361626,-105.0806782,11z
	var parameter = "@" + center.k + "," + center.B + "," + zoom + "z";

	window.open("https://www.google.com/maps/" + parameter);
}
function editLetter() {
	var target_elt = $(g_room_menu_target).closest("div.sticker");
	var letter_id = target_elt.attr("id");

	if (isLetterId(letter_id)) {
		var sn = letterIdToSerialNumber(letter_id);
		var tgt_params = getRoomParams(target_elt);

		cancelCreateLetter();

		var coord = {};
		//var words = extractLatLng(tgt_params.words, coord);
		coord.latlng = target_elt.data("latlng");
		coord.zoom = target_elt.data("zoom");

		if (coord.latlng) {
			editing_letter_id = letter_id;

			var pos = target_elt.offset();
			var $ew_form = $("#edit_wizard");

			$ew_form.show().offset(pos);
			$ew_form.find(".error_info > span").text("").parent().hide();

			var words_elt = $ew_form.find("textarea");

			//var foreword = ">>(" + coord.latlng.lat().toFixed(7) + "," + coord.latlng.lng().toFixed(7) + ")+" + coord.zoom + "\n";
			var foreword = buildForeword(coord.latlng, coord.zoom);
			words_elt.data("foreword", foreword);

			words_elt.val(tgt_params.words).focus();
			words_elt.trigger("input");
		}
	}
}
