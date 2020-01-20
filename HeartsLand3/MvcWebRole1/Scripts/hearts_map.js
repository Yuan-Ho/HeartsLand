var g_map;
var g_panes;
var g_overlay;
var editing_letter_id;

HeartsOverlay.prototype = new google.maps.OverlayView();

function HeartsOverlay(map) {
	this.setMap(map);
}

$(function () {
	initializeMap();

	populateNicknameList();

	pagePrepare();

	onReady(false);

	onContentReady();
});

function initializeMap() {
	//var myLatLng = new google.maps.LatLng(23, 120);
	var myLatLng = new google.maps.LatLng(Math.random() * 160 - 80, Math.random() * 360 - 180);
	var mapOptions = {
		zoom: 7,
		center: myLatLng,
		mapTypeId: google.maps.MapTypeId.SATELLITE,
	};

	g_map = new google.maps.Map(document.getElementById('map_canvas'), mapOptions);
	g_overlay = new HeartsOverlay(g_map);
	var latest_click_latlng;

	google.maps.event.addListener(g_map, 'click', function (event) {
		//console.log("click. " + event.latLng.toString() + ".");
		latest_click_latlng = event.latLng;
	});
	document.addEventListener('dblclick', function (event) {
		if (event.target.tagName === "TEXTAREA" || event.target.tagName === "INPUT")
			return;
		if (!$(event.target).closest("#map_canvas").length)
			return;
		//console.log("document dblclick (capturing).");

		var zoomLevel = g_map.getZoom();
		//var foreword = ">>(" + latest_click_latlng.lat().toFixed(7) + "," + latest_click_latlng.lng().toFixed(7) + ")+" + zoomLevel + "\n";
		var foreword = buildForeword(latest_click_latlng, zoomLevel);

		if (editing_letter_id)
			moveEditWizard(event, foreword);
		else
			showCreateWizard(event, foreword);

		event.preventDefault();
		event.stopPropagation();
	}, true);
}
function extractLatLngMap(flags, coord) {
	coord.latlng = null;
	var text = flagsGet(flags, MT_COORDINATE);
	if (text !== null) {
		text = text.replace(/^\(([\d\.\-]+),([\d\.\-]+)\)\+(\d+)$/, function (match_text, $1, $2, $3) {
			coord.latlng = new google.maps.LatLng($1, $2);
			coord.zoom = Number($3);
			return "";
		});
	}
	//return text;
}
function resizeHandler() {
	var $div = $("#shortcut_panel");
	var $svg = $("#shortcut_panel svg");

	var div_height = $div.height();
	var div_width = $div.width();

	var ratio = div_width / 787/*image original size*/;
	var svg_height = ratio * 485/*image original size*/;

	$("#shortcut_panel image").attr("height", svg_height + "px").attr("width", div_width + "px");
	$svg.attr("height", (svg_height + 8) + "px").attr("width", (div_width + 7) + "px");
	// add 8 to reserve space for shadow.

	$svg[0].style.left = '0px';
	$svg[0].style.top = (div_height - svg_height) + 'px';

	collapseShortcut($div, div_height);
}
function onContentReady() {
	resizeHandler();
}
function toggleShortcut() {
	var $div = $("#shortcut_panel");
	var div_height = $div.height();

	if ($div[0].style.top == 0 || $div[0].style.top == "0px") {
		collapseShortcut($div, div_height);
	}
	else {
		$div[0].style.top = "0px";
	}
}
function collapseShortcut($div, div_height) {
	$div[0].style.top = -div_height + 16 * 3 + 'px';
}
function buildForeword(latlng, zoom) {
	var foreword = "(" + latlng.lat().toFixed(7) + "," + latlng.lng().toFixed(7) + ")+" + zoom;
	return foreword;
}
function releaseContextDrag() {
}
function cancelCreateLetter() {
	editing_letter_id = null;
	$("#create_wizard").hide();
	$("#edit_wizard").hide();
}
function showCreateWizard(event, foreword) {
	var $cw_form = $("#create_wizard");

	$cw_form.show().offset({ left: event.pageX, top: event.pageY });
	$cw_form.find(".error_info > span").text("").parent().hide();		// total 3.

	var words_elt = $cw_form.find("textarea");
	//var words = words_elt.val();
	//var orig_len = words.length;

	//words = extractLatLng(words, {});

	words_elt.data("foreword", foreword);

	//words = foreword + words;

	words_elt/*.val(words)*/.focus();

	//words_elt[0].selectionStart = words.length;
	//words_elt[0].selectionEnd = words.length;
}
function moveEditWizard(event, foreword) {
	var $ew_form = $("#edit_wizard");

	$ew_form.show().offset({ left: event.pageX, top: event.pageY });

	var words_elt = $ew_form.find("textarea");

	words_elt.data("foreword", foreword);
}

