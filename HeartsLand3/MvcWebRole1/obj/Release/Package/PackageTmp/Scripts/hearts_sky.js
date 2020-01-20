var g_panes;
var small_view_origin = { x: 0, y: 0 };
var g_letter_coords;
var background_origin = { x: 0, y: 0 };

$(function () {
	if (checkNavigator()) {
		doWrap();

		initializeSky();

		populateNicknameList();

		pagePrepare();

		onReady(false);

		var $map_canvas = $("#map_canvas");
		$map_canvas.mousedown(mouseDownHandler);
		$map_canvas.mouseup(mouseUpHandler);
		$map_canvas.mousemove(mouseMoveHandler);

		$map_canvas.on("touchstart", onTouchStart);
		$map_canvas.on("touchend", onTouchEnd);
		$map_canvas.on("touchcancel", onTouchEnd);
		$map_canvas.on("touchleave", onTouchEnd);
		$map_canvas.on("touchmove", onTouchMove);

		$map_canvas.click(clickHandler);

		//console.log("$().");
		onContentReady();
		setTimeout(resizeHandler, 0/*100*/);		// run again so that width of tds containing tbrl section become correct.
	}
});
function positionButtons() {
	var total_w = g_panes.width();
	var total_h = g_panes.height();

	var cloud_elts = $("a.cloud_ground");

	cloud_elts.each(function (idx, a_elt) {
		var cloud_elt = $(a_elt);

		var div_w = cloud_elt.width();		// Width becomes correct after inserting into document.
		var spacing_w = (total_w - cloud_elts.length * div_w) / (2 * cloud_elts.length);		// may be negative if total_w is small.	

		cloud_elt.css("left", (spacing_w + (2 * spacing_w + div_w) * idx) + "px");
	});
	//
	var sky_elts = $("div.sky_passage");
	sky_elts.each(function (idx, a_elt) {
		var elt = $(a_elt);
		var div_w = elt.width();

		elt.css("left", ((1.1 * div_w) * (idx - 1) - div_w / 2 + total_w / 2 + 5/*to contain hole completely*/) + "px");
	});
}
function initializeSky() {
	g_panes = $("#sticker_pane");

	background_origin.x = Math.floor(Math.random() * 431/*width of background image*/);
	background_origin.y = Math.floor(Math.random() * 426/*height of background image*/);
}
function offsetToBigCoord(off) {
	var map_canvas = document.getElementById("map_canvas");

	off.left -= map_canvas.offsetLeft;
	off.top -= map_canvas.offsetTop;

	var big_coord = {};

	big_coord.x = off.left + small_view_origin.x;
	big_coord.y = off.top + small_view_origin.y;

	//console.log("small_view_origin=(" + small_view_origin.x + ", " + small_view_origin.y + ").");

	return big_coord;
}
function bigCoordToOffset(big_coord) {
	var off = {};
	off.left = big_coord.x - small_view_origin.x;
	off.top = big_coord.y - small_view_origin.y;

	var map_canvas = document.getElementById("map_canvas");

	off.left += map_canvas.offsetLeft;
	off.top += map_canvas.offsetTop;

	return off;
}

function resizeHandler() {
	//console.log("resizeHandler().");
	positionButtons();

	fitStoreyWidth(false);

	if (typeof positionSticker === "function")
		positionSticker();
}
function onContentReady() {
	//console.log("onContentReady().");
	doWrap();

	resizeHandler();
}
function buildForeword(latlng, zoom) {
	var foreword = "(" + latlng.x + "," + latlng.y + ")";
	return foreword;
}
function moveStickerPane() {
	var transform = "matrix(1,0,0,1," + -small_view_origin.x + "," + -small_view_origin.y + ")";
	g_panes.css("transform", transform);

	$("div.sky_hole").css("background-position-x", (background_origin.x - small_view_origin.x) + "px")
					.css("background-position-y", (background_origin.y - small_view_origin.y) + "px");
}
function wheelStickerPane(delta, up_down) {
	if (up_down)
		small_view_origin.y -= delta;
	else
		small_view_origin.x += delta;

	moveStickerPane();
}
function onKeyDown(keyname) {
	if (keyname == "Home") {
		if (g_letter_coords && g_letter_coords.first)
			goCoord(g_letter_coords.first);
		return true;
	}
	else if (keyname == "End") {
		if (g_letter_coords && g_letter_coords.last)
			goCoord(g_letter_coords.last);
		return true;
	}
	else if (keyname == "PageUp") {
		wheelStickerPane(window.innerWidth * 0.6, false);
		return true;
	}
	else if (keyname == "PageDown") {
		wheelStickerPane(-window.innerWidth * 0.6, false);
		return true;
	}
	else if (keyname == "Right") {
		wheelStickerPane(window.innerWidth / 40, false);
		return true;
	}
	else if (keyname == "Left") {
		wheelStickerPane(-window.innerWidth / 40, false);
		return true;
	}
	else if (keyname == "Up") {
		wheelStickerPane(window.innerHeight / 40, true);
		return true;
	}
	else if (keyname == "Down") {
		wheelStickerPane(-window.innerHeight / 40, true);
		return true;
	}
}
////////////////////////////////////////////////////////////////////////////////
function showWizard(id, big_coord) {
	var $wizard = $("#" + id);
	$wizard.show();
	$wizard.data("latlng", big_coord);

	var elt = document.getElementById("map_canvas");
	if (elt.scrollTop !== 0 || elt.scrollLeft !== 0)
		console.log("scrollTop: " + elt.scrollTop + ", scrollLeft: " + elt.scrollLeft + ".");

	//elt.scrollTop = 0;
	//elt.scrollLeft = 0;

	var foreword = buildForeword(big_coord);
	var words_elt = $wizard.find("textarea");
	words_elt.data("foreword", foreword)/*.focus()*/;		// the focus() moves panes unexpectedly.
}
function moveWizardInternal($wizard, big_coord) {
	var off = bigCoordToOffset(big_coord);

	var $tables = $wizard.children("table.room");

	var w = $tables.eq(0).outerWidth(true) + $tables.eq(1).width();
	//console.log("moveWizardInternal. w=" + w + ".");
	off.left -= w;

	off.left -= parseFloat($wizard.css("border-left-width"));		// 20px
	//pos.left -= parseFloat($tables.eq(0).css("margin-left"));		// 2px
	//pos.left -= parseFloat($tables.eq(0).css("margin-right"));		// 2px
	off.left -= parseFloat($tables.eq(1).css("margin-left"));		// 2px

	off.top -= parseFloat($wizard.css("border-top-width"));			// 20px
	off.top -= parseFloat($tables.eq(1).css("margin-top"));			// 19.2px

	$wizard.offset(off);		// inner side of margin. outer side of border.

	small_view_origin.x = big_coord.x - w - 50;
	small_view_origin.y = big_coord.y - ($(window).height() - $wizard.height()) / 2;

	moveStickerPane();
}
function moveWizard(id, big_coord) {
	var $wizard = $("#" + id);
	moveWizardInternal($wizard, big_coord);

	var words_elt = $wizard.find("textarea");
	words_elt.focus();

	var elt = document.getElementById("map_canvas");
	if (elt.scrollTop !== 0 || elt.scrollLeft !== 0)
		console.log("scrollTop: " + elt.scrollTop + ", scrollLeft: " + elt.scrollLeft + ".");
}

////////////////////////////////////////////////////////////////////////////////
var contextdrag = {
	state: 0,		// 0=inactive, 1=dragging, 2=dragging and cancel other handler.
	orig: { x: 0, y: 0 },
	sorig: { left: 0, top: 0 },
};
function startDrag(event) {
	contextdrag.state = 1;

	contextdrag.orig.x = event.pageX;
	contextdrag.orig.y = event.pageY;

	contextdrag.sorig.left = small_view_origin.x;
	contextdrag.sorig.top = small_view_origin.y;

	$("#map_canvas").addClass("dragging");
}
function endDrag(event) {
	collapseMenu();
	return releaseContextDrag();
}
function moveDrag(event) {
	if (contextdrag.state == 1) {
		//var dist_x = Math.abs(event.pageX - contextdrag.orig.x);
		//var dist_y = Math.abs(event.pageY - contextdrag.orig.y);

		//if (dist_x >= MOUSE_MOVE_THRESHOLD || dist_y >= MOUSE_MOVE_THRESHOLD)
		if (fartherThan(event.pageX, contextdrag.orig.x, event.pageY, contextdrag.orig.y, MOUSE_MOVE_THRESHOLD))
			contextdrag.state = 2;
	}
	if (contextdrag.state >= 1) {
		small_view_origin.x = contextdrag.sorig.left - (event.pageX - contextdrag.orig.x);
		small_view_origin.y = contextdrag.sorig.top - (event.pageY - contextdrag.orig.y);

		moveStickerPane();
	}
}
function releaseContextDrag() {
	debugLog("releaseContextDrag().");

	if (contextdrag.state == 1) {
		contextdrag.state = 0;
		$("#map_canvas").removeClass("dragging");
	}
	else if (contextdrag.state != 0) {
		contextdrag.state = 0;
		$("#map_canvas").removeClass("dragging");
		return false;
	}
}
/////
function mouseDownHandler(event) {
	if (event.target.tagName === "TEXTAREA" || event.target.tagName === "INPUT")
		return;

	if (event.which == 1) {
		console.log("left mouse down. orig: (" + event.pageX + ", " + event.pageY + ").");
		startDrag(event);
	}
}
function mouseUpHandler(event) {
	if (event.which == 1)
		console.log("left mouse up.");
}
function clickHandler(event) {
	console.log("click.");
	return endDrag(event);
}
function mouseMoveHandler(event) {
	//console.log("mouse move. x=" + event.pageX + ", y=" + event.pageY + ".");
	moveDrag(event);
}
/////
function onTouchStart(event) {
	var touch = event.originalEvent.changedTouches[0];
	debugLog("touchstart. (" + touch.pageX + ", " + touch.pageY + ").");
	startDrag(touch);
}
function onTouchEnd(event) {
	var touch = event.originalEvent.changedTouches[0];
	debugLog("touchend. (" + touch.pageX + ", " + touch.pageY + ").");
	endDrag(touch);
}
function onTouchMove(event) {
	var touch = event.originalEvent.changedTouches[0];
	debugLog("touchmove. (" + touch.pageX + ", " + touch.pageY + ").");
	moveDrag(touch);
}
