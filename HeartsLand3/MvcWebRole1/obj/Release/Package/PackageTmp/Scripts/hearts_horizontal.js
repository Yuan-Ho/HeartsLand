$(function () {
	if (checkNavigator()) {
		doWrap();

		populateNicknameList();

		pagePrepare();

		onReady(true);

		var $document = $(document);
		$document.mousedown(mouseDownHandler);
		$document.mouseup(mouseUpHandler);
		$document.mousemove(mouseMoveHandler);
		$document.mousemove(onMouseMoveMoonGate);
		$document.contextmenu(contextMenuHandler);

		onContentReady();
		setTimeout(resizeHandler, 0);
	}
});
function resizeHandler() {
	//console.log("resizeHandler().");
	adjustElementHeight();
	fitStoreyWidth(true);
	adjustSkyHeight();

	setTimeout(drawMoonGate, 0);		// The scrollbar may disappear after setting canvas size. So draw after scrollbar stablized.
}
function onContentReady() {
	doWrap();

	//adjustElementHeight();
	resizeHandler();
	//fitStoreyWidth();

	initialScroll();
}

var contextdrag = {
	state: 0,		// 0=inactive, 1=detecting, 2=dragging_x, 3=dragging_y.
	orig: { x: 0, y: 0 },
	sorig: { left: 0, top: 0 },
	mul: { up: 0, down: 0, left: 0, right: 0 }
};
function mouseDownHandler(event) {
	if (event.target.tagName === "TEXTAREA" || event.target.tagName === "INPUT")
		return;
	if (event.which == 3) {
		contextdrag.state = 1;
		contextdrag.orig.x = event.pageX;
		contextdrag.orig.y = event.pageY;

		var box = $("#contextdragbox");
		box.offset({ left: event.pageX - 20, top: event.pageY - 20 });
		box.css("border-style", "solid");
		box.css("visibility", "visible");

		//console.log("right mouse down. orig: (" + contextdrag.orig.x + ", " + contextdrag.orig.y + ").");
	}
	//else
	//	console.log("mouse down");
}
function mouseUpHandler(event) {
	if (event.which == 3) {
		console.log("right mouse up");
	}
	//else
	//	console.log("mouse up");
}
function mouseMoveHandler(event) {
	//console.log("mouse move. x=" + event.pageX + ", y=" + event.pageY + ".");

	if (contextdrag.state == 1) {
		var dist_x = Math.abs(event.pageX - contextdrag.orig.x);		// pageX/Y is relative to the <html> element.
		var dist_y = Math.abs(event.pageY - contextdrag.orig.y);

		//console.log("right mouse move. dist: (" + dist_x + ", " + dist_y + ").");

		if (dist_x >= 20 || dist_y >= 20) {
			if (dist_x >= 20) {
				contextdrag.state = 2;
			}
			else {
				contextdrag.state = 3;
			}
			var box = $("#contextdragbox");

			box.css("visibility", "collapse");
			box.offset({ left: 0, top: 0 });		// move to (0, 0) so that when page content width becomes smaller, the box won't block body width from shrinking.

			var w = $(window);
			var d = $(document);

			contextdrag.orig.x = event.clientX;		// clientX/Y is relative to the viewport.
			contextdrag.orig.y = event.clientY;

			contextdrag.sorig.left = w.scrollLeft();
			contextdrag.sorig.top = w.scrollTop();
			//

			contextdrag.mul.up = Math.max((contextdrag.sorig.top) / (event.clientY), 1);
			contextdrag.mul.down = Math.max((d.height() - w.height() - contextdrag.sorig.top) / (w.height() - event.clientY), 1);

			contextdrag.mul.left = Math.max((contextdrag.sorig.left) / (event.clientX), 1);
			contextdrag.mul.right = Math.max((d.width() - w.width() - contextdrag.sorig.left) / (w.width() - event.clientX), 1);

			//console.log(JSON.stringify(contextdrag.mul));
		}
		else if (dist_x >= 10)
			$("#contextdragbox").css(event.pageX > contextdrag.orig.x ? "border-right-style" : "border-left-style", "dashed");
		else if (dist_y >= 10)
			$("#contextdragbox").css(event.pageY > contextdrag.orig.y ? "border-bottom-style" : "border-top-style", "dashed");
	}
	else if (contextdrag.state == 2 || contextdrag.state == 3) {
		var w = $(window);
		//console.log("client: (" + event.clientX + ", " + event.clientY + "). window: (" + w.width() + ", " + w.height() + ").");

		//var d = $(document);
		//console.log("scroll: (" + w.scrollLeft() + ", " + w.scrollTop() + "). document: (" + d.width() + ", " + d.height() + ").");

		if (contextdrag.state == 2) {
			if (event.clientX > contextdrag.orig.x)
				w.scrollLeft(contextdrag.sorig.left + (event.clientX - contextdrag.orig.x) * contextdrag.mul.right);
			else
				w.scrollLeft(contextdrag.sorig.left + (event.clientX - contextdrag.orig.x) * contextdrag.mul.left);
		}
		else if (contextdrag.state == 3) {
			if (event.clientY > contextdrag.orig.y)
				w.scrollTop(contextdrag.sorig.top + (event.clientY - contextdrag.orig.y) * contextdrag.mul.down);
			else
				w.scrollTop(contextdrag.sorig.top + (event.clientY - contextdrag.orig.y) * contextdrag.mul.up);
		}
	}
}
function releaseContextDrag() {
	if (contextdrag.state == 1) {
		contextdrag.state = 0;

		var box = $("#contextdragbox");

		box.css("visibility", "collapse");
		box.offset({ left: 0, top: 0 });		// move to (0, 0) so that when page content width becomes smaller, the box won't block body width from shrinking.
	}
	else if (contextdrag.state != 0) {
		contextdrag.state = 0;
		return false;
	}
}
function contextMenuHandler(event) {
	// the event is eaten when click inside google map.
	console.log("context menu");
	return releaseContextDrag();
}
function onKeyDown(keyname) {
	if (keyname == "Home") {
		window.scrollBy(document.documentElement.scrollWidth, 0);
		return true;
	}
	else if (keyname == "End") {
		window.scrollBy(-document.documentElement.scrollWidth, 0);
		return true;
	}
	else if (keyname == "PageUp") {
		window.scrollBy(window.innerWidth * 0.6, 0);
		return true;
	}
	else if (keyname == "PageDown") {
		window.scrollBy(-window.innerWidth * 0.6, 0);
		return true;
	}
}
var moongate = {
	image: new Image(),
	image_ready: false,
	centerX: 0,
	centerY: 0,
	radius: 0,

	is_inside: true,
	pattern: null,
};

moongate.image.onload = function (e) {
	moongate.image_ready = true;
	setTimeout(drawMoonGate, 0);
};
moongate.image.src = "/Images/maze_gray.gif";

function drawMoonGate() {
	//console.log("drawMoonGate(). retry_times=" + retry_times + ".");

	var w = $(window);
	var canvas_elt = document.getElementById("moongate_canvas");

	canvas_elt.width = w.width();
	canvas_elt.height = w.height();

	var ctx = canvas_elt.getContext("2d");

	moongate.centerX = canvas_elt.width / 2;
	moongate.centerY = canvas_elt.height / 2;
	moongate.radius = Math.max(canvas_elt.width, canvas_elt.height) * 0.5 * 0.95;

	ctx.save();
	ctx.lineWidth = 2;
	ctx.strokeStyle = "rgba(128,128,128,1)";
	ctx.shadowColor = "black";
	ctx.shadowBlur = 7;

	ctx.beginPath();

	ctx.arc(moongate.centerX, moongate.centerY, moongate.radius, 0, Math.PI * 2, true);
	ctx.stroke();
	ctx.restore();
	//
	ctx.beginPath();
	ctx.rect(0, 0, canvas_elt.width, canvas_elt.height);

	ctx.arc(moongate.centerX, moongate.centerY, moongate.radius, 0, Math.PI * 2, true);
	ctx.clip();
	ctx.globalAlpha = moongate.is_inside ? 0.8 : 0.5;

	if (moongate.image_ready) {
		if (moongate.pattern == null)
			moongate.pattern = ctx.createPattern(moongate.image, "repeat");
		ctx.fillStyle = moongate.pattern;
	}
	else {
		ctx.fillStyle = "rgba(128,128,128,1)";
	}
	ctx.fillRect(0, 0, canvas_elt.width, canvas_elt.height);
}
function onMouseMoveMoonGate(event) {
	//console.log("mouse move. x=" + event.clientX + ", y=" + event.clientY + ".");
	
	var distX = moongate.centerX - event.clientX;
	var distY = moongate.centerY - event.clientY;

	var square = distX * distX + distY * distY;

	if (square > moongate.radius * moongate.radius) {
		if (moongate.is_inside) {
			moongate.is_inside = false;
			drawMoonGate();
		}
	}
	else
		if (!moongate.is_inside) {
			moongate.is_inside = true;
			drawMoonGate();
		}
}