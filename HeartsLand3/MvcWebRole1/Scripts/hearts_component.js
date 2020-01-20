var blob_store = {};

function createGeneralDialog(params) {
	var html = "<div title='" + params.title + "'>"
					+ params.message
					+ "<p class='error_info'>失敗。錯誤訊息：\"<span></span>\"</p>";

	if (params.has_reason)
		html += "<label class='wizard'>理由：</label>"
				+ "<textarea cols='50' rows='5' maxlength='500' placeholder='請輸入理由，或留空白。'></textarea>";

	html += "</div>";
	var elt = $(html);

	elt.dialog({
		height: 350,
		width: 500,
		modal: true,
		buttons: {
			"確定": function (event) {
				$(event.currentTarget).prop("disabled", true);		// event.target would be the span.

				params.callback();
			},
			"取消": function () {
				elt.dialog("close");
			}
		}
	});
	return elt;
}
function modifyGeneralDialog(elt, params) {
	if (params.err_msg != undefined) {
		elt.find(".error_info span").text(params.err_msg).parent().show();
	}
}
function showHelpDialog(event) {
	var elt = $("#help_dialog");
	elt.dialog({
		height: 350,
		width: "40em",
		modal: false,
		position: {
			my: "right bottom",
			at: "right top",
			of: event
		},
	});
}
function hideHelpDialog(event) {
	var $target = $(event.target);

	if (!$target.closest(".ui-dialog:has(#help_dialog)").length) {
		var elt = $(".ui-dialog #help_dialog");		// before initialization, the #help_dialog is not contained inside .ui-dialog and elt.length is 0.
		elt.dialog("close");
	}
}
function showCaptchaDialog(callback, params) {
	var elt = $("#captcha_dialog");

	var wait_seconds = 0;
	var show_captcha = true;

	if (params.wait_seconds != undefined)
		if (params.wait_seconds <= 60)
			wait_seconds = params.wait_seconds;

	if (wait_seconds === 0) {
		elt.find("p.dlg_msg").text("為了避免機器自動快速灌水，請幫忙輸入驗證碼，驗證碼可以區分機器或真人。");
	}
	else {
		if (wait_seconds <= 10) {
			elt.find("p.dlg_msg").html("為了避免快速灌水，請稍待<span>" + wait_seconds + "</span>秒後再重試。");
			show_captcha = false;
		}
		else {
			elt.find("p.dlg_msg").html("為了避免機器自動快速灌水，請幫忙輸入驗證碼，驗證碼可以區分機器或真人。<br><br>"
										+ "或者稍待<span>" + wait_seconds + "</span>秒後即可不輸入驗證碼重試。");
		}
		var expire_time = Date.now() + wait_seconds * 1000;

		var handle = setInterval(function () {
			var remaining_seconds = Math.round((expire_time - Date.now()) / 1000);

			if (remaining_seconds <= 0) {
				elt.find("p.dlg_msg > span").text("0");
				clearInterval(handle);
				handle = null;
			}
			else
				elt.find("p.dlg_msg > span").text(remaining_seconds.toFixed(0));
		}, 1000);
	}
	var is_ok = false;

	elt.dialog({
		height: 350,
		width: 500,
		modal: true,
		buttons: {
			"確定": function () {
				is_ok = true;
				$(this).dialog("close");
			},
			"取消": function () {
				$(this).dialog("close");
			}
		},
		close: function () {
			if (handle)
				clearInterval(handle);
			callback(is_ok);
		}
	});
	if (show_captcha)
		Recaptcha.create(ReCaptchaPublicKey, "recaptcha_dlg",
							{
								theme: "clean",
								callback: Recaptcha.focus_response_field
							});
	else
		Recaptcha.destroy();
}
function captchaLoaded() {
	fitStoreyWidth();
	Recaptcha.focus_response_field();
}
function wizardNextStep(cur_step) {
	var next_step = cur_step + 1;

	$("#step" + cur_step + " .room_body").addClass("paststep");

	$("#step" + cur_step + " input:text").each(function () {
		this.setSelectionRange(0, 0);
	});
	$("#step" + cur_step + " input").prop("disabled", true).addClass("paststep");

	var elt = $("#step" + next_step).css("visibility", "visible");
	$(window).scrollLeft(elt.offset().left);
}
function changeColor(elt, from_color, to_color) {
	var names = ["color", "background-color",
				"border-top-color", "border-bottom-color", "border-left-color", "border-right-color"];
	var changed = false;

	for (var i = 0, len = names.length; i < len; i++)
		if (elt.css(names[i]) === from_color) {
			elt.css(names[i], to_color);
			changed = true;
		}
	//if (changed)
	//	console.log("Changed color of " + elt.length + " element - " + elt[0].tagName);
}
function changeChildrenColor(elt, from_color, to_color) {
	elt.find("*").each(function (idx, elt) {
		changeColor($(elt), from_color, to_color);
	});
}
function shrinkImageInternal(info, image, max_width, max_height, prop_name, type) {
	var getBlobURL = (window.URL && URL.createObjectURL.bind(URL)) ||
		(window.webkitURL && webkitURL.createObjectURL.bind(webkitURL)) ||
		window.createObjectURL;

	var canvas = document.createElement("canvas");

	var ratio = Math.min(max_width / image.width, max_height / image.height, 1);
	var width = Math.round(image.width * ratio);
	var height = Math.round(image.height * ratio);

	canvas.width = width;
	canvas.height = height;
	canvas.getContext("2d").drawImage(image, 0, 0, width, height);

	// data:image/jpeg;base64,/9j/4AAQSkZJRgABAQEAAAAAAAD/2wBDAAYEBQYFBAYGBQYHBwYIChAKCgkJChQODwwQFxQYGBcUFhYaHSUfGhsjHBYWICwgIyYnKS...
	var data_url = canvas.toDataURL(type, 0.8);

	info[prop_name] = dataURLtoBlob(data_url, type);
	info[prop_name + "_url"] = getBlobURL(info[prop_name]);
}
function shrinkImage(info) {
	var getBlobURL = (window.URL && URL.createObjectURL.bind(URL)) ||
		(window.webkitURL && webkitURL.createObjectURL.bind(webkitURL)) ||
		window.createObjectURL;

	info.org_blob_url = getBlobURL(info.org_blob);

	var threshold = (info.org_blob.type === "image/gif" ? 500 : 250) * 1024;

	if (info.org_blob.size >= 30 * 1024) {
		var image = document.createElement("img");

		image.onload = function () {
			var type = info.org_blob.type === "image/png" ? "image/png" : "image/jpeg";

			if (info.org_blob.size >= threshold) {
				shrinkImageInternal(info, image, 1200, 900, "shrunk_blob", type);
				if (info.shrunk_blob.size > info.org_blob.size) {
					delete info.shrunk_blob;
					delete info.shrunk_blob_url;
				}
			}
			if (!isViewScribble()) {
				shrinkImageInternal(info, image, 300, 300, "thumbnail_blob", type);
				if (info.thumbnail_blob.size > info.org_blob.size) {
					delete info.thumbnail_blob;
					delete info.thumbnail_blob_url;
				}
			}
		};
		image.src = info.org_blob_url;
	}
}
function prepareUploadFile(blob_url, upload_files) {
	var info = blob_store[blob_url];
	if (info) {
		var key = randomAlphaNumericString(10);
		//var ext = fileExtension(file.name);
		//var filename = randomAlphaNumericString(8) + ext;

		//upload_files.append(key/*match_text*//*filename*/, info.shrunk_blob ? info.shrunk_blob : info.org_blob);

		if (!info.thumbnail_blob) {
			if (info.shrunk_blob)
				upload_files.append("n6/" + key, info.shrunk_blob);
			else
				upload_files.append("n1/" + key, info.org_blob);
		}
		else {
		    if (info.shrunk_blob) {
		        upload_files.append("n5/" + key, info.shrunk_blob);
		        upload_files.append("n4/" + key, info.thumbnail_blob);
		    }
		    else {
		        upload_files.append("n3/" + key, info.org_blob);
		        upload_files.append("n2/" + key, info.thumbnail_blob);
		    }
		}
		//return "blob:" + filename;
		return key;
	}
	return null;
}
function makeLetterEditor(editor_id, insert_input_pt, insert_option_pt, params) {
	if (g_no_tbrl) {
		params.no_preview = true;
		params.simplified_option = true;
	}
	var onContentChanged = function () {
		//console.log("letter editor onContentChanged().");
		if (isViewSquare()) {
			var $wizard = editor_elt.parent();		// to include preview_room_elts and preview_banner_elt.
			fitStoreyWidthElement($wizard);
			if (typeof onEditorContentChanged === "function") {
				setTimeout(onEditorContentChanged, 0, $wizard);		// wait for width() to become correct.
			}
		}
		else
			fitStoreyWidth();
	};
	var changeNumberOfRoomTable = function (cnt, sn_base, insert_before, creator) {
		if (preview_room_elts.length < cnt)
			for (var i = preview_room_elts.length; i < cnt; i++) {
				var preview_room_elt = makeRoomTable({
					//id: id_prefix + i,
					serial_num: i + sn_base,
					create_time: new Date(),
					creator: creator,
					show_edit: params.is_edit,
					insert_before: insert_before,
					addi_cls: "t_wizard",
				});
				changeChildrenColor(preview_room_elt, "rgb(0, 128, 0)"/*green*/, "indigo");

				// jquery array is always in document order. may not match need.
				var arr = preview_room_elts.toArray();
				arr.push(preview_room_elt[0]);
				preview_room_elts = $(arr);

				//preview_room_elts = preview_room_elts.add(preview_room_elt);
			}
		else if (preview_room_elts.length > cnt) {
			for (var i = cnt; i < preview_room_elts.length; i++) {
				preview_room_elts.eq(i).remove();
			}
			preview_room_elts = preview_room_elts.slice(0, cnt);
		}
	};
	if (!params.no_preview) {
		var preview_banner_elt = makeRoomBanner({
			//id: "preview_banner",
			words: params.have_heading ? "預覽標題將顯示在此處" : "留言預覽",
			insert_before: editor_id
		});

		var preview_room_elts = makeRoomTable({
			//id: "preview_room_0",
			serial_num: 1,
			create_time: new Date(),
			show_edit: params.is_edit,
			insert_before: editor_id,
			addi_cls: "t_wizard",
		});
	}
	else {
		var preview_banner_elt = makeDummyInsert({
			insert_before: editor_id
		});
	}
	if (!params.simplified_option) {
		var preview_on_elt = makeOptionCheckBox({
			//id: "preview_on",
			text: "即時預覽",
			checked: true,
			insert_before: insert_option_pt
		});
		var vertical_text_on_elt = makeOptionCheckBox({
			//id: "vertical_text_on",
			text: "直排",
			checked: true,
			insert_before: insert_option_pt
		}).on("change", function (event) {
			var elt = $(event.target);
			var checked = elt.prop("checked");

			if (!params.no_preview) {
				modifyRoomTable(preview_room_elts/*$("[id^='preview_room_']")*/, {
					css_cls: (checked && !g_no_tbrl) ? "tbrl" : "hori_tb",
				});
				onContentChanged();
			}
		});
		var insider_only_elt = makeOptionCheckBox({
			text: "不公開",
			checked: false,
			insert_before: insert_option_pt
		});
	}
	var creator_elt = makePlainInput({
		//id: "creator",
		name: "暱稱",
		max_len: CREATOR_FIELD_MAX_LEN,
		min_len: CREATOR_FIELD_MIN_LEN,
		size: 15,
		default_value: lastUsedNickname()/*"遊民"*/,
		validate: "user_name",
		list: "remembered_nn",
		insert_before: insert_input_pt
	}).on("input", function (event) {
		setTimeout(function () {
			var new_params = {};
			if (params.is_edit)
				new_params.editor = $(event.target).val();
			else
				new_params.creator = $(event.target).val();

			if (!params.no_preview) {
				modifyRoomTable(preview_room_elts/*$("[id^='preview_room_']")*/, new_params);
				onContentChanged();
			}
		}, 0);
	});
	if (params.have_reply_to) {
		var reply_to_sn_elt = makePlainInput({
			name: "回應",
			max_len: 4,
			min_len: 0,
			size: 8,
			no_place_holder: true,
			validate: "number",
			insert_before: insert_input_pt
		}).on("input", function (event) {
		});
	}
	if (params.have_heading) {
		var heading_elt = makePlainInput({
			//id: "heading",
			name: "標題",
			max_len: HEADING_FIELD_MAX_LEN,
			min_len: HEADING_FIELD_MIN_LEN,
			size: params.big ? 65 : 43,
			default_value: "",
			insert_before: insert_input_pt
		}).on("input", function (event) {
			setTimeout(function () {
				var text = $(event.target).val();
				if (text === "")
					text = "預覽標題將顯示在此處";

				if (!params.no_preview) {
					modifyRoomBanner(preview_banner_elt, { words: text });
					onContentChanged();
				}
			}, 0);
		});
	}
	makeImageChooser({
		insert_before: insert_option_pt
	}).on("change", function (event) {
		// If a picture is posted(uploaded) in a letter and selected again, the "change" event will not fire because there is no change.
		// when user cancels, the event fires with 0 this.files.length.

		var blob_text = "";

		for (var i = 0; i < this.files.length; i++) {
			var type = this.files[i].type;
			if (type.substring(0, 6) !== "image/")
				continue;

			var info = { org_blob: this.files[i] };

			shrinkImage(info);

			//var blob_url = getBlobURL(this.files[i]);
			//blob_store[blob_url] = this.files[i];
			blob_store[info.org_blob_url] = info;

			//blob_text += "\n" + info.org_blob_url + "\n";
			blob_text = "\n" + info.org_blob_url + "\n" + blob_text;
		}
		if (blob_text.length > 0) {
			var text = words_elt.val();

			var ss = words_elt[0].selectionStart;		// it is 5 on IE for empty textarea.
			var se = words_elt[0].selectionEnd;

			var before = text.substring(0, ss);
			var after = text.substring(se);

			words_elt.val(before + blob_text + after);

			words_elt[0].selectionStart = ss + 1;		// skip "\n".
			words_elt[0].selectionEnd = ss + blob_text.length - 1;		// skip "\n".

			words_elt.focus();
			setTimeout(function () { words_elt.trigger("input"); }, 250);		// wait for img load.
		}
	});
	var words_elt = makeWordsTextArea({
		//id: "words",
		cols: params.big ? 70 : 50,
		rows: params.big ? 12 : 10,
		max_len: params.can_split ? WORDS_FIELD_INPUT_LIMIT : (params.is_edit ? WORDS_FIELD_EDIT_MAX_LEN : WORDS_FIELD_MAX_LEN),
		insert_before: insert_input_pt
	}).on("input", function (event) {
		setTimeout(function () {
			var input_elt = $(event.target);
			var text = input_elt.val();

			if (text === "")
				text = "預覽內容將顯示在此處。";

			if (params.can_split)
				var split_arr = splitLongWords(text);

			updateTextAreaInfo(input_elt, params.can_split ? split_arr.length : 1);

			if (!params.no_preview && preview_on_elt.prop("checked")) {
				if (params.can_split) {
					changeNumberOfRoomTable(
											split_arr.length,
											params.get_new_sn(),
											editor_id,
											creator_elt.val());

					for (var i = 0; i < split_arr.length; i++) {
						modifyRoomTable(preview_room_elts.eq(i)/*$("#preview_room_" + i)*/, {
							words: split_arr[i]
						});
					}
				}
				else
					modifyRoomTable(preview_room_elts.first(), { words: text });

				adjustElementHeight();
				onContentChanged();
			}
		}, 0);
	});
	if (!params.simplified_option) {
		var ewr_radio_elts = makeOptionRadio(
			{
				options: ["英文字預設排列", "英文字立正直排", "英文字轉45度", "英文字轉90度"],
				values: ["0", "1", "3", "4"],
				check_idx: 0,
				insert_before: insert_option_pt
			}).on("change", function (event) {
				var elt = $(event.target);
				var checked = elt.prop("checked");

				if (checked) {
					var val = elt.val();
					var ewr_css_cls = val === "0" ? "" : "tbrl_ewr" + val;

					if (!params.no_preview) {
						modifyRoomTable(preview_room_elts, { ewr_css_cls: ewr_css_cls });
						modifyRoomBanner(preview_banner_elt, { ewr_css_cls: ewr_css_cls });
						onContentChanged();
					}
				}
			});
	}
	var editor_elt = $("#" + editor_id);

	if (!params.no_preview) {
		changeChildrenColor(preview_banner_elt, "rgb(0, 128, 0)"/*green*/, "indigo");
		changeChildrenColor(preview_room_elts, "rgb(0, 128, 0)"/*green*/, "indigo");
	}
	//changeChildrenColor(editor_elt, "rgb(0, 128, 0)"/*green*/, "indigo");

	return {
		check_input: function () {
			return (!params.have_heading || checkPlainInput(heading_elt)) &&
					checkPlainInput(creator_elt) &&
					(!params.have_reply_to || checkPlainInput(reply_to_sn_elt)) &&
					words_elt.val().length > 0;
		},
		clear_input: function () {
			//creator_elt.val("");
			//creator_elt.trigger("input");

			words_elt.val("");
			words_elt.trigger("input");		// reset the preview.

			if (params.have_heading)
				heading_elt.val("");
			if (params.have_reply_to)
				reply_to_sn_elt.val("");
		},
		post_data: function () {
			var creator = creator_elt.val();
			var words = words_elt.val();
			var foreword = words_elt.data("foreword");
			var flags = "";

			words = $.trim(words);
			if (foreword)
				flags = flagsAdd(flags, MT_COORDINATE, foreword);
				// words = foreword + words;

			var heading = params.have_heading ? heading_elt.val() : undefined;

			if (params.simplified_option) {
				var heading_flags = "";
			}
			else {
				// layout: 0=absence=default=vertical, 2=horizontal, 1/3/4=vertical English 0/45/90 degrees.
				var val = ewr_radio_elts.filter(":checked").val();

				var heading_flags = params.have_heading ? flagsAdd("", MT_LAYOUT, val) : undefined;

				if (!vertical_text_on_elt.prop("checked"))
					val = 2;

				flags = flagsAdd(flags, MT_LAYOUT, val);

				if (insider_only_elt.prop("checked"))
					flags = flagsAdd(flags, MT_AUTHORIZATION, 2);

				if (params.have_reply_to) {
					val = reply_to_sn_elt.val();
					var reply_to_sn = Number(val);		// ""->0, "0"->0, "a"->NaN.
					if (!isNaN(reply_to_sn))
						flags = flagsAdd(flags, MT_REPLY_TO, reply_to_sn);
				}
			}
			var upload_files = new FormData();
			var upload = false;

			words = words.replace(/^blob\:[^'"<>\s]+$/igm, function (match_text) {
				var key = prepareUploadFile(match_text, upload_files);
				if (key) {
					upload = true;
					return key;
				}
				return match_text;
			});

			return {
				creator: creator,
				words: words,
				heading: heading,
				delta_flags: flags,
				upload_files: upload ? upload_files : undefined,
				heading_delta_flags: heading_flags,
			};
		},
		showCreate: function (target_elt, reply_to_sn) {
			if (!params.no_preview) {
				modifyRoomTable(preview_room_elts.first(), {
					serial_num: params.get_new_sn(),
					create_time: new Date(),
				});

				if (isViewSquare())
					preview_banner_elt.hide();
				else
					preview_banner_elt.show();
				//preview_room_elts.show();
			}

			if (target_elt) {
				reply_to_sn_elt.val(reply_to_sn);
				//editor_elt.show();
				//fitStoreyWidth();

				this.move(target_elt);
			}
			else {
				reply_to_sn_elt.val("");
				this.move(preview_banner_elt);
			}
		},
		hide: function () {
			if (!params.no_preview) {
				preview_banner_elt.hide();
				preview_room_elts/*$("[id^='preview_room_']")*/.hide();
			}
			editor_elt.hide();

			fitStoreyWidth();
		},
		focus: function () {
			heading_elt.focus();
		},
		move: function (insert_point) {
			editor_elt.find(".error_info > span").text("").parent().hide();		// 會找到2個，一個是名字input下的check error info，一個是內容text area下的post error info。

			if (!isViewSquare()) {
				if (g_reverse_insert) {
					insert_point.before(editor_elt);
					if (!params.no_preview)
						insert_point.before(preview_room_elts);
				}
				else {
					insert_point.after(editor_elt);
					if (!params.no_preview)
						insert_point.after(preview_room_elts);
				}
			}
			editor_elt.show();
			if (!params.no_preview)
				preview_room_elts.show();

			//console.log("letter editor move().");
			fitStoreyWidth();

			if (!isViewSquare())
				editor_elt[0].scrollIntoView();
		},
		showEdit: function (target_elt) {
			var tgt_params = getRoomParams(target_elt);

			if (!params.no_preview) {
				preview_banner_elt.hide();

				modifyRoomTable(preview_room_elts.first()/*$("#preview_room_0")*/, {
					serial_num: tgt_params.serial_num,
					creator: tgt_params.creator,
					create_time: tgt_params.create_time,
					editor: creator_elt.val(),
					edit_time: new Date(),
				});
			}
			var ewr_value = "0";
			// tgt_params.ewr_css_cls is undefined for horizontal, "" for default.
			var idx = tgt_params.ewr_css_cls ? tgt_params.ewr_css_cls.indexOf("tbrl_ewr") : -1;
			if (idx === 0)
				ewr_value = tgt_params.ewr_css_cls.substr(idx + 8);

			this.fillEditor(tgt_params.words,
				tgt_params.css_cls !== "hori_tb",		// in case tgt_params.css_cls is undefined.
				tgt_params.addi_cls.indexOf("insider_only") !== -1,
				ewr_value,
				tgt_params.reply_to_sn ? tgt_params.reply_to_sn : "");

			$("#" + insert_option_pt).parent().children().slice(2, 8).toggle(tgt_params.serial_num !== 0);
			reply_to_sn_elt.parent().toggle(tgt_params.serial_num !== 0);

			this.move(target_elt);
		},
		fillEditor: function (words, vertical_text_on, insider_only, ewr_value, reply_to_sn) {
			if (!params.simplified_option) {
				modifyOptionCheckBox(vertical_text_on_elt, vertical_text_on);
				modifyOptionCheckBox(insider_only_elt, insider_only);
				modifyOptionRadio(ewr_radio_elts, ewr_value);
			}
			words_elt.val(words);
			words_elt.trigger("input");

			reply_to_sn_elt.val(reply_to_sn);
		}
	};
}
function dataURLtoBlob(data_url, type) {
	var pos = data_url.indexOf(",");

	var bin_str = atob(data_url.substr(pos + 1));

	var ab = new ArrayBuffer(bin_str.length);
	var view = new Uint8Array(ab);

	for (var i = 0; i < view.length; i++) {
		view[i] = bin_str.charCodeAt(i);
	}
	return new Blob([ab], { type: type/*"image/jpeg"*/ });
	//var bb = new BlobBuilder();
	//bb.append(ab);
	//return bb.getBlob("image/jpeg");
};
function makeBoardNameInput(insert_input_pt) {
	var input_elt = makePlainInput({
		name: "留言板名",
		max_len: BOARD_NAME_FIELD_MAX_LEN,
		min_len: BOARD_NAME_FIELD_MIN_LEN,
		size: 24,
		default_value: "",
		validate: "user_name",
		insert_before: insert_input_pt
	}).on("input", function () {
		setTimeout(function () {
			preview_elt.text(input_elt.val() + "板");
		}, 0);
	});
	var preview_elt = makeTextLabel({
		name: "預覽板名：",
		insert_before: insert_input_pt
	});
	return {
		check_input: function () {
			return checkPlainInput(input_elt);
		},
		val: function (new_value) {
			if (new_value !== undefined) {
				input_elt.val(new_value);
				input_elt.trigger("input");
			}
			else
				return input_elt.val();
		},
		focus: function () {
			input_elt.focus();
		}
	};
}
function askCaptchaAndPostGoodsAndShowResult(btn, url, post_data, wait_seconds) {
	var btn_elt = $(btn);
	var status_info = findInfoElement(btn_elt, 1, 1, ".status_info");

	btn_elt.prop("disabled", true);
	status_info.removeClass("is_error").text("正在連絡伺服器…");

	askCaptchaAndPostGoods(url, post_data,
		function (suc, response) {
			if (suc && response.ok) {
				status_info.removeClass("is_error").text("結果：成功。");
			}
			else {
				status_info.addClass("is_error").text("結果：失敗。錯誤訊息：" + response);

				setTimeout(function () {
					btn_elt.prop("disabled", false);
				}, 1000);
			}
		}, wait_seconds);
}
function postLetter(url, post_data, btn, callback) {
	post_data.board_id = getboardid();
	post_data.discussion_id = getdiscussionid();

	buttonWithInfoBegin(btn);

	postGoods(url, post_data, function (suc, response) {
		if (suc && response.letter_id) {
			if (isViewSquare())
				var hash = "#g_" + response.letter_id;
			else
				var hash = "#" + response.letter_id;

			refreshDiscussionLoad(hash, true);

			//location.hash = hash;
			//location.reload();
			buttonWithInfoFinish(btn);
			callback(true);
		}
		else {
			buttonWithInfoFinish(btn, response);
			callback(false);
		}
	});
}
function doEditLetter(btn) {
	if (edit_wizard_elt.check_input()) {
		var post_data = edit_wizard_elt.post_data();

		post_data.letter_id = editing_letter_id;
		rememberNickname(post_data.creator);

		postLetter("/editletter", post_data, btn, function (suc) {
		});
	}
}
function doCreateLetter(btn) {
	// the autocomplete="off" is only for firefox.
	// see https://github.com/twbs/bootstrap/issues/793.

	if (create_wizard_elt.check_input()) {
		var post_data = create_wizard_elt.post_data();

		rememberNickname(post_data.creator);

		postLetter("/createletter", post_data, btn, function (suc) {
			if (suc)
				create_wizard_elt.clear_input();
		});
	}
}
function doCreateDiscussion(btn, view_type/*0=horizontal,2=map,3=sky,4=scribble*/) {
	var board_id = getboardid();

	if (create_wizard_elt.check_input()) {
		buttonWithInfoBegin(btn);

		var post_data = create_wizard_elt.post_data();

		if (view_type !== 0)
			post_data.heading_delta_flags = flagsAdd(post_data.heading_delta_flags, MT_VIEW, view_type);
		post_data.board_id = board_id;
		rememberNickname(post_data.creator);

		postGoods("/creatediscussion", post_data, function (suc, response) {
			if (suc && response.discussion_id) {
				var link = '/' + board_id + '/' + response.discussion_id;
				if (view_type === 2)
					link += "?view=map";
				else if (view_type === 3)
					link += "?view=sky";
				else if (view_type === 4)
					link += "?view=scb";
				location.assign(link);
			}
			else {
				buttonWithInfoFinish(btn, response);
			}
		});
	}
}
function buttonWithInfoBegin(btn) {
	var $btn = $(btn);
	var err_info = findInfoElement($btn, 1, -1, ".error_info");
	var err_msg = err_info.children("span");

	$btn.prop("disabled", true);
	err_info.hide();
	err_msg.text("");
}
function buttonWithInfoFinish(btn, msg/*may be empty string*/) {
	var $btn = $(btn);
	var err_info = findInfoElement($btn, 1, -1, ".error_info");
	var err_msg = err_info.children("span");

	if (typeof msg !== "undefined") {
		err_msg.text(msg);
		err_info.show();
	}
	setTimeout(function () {
		$btn.prop("disabled", false);
	}, 1000);
}
function updateRoomMenu() {
	if (isDiscussion()) {
		var cls_id = isViewMap() ? "div.sticker" : "table.room";

		var target_elt = $(g_room_menu_target).closest(cls_id);
		var letter_id = target_elt.attr("id");

		if (!isLetterId(letter_id))
			return false;

		updateRoomMenuInternal(target_elt, letter_id);
	}
	return true;
}
function updateRoomMenuInternal(target_elt, letter_id) {
	var is_delete_remark = target_elt.hasClass("delete_remark");
	if (is_delete_remark) {
		$("#board_owner_menu li").addClass("ui-state-disabled");
		$("#discussion_creator_menu li").addClass("ui-state-disabled");
		$("#letter_creator_menu li").addClass("ui-state-disabled");
		$("#lau_letter_creator_menu li").addClass("ui-state-disabled");
		$("#not_letter_creator_menu li").addClass("ui-state-disabled");
	}
	else {
		var sn = letterIdToSerialNumber(letter_id);

		$("#board_owner_menu li").toggleClass("ui-state-disabled", !isBoardOwner() && !isMenuAllOn());
		$("#discussion_creator_menu li").toggleClass("ui-state-disabled", !isDiscussionCreator());
		$("#letter_creator_menu li").toggleClass("ui-state-disabled", !isLetterCreator(letter_id));
		$("#lau_letter_creator_menu li").toggleClass("ui-state-disabled", !isLauAndLetter(letter_id));

		$("#reply_to_menu").toggle(sn > 0);

		var undelete = target_elt.hasClass("deleted");
		var cmd_name = undelete ? "復原" : "刪除";

		var reported = target_elt.hasClass("reported");
		var report_cmd_name = reported ? "取消檢舉" : "檢舉";

		if (sn > 0) {
			$("li[data-action='delete'] > a").text(cmd_name + "留言");
			$("li[data-action='report'] > a").text(report_cmd_name + "留言");

			$("li[data-action='edit'] > a").text("編輯留言");

			//if (undelete)
			//	$("li[data-auth='nbod']").addClass("ui-state-disabled");
			// permanent_delete a deleted letter is allowed.
		}
		else {
			$("li[data-action='delete'] > a").text(cmd_name + "討論串");
			$("li[data-action='report'] > a").text(report_cmd_name + "討論串");

			$("li[data-action='edit'] > a").text("編輯標題");

			$("li[data-auth='nbod']").addClass("ui-state-disabled");
			$("li[data-action='permanent_delete']").addClass("ui-state-disabled");
		}
		if (undelete)
			$("li[data-action='edit']").addClass("ui-state-disabled");

		$("li[data-action='report']").toggleClass("ui-state-disabled", undelete || (reported && !isBoardOwner())/* || sn === 0*/);
		$("li[data-action='prosecute']").toggleClass("ui-state-disabled", undelete || sn === 0);
	}
}
function isDiscussionCreator() {
	return isLetterCreator("e0");
}
function isLetterCreator(letter_id) {
	var user_id = getUserId();
	if (user_id)		// if not logged in, user_id is NaN. falsy.
	{
		var elt = $("#" + letter_id);
		var params = getRoomParams(elt);

		return params.creator_uid_link === makeUserLink(user_id);
	}
	return false;
}
function isLauAndLetter(letter_id) {
	var elt = $("#" + letter_id);
	var params = getRoomParams(elt);

	return params.creator_uid_link === undefined && !isUserLoggedIn();
}
function onMenuClick(event) {
	var a_elt = $(event.target);
	var li_elt = a_elt.parent();

	if (li_elt.hasClass("ui-state-disabled"))
		return;

	var data_action = li_elt.data("action");

	if (data_action === "edit")
		editLetter();
	else if (data_action === "delete")
		deleteLetter(0);
	else if (data_action === "permanent_delete")
		deleteLetter(2);
	else if (data_action === "prosecute")
		deleteLetter(3);
	else if (data_action === "report")
		deleteLetter(4);

	// return false;
}
// type: 0=delete, 1=undelete, 2=permanent delete, 3=prosecute, 4=report, 5=unreport.
function deleteLetter(type) {
	var target_elt = $(g_room_menu_target).closest(isViewMap() ? "div.sticker" : "table.room");

	var letter_id = target_elt.attr("id");

	if (type === 0 && target_elt.hasClass("deleted"))
		type = 1;
	if (type === 4 && target_elt.hasClass("reported"))
		type = 5;

	//var undelete = is_permanent_delete ? false : target_elt.hasClass("deleted");
	//var cmd_name = is_permanent_delete ? "永久刪除" : (undelete ? "復原" : (is_fast_prosecute ? "先斬後奏" : "刪除"));
	var cmd_name = ["刪除", "復原", "永久刪除", "先斬後奏", "檢舉", "取消檢舉"][type];
	var flag_type = [MT_DELETED, MT_DELETED, MT_PERMANENT_DELETE, MT_DELETED, MT_REPORT, MT_REPORT][type];
	var flag_num = [1, -1, 1, 1, 1, 0][type];

	if (isLetterId(letter_id)) {
		var sn = letterIdToSerialNumber(letter_id);
		var target_name = (sn > 0 ? "留言" : "討論串");

		var msg = "";
		if (type === 2) {
			msg += "<p>原作者可以永久刪除自己的留言，任何人都無法復原。</p>";
		}
		else if (type === 4 || type === 5) {
			msg += "<p>任何人可以檢舉任一" + target_name + "。</p>";
			if (sn > 0)
				msg += "<p>串主或副板主以上可以刪除被檢舉的留言。</p>";
			else
				msg += "<p>副板主以上可以刪除被檢舉的討論串。</p>";

			msg += "<p>副板主以上可以取消檢舉（不刪除）被檢舉的" + target_name + "。</p>";
		}
		else {
			if (sn > 0) {
				msg += "<p>任何人可以對任一留言先斬後奏，但可能被串主或副板主以上復原。</p>";
				msg += "<p>串主可以刪除討論串中的任一留言，也可以復原。</p>";
			}
			msg += "<p>副板主以上可以刪除" + target_name + "，也可以復原。</p>";
		}
		msg += "<p>請按”確定”以送出" + cmd_name + target_name + "指令。</p>";

		var title = cmd_name + (sn > 0 ? "留言（第" + sn + "則）" : "討論串");

		var delete_dlg = createGeneralDialog({
			title: title,
			message: msg,
			has_reason: true,
			callback: function () {
				var reason = delete_dlg.find("textarea").val();
				var foreword = ">>" + title + "\n";

				postGoods("/controlletter",
					{
						board_id: getboardid(),
						discussion_id: getdiscussionid(),
						letter_id: letter_id,
						delta_flags: flagsAdd("", flag_type, flag_num),
						reason: foreword + reason,
					},
					function (suc, response) {
						if (suc && response.ok) {
							location.reload();
						}
						else {
							modifyGeneralDialog(delete_dlg,
								{
									err_msg: response
								});
						}
					});
			}
		});
	}
}
function voteLetter(flag_type, event) {
	if (event) {
		if (isViewSquare())
			//if (g_last_mousedown_xy.x !== event.clientX || g_last_mousedown_xy.y !== event.clientY)
			if (fartherThan(g_last_mousedown_xy.x, event.clientX, g_last_mousedown_xy.y, event.clientY, MOUSE_MOVE_THRESHOLD))
				return;

		// for view=map, event.target is the "<image>".
		var target_elt = $(event.currentTarget).closest(isViewMap() ? "div.sticker" : "table.room");
	}
	else {
		var target_elt = $(g_room_menu_target).closest(isViewMap() ? "div.sticker" : "table.room");
	}
	var letter_id = target_elt.attr("id");
	if (isLetterId(letter_id)) {
		postGoods("/voteletter",
					{
						board_id: getboardid(),
						discussion_id: getdiscussionid(),
						letter_id: letter_id,
						delta_flags: flagsAdd("", flag_type, 1),
					},
					function (suc, response) {
						if (suc && response.ok) {
							if (isViewSquare())
								var hash = "#g_" + letter_id;
							else
								var hash = "#" + letter_id;

							refreshDiscussionLoad(hash, true);
						}
						else {
							alert(/*"投票失敗。" + */response);
						}
					});
	}
}
var g_discussion_load_time = "";

function clearLetterElements() {
	$("[id^='e']").each(function (idx, elt) {
		var letter_id = elt.id;
		if (isLetterId(letter_id)) {
			elt.parentNode.removeChild(elt);
		}
	});
}
var g_waiting_refresh_cnt = 0;
function refreshDiscussionLoad(hash, force) {
	var $btn = $("#refresh_btn");
	var board_id = getboardid();
	var discussion_id = getdiscussionid();

	//console.log("refreshDiscussionLoad.");

	// For Chrome. As for IE, the handler will not be entered.
	if ($btn.prop("disabled")) {
		if (force)
			g_waiting_refresh_cnt++;
		return;
	}
	//console.log("Do refresh.");
	$btn.prop("disabled", true).text("更新中");		// works for IE but not Chrome.

	//getGoods("/discussionload/" + board_id + "/" + discussion_id,
	getGoods("/refreshload/" + board_id + "/" + discussion_id + "?lmt=" + g_discussion_load_time,
		function (data) {
			if (data != null) {
				if (data.length > 0) {
					var $data = $("<div>" + data + "</div>");
					var discussion_load_time = $data.children("var").text();

					if (discussion_load_time != g_discussion_load_time) {
						location.hash = hash/*"#last"*/;
						onNewDiscussionLoad($data);

						g_discussion_load_time = discussion_load_time;

						$btn.text("已更新");
					}
					else
						$btn.text("無更新內容");
				}
				else
					$btn.text("無更新內容");
			}
			else
				$btn.text("更新失敗");

			setTimeout(function () {
				$btn.prop("disabled", false).text("更新");
				if (g_waiting_refresh_cnt > 0) {
					g_waiting_refresh_cnt--;
					setTimeout(refreshDiscussionLoad, 0, hash/*is the previous one*/, false);
				}
			}, 5 * 1000);
		});
}
function processDiscussionLoadSquare(data) {
	var letter_arr = [];

	data.children("section").each(function (idx, elt) {
		var letter = dataToLetter($(elt));

		var sn = letterIdToSerialNumber(letter.letter_id);
		letter_arr[sn] = letter;
	});
	var letter_coords = {};

	for (var sn = 0; sn < letter_arr.length; sn++) {
		if (!letter_arr[sn]) continue;
		var letter = letter_arr[sn];

		if (sn >= next_serial_number)
			next_serial_number = sn + 1;

		var deleted = isLetterDeleted(letter.flags);	// flagsCheck(letter.flags, DELETED_FLAG_CHAR, 1);
		var insider_only = flagsCheck(letter.flags, MT_AUTHORIZATION, 2);

		if (letter.subtype === "h") {
			insertHeading(letter);
		}
		else {
			// delete_remark won't have coord so will not show.
			if (!deleted && !insider_only) {
				var coord = insertLetter(/*panes, */sn, letter);		// beat browser jump. don't let browser automatically jump to that letter.

				if (coord) {
					letter_coords.last = coord;
					if ("#g_" + letter.letter_id === location.hash)
						letter_coords.hash = coord;
					if (!letter_coords.first)
						letter_coords.first = coord;
				}
			}
		}
	}
	if (location.hash === "#last")
		letter_coords.hash = letter_coords.last;
	letter_coords.home = letter_coords.hash || letter_coords.first;

	if (letter_coords.home)
		goCoord(letter_coords.home);

	return letter_coords;
}
function extractLatLng(flags, coord) {
    coord.latlng = null;
    var text = flagsGet(flags, MT_COORDINATE);
    if (text !== null) {
        text = text.replace(/^\(([\d\.\-]+),([\d\.\-]+)\)$/, function (match_text, $1, $2) {
            coord.latlng = { x: Number($1), y: Number($2) };
            return "";
        });
    }
    //return text;
}
