var next_serial_number = 1;
var sd = {
	activePath: null,
	currBlendMode: "normal",
	redoArray: [],
	cursorCircle: null,

	scribbleProject: null,
	toolProject: null,

	scribbleTool: null,
	dropperTool: null,
	tipSizeTool: null,
	handTool: null,
	poserTool: null,

	penStyle: {
		strokeWidth: 2,
		strokeColor: "red",
	},
	eraserStyle: {
		strokeWidth: 20,
		strokeColor: "grey",
	},
	prevColor: null,
	prevStrokeWidth: null,
	prevViewCenter: null,

	currStylePtr: null,
	currBlobText: null,

	lastMouseEvent: null,
	lastMousePos: { x: 0, y: 0 },
	lastPathInfo: null,
};
$(function () {
	initializeScribble();

	populateNicknameList();

	pagePrepare();

	onReady(false);

	onContentReady();
});
function initializeScribble() {
	//
	paper.setup("scribble_canvas");
	sd.scribbleProject = paper.project;

	paper.setup("tool_canvas");
	sd.toolProject = paper.project;
	//
	sd.dropperTool = new paper.Tool();
	sd.dropperTool.onMouseDown = mouseDownDropper;
	sd.dropperTool.onMouseDrag = mouseDragDropper;
	sd.dropperTool.onMouseUp = mouseUpDropper;
	sd.dropperTool.onMouseMove = mouseMoveDropper;

	sd.tipSizeTool = new paper.Tool();
	sd.tipSizeTool.onMouseDown = mouseDownTipSize;
	sd.tipSizeTool.onMouseDrag = mouseDragTipSize;
	sd.tipSizeTool.onMouseUp = mouseUpTipSize;
	sd.tipSizeTool.onMouseMove = mouseMoveTipSize;

	sd.handTool = new paper.Tool();
	sd.handTool.onMouseDown = mouseDownHand;
	sd.handTool.onMouseDrag = mouseDragHand;
	sd.handTool.onMouseUp = mouseUpHand;
	sd.handTool.onMouseMove = mouseMoveHand;

	sd.poserTool = new paper.Tool();
	sd.poserTool.onMouseDown = mouseDownHand;
	sd.poserTool.onMouseDrag = mouseDragHand;
	sd.poserTool.onMouseUp = mouseUpPoser;
	sd.poserTool.onMouseMove = mouseMoveHand;

	sd.scribbleTool = new paper.Tool();
	sd.scribbleTool.onMouseDown = mouseDownScribble;
	sd.scribbleTool.onMouseDrag = mouseDragScribble;
	sd.scribbleTool.onMouseUp = mouseUpScribble;
	sd.scribbleTool.onMouseMove = mouseMoveScribble;

	scribblePen();
	//
	var ini_color = {
		r: Math.floor(Math.random() * 200 + 28),
		g: Math.floor(Math.random() * 200 + 28),
		b: Math.floor(Math.random() * 200 + 28)
	};
	var ini_clr_text = "#" + ini_color.r.toString(16) + ini_color.g.toString(16) + ini_color.b.toString(16);
	updateColor(ini_clr_text);

	$("#color_input").val(ini_clr_text)
		.chromoselector({
			target: "#color_pane",
			autoshow: false,
			resizable: false,
			update: function () {
				var hex_color = $(this).chromoselector("getColor").getHexString();
				updateColor(hex_color);
			}
		}).chromoselector("show", 0);

	$("#color_pane").append($("#color_buttons"));		// move to under color wheel.
}
function resizeHandler() {
	var win = $(window);

	var total_w = win.width();
	var total_h = win.height();

	var elts = $("div.bottom_passage");
	elts.each(function (idx, a_elt) {
		var elt = $(a_elt);
		var div_w = elt.width();
		var sh = (elts.length - 1) / 2;

		elt.css("left", ((1.1 * div_w) * (idx - sh) - div_w / 2 + total_w / 2) + "px");
	});
	var elts = $("div.top_passage");
	elts.each(function (idx, a_elt) {
		var elt = $(a_elt);
		var div_w = elt.width();
		var sh = (elts.length - 1) / 2;

		elt.css("left", ((1.1 * div_w) * (idx - sh) - div_w / 2 + total_w / 2) + "px");
	});
	var elts = $("div.left_passage");
	elts.each(function (idx, a_elt) {
		var elt = $(a_elt);
		var div_h = total_h / elts.length/*elt.height()*/;
		var sh = (elts.length - 1) / 2;

		elt.css("top", (div_h * idx) + "px");
		//elt.css("top", ((1.1 * div_h) * (idx - sh) - div_h / 2 + total_h / 2) + "px");
	});
}
function onContentReady() {
	resizeHandler();
}
function releaseContextDrag() {
}
function pagePrepare() {
	g_image_limit = Infinity;

	var board_name = pre_boardsetting.BoardName;
	prepareShortcut(board_name);

	$("#floating_info").hide();

	processDiscussionLoadSquare($("#pre_discussionload"));

	facebookInit();
}
function insertHeading(letter) {
	$("a[data-link2='discussion']").text(letter.words);
}
function insertLetter(sn, letter) {
	var coord = {};
	extractLatLng(letter.flags, coord);

	if (coord.latlng) {
		var letter_type = flagsGetNumber(letter.flags, MT_LETTER_TYPE);
		if (letter_type === 1/*image*/) {
			var html = embedElements(letter.words);
			var elt = $(html);

			elt.data("latlng", coord.latlng);
			elt.attr("id", letter.letter_id);
			elt.addClass("not_show");

			$("#chosen_poser").after(elt);

			coord.letter_id = letter.letter_id;

			return coord;
		}
	}
}
function onImageReadySpecific(elt) {
	var $elt = $(elt);
	var latlng = $elt.data("latlng");

	if (latlng) {
		sd.scribbleProject.activate();

		var letter_id = $elt.attr("id");
		var raster = new paper.Raster(letter_id);

		//console.log("Raster width=" + raster.width + ", height=" + raster.height + ".");

		raster.position.x = latlng.x - raster.width / 2;
		raster.position.y = latlng.y + raster.height / 2;
		return false;
	}
}
function goCoord(coord) {
}
/////////
function updateColor(hex_color, set_selector) {
	if (set_selector)
		$("#color_input").chromoselector("setColor", hex_color);
	else {
		console.log("updateColor. " + hex_color);
		$("#color_btn").css("color", hex_color);
		$("#clr_ok_btn").css("background-color", hex_color);

		sd.penStyle.strokeColor = hex_color;
		updateCursor();
	}
}
function updateCursor() {
	sd.toolProject.activate();
	var pos = sd.lastMousePos/* || paper.view.center*/;
	if (sd.cursorCircle !== null) {
		sd.cursorCircle.remove();
		sd.cursorCircle = null;
	}
	if (paper.tool === sd.scribbleTool) {
		sd.cursorCircle = new paper.Path.Circle(pos, sd.currStylePtr.strokeWidth / 2);

		sd.cursorCircle.strokeColor = sd.currStylePtr.strokeColor;
		sd.cursorCircle.strokeWidth = 1;
	}
	else if (paper.tool === sd.tipSizeTool) {
		sd.cursorCircle = new paper.Path.Circle(pos, sd.currStylePtr.strokeWidth / 2);

		sd.cursorCircle.strokeColor = "red";
		sd.cursorCircle.strokeWidth = 1;
		sd.cursorCircle.dashArray = [1, 1];

		$("#floating_info").text("寬" + sd.currStylePtr.strokeWidth + "點");
	}
	else if (paper.tool === sd.poserTool) {
		sd.cursorCircle = new paper.Raster("chosen_poser");

		//sd.cursorCircle.position = pos;
		sd.cursorCircle.position.x = pos.x - sd.cursorCircle.width / 2;
		sd.cursorCircle.position.y = pos.y + sd.cursorCircle.height / 2;
	}
	else if (paper.tool === sd.dropperTool) {
		sd.cursorCircle = new paper.Group();

		var rr = 85;
		var rw = 20;

		var path = new paper.Path.Arc([pos.x + rr, pos.y], [pos.x, pos.y - rr], [pos.x - rr, pos.y]);
		path.strokeColor = sd.penStyle.strokeColor;
		path.strokeWidth = rw;
		sd.cursorCircle.addChild(path);

		path = new paper.Path.Circle(pos, rr + rw);
		path.strokeColor = "lightgrey";
		path.strokeWidth = rw;
		sd.cursorCircle.addChild(path);

		path = new paper.Path.Arc([pos.x + rr, pos.y], [pos.x, pos.y + rr], [pos.x - rr, pos.y]);
		path.strokeColor = sd.prevColor;
		path.strokeWidth = rw;
		sd.cursorCircle.addChild(path);
	}
	sd.toolProject.view.draw();
}
function updateMousePosition(event) {
	sd.lastMouseEvent = event.event;
	sd.lastMousePos = event.point;

	if (sd.cursorCircle !== null) {
		if (paper.tool === sd.poserTool) {
			sd.cursorCircle.position.x = sd.lastMousePos.x - sd.cursorCircle.width / 2;
			sd.cursorCircle.position.y = sd.lastMousePos.y + sd.cursorCircle.height / 2;
		}
		else
			sd.cursorCircle.position = sd.lastMousePos;
	}
}
function updateDropper(is_first) {
	var hr = sd.scribbleProject.hitTest(sd.lastMousePos);
	var color = sd.penStyle.strokeColor;

	if (hr !== null) {
		if (hr.type === "segment" || hr.type === "stroke") {
			if (hr.item.blendMode !== "destination-out")
				color = hr.item.strokeColor.toCSS(true);
		}
		else if (hr.type === "pixel") {
			console.log("hr.color = " + hr.color + ".");
			color = hr.color;
		}
		// console.log("hr.type = " + hr.type + ".");
	}
	//$("#dropper_btn").css("color", color);
	if (is_first || color !== sd.penStyle.strokeColor)
		updateColor(color, true);
}
//////////
function scribbleDropper() {
	//console.log("scribbleDropper.");
	sd.dropperTool.activate();
	//updateCursor();

	sd.prevColor = sd.penStyle.strokeColor;
	updateDropper(true);

	$("#map_canvas").removeClass("cursor_pencil cursor_dropper cursor_eraser cursor_hand cursor_poser").addClass("cursor_dropper");
}
function mouseDownDropper(event) {
	if (event.event.which === 1) {
		//console.log("mouseDownDropper.");
		updateMousePosition(event);
		return false;
	}
}
function mouseUpDropper(event) {
	if (event.event.which === 1) {
		//console.log("mouseUpDropper.");
		//updateCursor();
		restoreTool();
		return false;
	}
}
function mouseDragDropper(event) {
	//console.log("mouseDragDropper. " + event.event.which);
	//if (event.event.which === 1 || event.event.which === 0/*on IE*/) {
	updateMousePosition(event);
	updateDropper(false);
	return false;
	//}
}
function mouseMoveDropper(event) {
	//console.log("mouseMoveDropper.");
	updateMousePosition(event);
	updateDropper(false);
}
/////////
function scribbleTipSize() {
	sd.tipSizeTool.activate();
	updateCursor();
}
function mouseDownTipSize(event) {
	if (event.event.which === 1) {
		updateMousePosition(event);
		sd.prevStrokeWidth = sd.currStylePtr.strokeWidth;

		var info_elt = $("#floating_info");
		info_elt.text("寬" + sd.currStylePtr.strokeWidth + "點").show();
		var elt_w = info_elt.width();

		info_elt.css("left", event.point.x - elt_w / 2 + "px").css("top", event.point.y + 20 + "px");
		return false;
	}
}
function mouseUpTipSize(event) {
	if (event.event.which === 1) {
		//updateCursor();
		$("#floating_info").hide();
		restoreTool();
		return false;
	}
}
function mouseDragTipSize(event) {
	if (event.point.x !== -1 || event.point.y !== -1) {
		var dist = new paper.Point(event.point.x - sd.lastMousePos.x, event.point.y - sd.lastMousePos.y);

		//console.log("Dist=" + dist + ", angle=" + dist.angle.toFixed(2) + ", length=" + dist.length.toFixed(2) + ".");
		//console.log("length=" + dist.length.toFixed(2) + ", reduced=" + Math.floor(dist.length / 5) + ".");
		var enlarge = dist.angle <= 135 && dist.angle >= -45;

		sd.currStylePtr.strokeWidth = sd.prevStrokeWidth + (enlarge ? 1 : -1) * Math.floor(dist.length / 5);
		limitStrokeWidth();

		updateCursor();
		return false;
	}
}
function mouseMoveTipSize(event) {
	updateMousePosition(event);
}
/////////
function scribbleHand() {
	sd.handTool.activate();

	updateCursor();
	$(".sel_passage").removeClass("sel_passage");
	$("#hand_btn").addClass("sel_passage");

	$("#map_canvas").removeClass("cursor_pencil cursor_dropper cursor_eraser cursor_hand cursor_poser").addClass("cursor_hand");
}
function mouseDownHand(event) {
	if (event.event.which === 1) {
		updateMousePosition(event);
		//sd.scribbleProject.activate();

		sd.prevViewCenter = sd.scribbleProject.view.center;
		return false;
	}
}
function mouseUpHand(event) {
	if (event.event.which === 1) {
		//restoreTool();
		sd.toolProject.view.center = sd.scribbleProject.view.center;
		return false;
	}
}
function mouseDragHand(event) {
	if (event.point.x !== -1 || event.point.y !== -1) {
		sd.scribbleProject.view.center = new paper.Point(
											sd.prevViewCenter.x - event.point.x + sd.lastMousePos.x,
											sd.prevViewCenter.y - event.point.y + sd.lastMousePos.y);

		return false;
	}
}
function mouseMoveHand(event) {
	updateMousePosition(event);
}
/////////
function scribblePen() {
	//console.log("scribblePen.");
	sd.scribbleTool.activate();

	sd.currBlendMode = "normal";
	sd.currStylePtr = sd.penStyle;

	updateCursor();
	$(".sel_passage").removeClass("sel_passage");
	$("#pen_btn").addClass("sel_passage");

	$("#map_canvas").removeClass("cursor_pencil cursor_dropper cursor_eraser cursor_hand cursor_poser").addClass("cursor_pencil");
}
function scribbleEraser() {
	sd.scribbleTool.activate();

	sd.currBlendMode = "destination-out";
	sd.currStylePtr = sd.eraserStyle;

	updateCursor();
	$(".sel_passage").removeClass("sel_passage");
	$("#eraser_btn").addClass("sel_passage");

	$("#map_canvas").removeClass("cursor_pencil cursor_dropper cursor_eraser cursor_hand cursor_poser").addClass("cursor_eraser");
}
function mouseDownScribble(event) {
	if (event.event.which === 1) {
		//event.point = paper.view.viewToProject(event.point);
		//console.log("Scribble left mouse down. " + event.point);
		updateMousePosition(event);

		//if (sd.currStylePtr.strokeWidth >= 2)
		paper.tool.minDistance = Math.max(sd.currStylePtr.strokeWidth / 2, 2);

		sd.scribbleProject.activate();
		sd.scribbleProject.currentStyle = sd.currStylePtr;

		sd.activePath = new paper.Path();
		//sd.activePath.strokeCap = "square";
		//sd.activePath.strokeJoin = "miter";

		sd.activePath.blendMode = sd.currBlendMode;
		sd.activePath.add(event.point);
		//sd.activePath.fullySelected = true;
		sd.redoArray.length = 0;
		//paper.view.draw();
		return false;
	}
}
//todo: a single point cannot be plot/erased.
function mouseUpScribble(event) {
	if (event.event.which === 1) {
		//console.log("Scribble left mouse up. " + event.point);
		if (sd.activePath !== null) {		// null happens when switching from dropper tool to scribble tool.
			//event.point = paper.view.viewToProject(event.point);
			paper.tool.minDistance = 2/*1*/;

			sd.lastPathInfo = printPath(sd.activePath, false);

			//sd.activePath.smooth();
			sd.activePath.simplify();
			sd.activePath = null;
			sd.scribbleProject.view.draw();
			return false;
		}
	}
}
function mouseDragScribble(event) {
	//console.log("Mouse drag: (" + event.point.x + ", " + event.point.y + ").");
	// when mouse goes out of window, drag event still fires with correct position.

	//event.point = paper.view.viewToProject(event.point);
	updateMousePosition(event);

	if (sd.activePath !== null) {
		sd.activePath.add(event.point);
		sd.scribbleProject.view.draw();
		return false;
	}
}
function mouseMoveScribble(event) {
	//console.log("Mouse move: (" + event.point.x + ", " + event.point.y + ").");
	// when mouse goes out of window, an event of (-1, -1) is fired.
	//event.point = paper.view.viewToProject(event.point);
	updateMousePosition(event);
}
//////////
function scribbleColor(event, on, cancel) {
	console.log("scribbleColor, on=" + on + ", cancel=" + cancel + ".");

	var elt = $("#color_pane");

	//if (elt.hasClass("not_show")) {
	if (on) {
		elt.removeClass("not_show");
		elt.position({
			my: "center center"/*"left top"*/,
			at: "right top",
			of: event,
			collision: "flipfit flipfit"
		});
		sd.prevColor = sd.penStyle.strokeColor;
		$("#clr_cancel_btn").css("background-color", sd.prevColor);
	}
	else {
		elt.addClass("not_show");

		if (cancel) {
			updateColor(sd.prevColor, true);
		}
	}
}
function scribbleUndo() {
	if (sd.scribbleProject.activeLayer.children.length !== 0) {
		var item = sd.scribbleProject.activeLayer.lastChild;
		item.remove();
		sd.redoArray.push(item);
		sd.scribbleProject.view.draw();
	}
}
function scribbleRedo() {
	if (sd.redoArray.length !== 0) {
		var item = sd.redoArray.pop();
		sd.scribbleProject.activeLayer.addChild(item);
		sd.scribbleProject.view.draw();
	}
}
//////////
function printPath(path, print_handle) {
	var info = "";
	for (var i = 0; i < path.segments.length; i++) {
		var s = path.segments[i];
		var text = "(" + s.point.x + "," + s.point.y + ")";

		if (print_handle) {
			text += "i:(" + s.handleIn.x.toFixed(2) + "," + s.handleIn.y.toFixed(2) + ")";
			text += "o:(" + s.handleOut.x.toFixed(2) + "," + s.handleOut.y.toFixed(2) + ")\n";
		}
		info += text;
	}
	return info;
}
function limitStrokeWidth() {
	if (sd.currStylePtr.strokeWidth > 50)
		sd.currStylePtr.strokeWidth = 50;
	else if (sd.currStylePtr.strokeWidth < 1)
		sd.currStylePtr.strokeWidth = 1;
}
function restoreTool() {
	if (sd.currStylePtr === sd.eraserStyle)
		scribbleEraser();
	else
		scribblePen();
}
function onKeyUp(keyname) {
	if (keyname == "I" || keyname == "i") {
		restoreTool();
	}
	else if (keyname == "C" || keyname == "c") {
		scribbleColor(sd.lastMouseEvent, false);
	}
	else if (keyname == "S" || keyname == "s") {
		$("#floating_info").hide();
		restoreTool();
	}
	else if (keyname == "Spacebar") {
		restoreTool();
	}
	else
		return false;
	return true;
}
//todo: don't allow changing tool while drawing a stroke.
function onKeyDown(keyname, repeat) {
	//console.log("onKeyDown (" + keyname + ").");
	if (keyname == "Z" || keyname == "z") {
		scribbleUndo();
	}
	else if (keyname == "Y" || keyname == "y") {
		scribbleRedo();
	}
	else if (keyname == "O" || keyname == "o") {
		scribblePoser();
	}
	else if (keyname == "P" || keyname == "p") {
		scribblePen();
	}
	else if (keyname == "E" || keyname == "e") {
		scribbleEraser();
	}
	else if (keyname == "H" || keyname == "h") {
		scribbleHand();
	}
	else if (keyname == "Spacebar") {
		if (!repeat)
			scribbleHand();
	}
	else if (keyname == "I" || keyname == "i") {
		if (!repeat)
			scribbleDropper();
	}
	else if (keyname == "C" || keyname == "c") {
		if (!repeat)
			scribbleColor(sd.lastMouseEvent, true);
	}
	else if (keyname == "S" || keyname == "s") {
		if (!repeat)
			scribbleTipSize();
	}
	else if (keyname == "F" || keyname == "f") {
		if (sd.scribbleProject.activeLayer.children.length !== 0) {
			var item = sd.scribbleProject.activeLayer.lastChild;

			if (sd.lastPathInfo !== null) {
				console.log("Before simplification: " + sd.lastPathInfo);
				console.log("After simplification: " + printPath(item, true));
				sd.lastPathInfo = null;
			}
			item.fullySelected = !item.fullySelected;
			sd.scribbleProject.view.draw();
		}
	}
	else if (keyname == "Add" || keyname == "+" || keyname == "=") {
		//if (sd.currStylePtr.strokeWidth < 50) {
		sd.currStylePtr.strokeWidth++;
		limitStrokeWidth();
		updateCursor();
		//}
	}
	else if (keyname == "Subtract" || keyname == "-") {
		//if (sd.currStylePtr.strokeWidth > 1) {
		sd.currStylePtr.strokeWidth--;
		limitStrokeWidth();
		updateCursor();
		//}
	}
	else
		return false;
	return true;
}
//Path: (1439,308)(1439,307)(1439,308)(1439,309)(1439,310)(1439,311)(1440,312)(1442,317)(1444,322)(1445,328)(1452,354)(1458,372)(1461,381)(1464,389)(1467,396)(1470,402)(1473,409)(1478,420)(1480,424)(1482,429)(1484,433)(1486,436)(1487,439)(1489,442)(1490,443)
//Path: (1439,308)i:(0.00,0.00)o:(0.00,-2.73)
//(1439,311)i:(-0.21,-0.42)o:(1.68,3.37)
//(1444,322)i:(-1.04,-3.82)o:(20.97,76.88)
//(1452,354)i:(-89.38,-302.53)o:(6.76,22.88)
//(1478,420)i:(-10.06,-21.55)o:(3.35,7.18)
//(1490,443)i:(-5.68,-5.68)o:(0.00,0.00)

function scribblePoser() {
	var $elt = $("#poser_chooser");
	$elt.click();
}
function onPoserChosen(event) {
	if (event.target.files.length === 1) {
		var type = event.target.files[0].type;

		if (type.substring(0, 6) === "image/") {
			var info = { org_blob: event.target.files[0] };

			shrinkImage(info);

			blob_store[info.org_blob_url] = info;

			sd.currBlobText = info.org_blob_url;

			$("#chosen_poser").attr("src", sd.currBlobText);
		}
	}
}
function onPoserLoad(event) {
	sd.poserTool.activate();

	updateCursor();
	$(".sel_passage").removeClass("sel_passage");
	$("#poser_btn").addClass("sel_passage");

	$("#map_canvas").removeClass("cursor_pencil cursor_dropper cursor_eraser cursor_hand cursor_poser").addClass("cursor_poser");
}
function mouseUpPoser(event) {
	if (event.event.which === 1) {
		console.log("Poser left mouse up. " + event.point);
		mouseUpHand(event);

		if (sd.scribbleProject.view.center.x == sd.prevViewCenter.x &&
			sd.scribbleProject.view.center.y == sd.prevViewCenter.y) {
			var foreword = "(" + event.point.x + "," + event.point.y + ")";		// After confirm(), event.point becomes (-1, -1).

			if (confirm("請按\"確定\"開始上傳貼圖，或按\"取消\"取消上傳。")) {
				restoreTool();
				//
				var flags = flagsAdd("", MT_COORDINATE, foreword);
				flags = flagsAdd(flags, MT_LETTER_TYPE, 1);

				var upload_files = new FormData();
				var key = prepareUploadFile(sd.currBlobText, upload_files);

				if (key) {
					var post_data = {
						creator: getUerNameOrDefault(), words: key, delta_flags: flags,
						upload_files: upload_files, board_id: getboardid(), discussion_id: getdiscussionid(),
					};
					postGoods("/createletter", post_data, function (suc, response) {
						if (suc && response.letter_id) {
							sd.currBlobText = null;
						}
						else {
							alert("抱歉，貼圖上傳失敗。");
						}
					});
				}
			}
		}
		return false;
	}
}