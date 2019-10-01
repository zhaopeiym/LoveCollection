var loveCollection = function () {
    var _pageData = {
        getCell: function (id, title, url) {
            url = url.indexOf("//") === -1 ? ("http://" + url) : url;
            return '<div class="cell" data-id="' + id + '">\
                        <div class="div-edit">\
                            <span class="span-edit">\
                                <a class="a-url-address" draggable ="true"  title="'+ title + '" href="' + url + '">' + title + '</a>\
                            </span >\
                            <a class="btn-edit visibilityHidden"><img src="/images/edit.png" /></a>\
                            <a class="btn-del visibilityHidden"><img src="/images/Delete.png" /></a>\
                        </div>\
                        </div>';
        },
        getCellA: function (url, title) {
            url = url.indexOf("//") === -1 ? ("http://" + url) : url;
            return '<a class="a-url-address" draggable="true" title="' + title + '" href="' + url + '">' + title + '</a>';
        },
        getCellNew: function () {
            return '<div class="cell">\
                        <div class="div-edit">\
                            <span class="span-edit btn-new visibilityHidden displayInlineBlock">新增</span>\
                        </div>\
                        </div>';
        },
        getTypeBlock: function (typeId, typeName) {
            return '<div class="type-block" data-typeid="' + typeId + '">\
                            <div data-id="' + typeId + '" class="panel-title"><span draggable="true">' + typeName + '</span>\
                               <a class="btn-type-edit visibilityHidden"><img src="/images/edit.png" /></a>\
                               <a class="btn-type-del visibilityHidden"><img src="/images/Delete.png" /></a>\
                            </div>\
                            <div class="div-block clearfix">\
                                <div class="cell">\
                                    <div class="span-edit btn-new visibilityHidden displayInlineBlock">新增</div>\
                                </div>\
                            </div>\
                        </div>';
        },
        getTypeTitle: function (id, name) {
            return '<div class="type-block" data-typeid="' + id + '" >\
                            <div  data-id="' + id + '" class="panel-title"><span draggable="true">' + name + '</span>\
                                  <a class="btn-type-edit visibilityHidden"><img src="/images/edit.png" /></a>\
                                  <a class="btn-type-del visibilityHidden"><img src="/images/Delete.png" /></a>\
                            </div>\
                            <div class="div-block clearfix"></div>\
                        </div>';
        },
        getTypeOperation: function (text) {
            return '<span draggable="true">' + text + '</span>\
                        <a class="btn-type-edit visibilityHidden"><img src="/images/edit.png" /></a>\
                        <a class="btn-type-del visibilityHidden"><img src="/images/Delete.png" /></a>';
        }
    };

    return {
        init: function () {
            toastr.options = { "positionClass": "toast-top-center" };
            this.bindEvent();
            this.pageInit();
        },
        //页面加载初始化
        pageInit: function () {
            //获取绑定类型
            $.ajax({
                url: "/api/LoveCollection/GetTypes",
                success: function (sData) {
                    var htmlType = "";
                    for (var i = 0; i < sData.length; i++) {
                        htmlType += '<span><input type="radio" name="loveCollectiontype" value="' + sData[i].id + '" />' + sData[i].name + "</span>";
                    }
                    htmlType += "<span class='span-addType'><a class='a-addType' href='javascript:;'>新建分类</a></span>";
                    $(".collectionTypes").html(htmlType);

                    $('input:radio[name="loveCollectiontype"]').iCheck({
                        checkboxClass: 'icheckbox_minimal-blue',
                        radioClass: 'iradio_minimal-blue',
                        increaseArea: '20%',
                    });
                }
            });
        },
        //事件绑定
        bindEvent: function () {
            var $content = $(".content");
            var dropObjTyep;
            var dropReceiveObj;

            //新建分类
            $(".btn-addtype").click(function () {
                var _this = $(this);
                if (_this.hasClass("add")) {//新建
                    $(".add-input-type.displayNone").removeClass("displayNone");
                    _this.text("保存分类").addClass("save").removeClass("add").prev().focus();
                }
                else if (_this.hasClass("save")) {//保存
                    $(".add-input-type").addClass("displayNone");
                    var typeName = $(".add-input-type").val();
                    _this.text("新建分类").addClass("add").removeClass("save");

                    $.ajax({
                        url: "/api/LoveCollection/AddType?name=" + typeName,
                        success: function (sData) {
                            var typeId = sData;
                            $(".content").append(_pageData.getTypeBlock(typeId, typeName));
                        }
                    });
                }
            });
            //删除分类
            $content.on("click", ".panel-title .btn-type-del", function () {
                var $this = $(this);
                var typeId = $this.parent().data("id");//类型id
                if (confirm("确定要删除类型和类型下的所有内容吗")) {
                    $this.closest(".type-block").remove();
                    $.ajax({
                        url: "/api/LoveCollection/DelType?typeId=" + typeId,
                        success: function (sData) {

                        }
                    });
                }
            })
            //保存分类（失去焦点时）
            $content.on("blur", ".inp-title", function () {
                var $this = $(this);
                var typeId = $this.parent().data("id");//类型id
                $this.parent().html(_pageData.getTypeOperation($this.val())); //"<span>" + $this.val() + "</span>");
                $.ajax({
                    url: "/api/LoveCollection/ModifyTypeNameById",
                    type: "post",
                    data: { "typeId": typeId, "typeName": $this.val() },
                    success: function (sData) {

                    }
                });
            })
            //鼠标在“分类”上面的时候
            $content.on("mouseover", ".panel-title", function () {
                if ($(this).find("input").length) return;
                $(this).find(".btn-type-edit.visibilityHidden").removeClass("visibilityHidden");
                $(this).find(".btn-type-del.visibilityHidden").removeClass("visibilityHidden");
            });
            //鼠标在移出“分类”的时候
            $content.on("mouseout", ".panel-title", function () {
                $(this).find(".btn-type-edit").addClass("visibilityHidden");
                $(this).find(".btn-type-del").addClass("visibilityHidden");
            });
            //编辑分类名称
            $content.on("click", ".panel-title .btn-type-edit", function () {
                var $this = $(this);
                var $panel = $this.closest(".panel-title");
                var text = $this.parent().find("span").text();
                if (!$this.find(".inp-title").length)
                    $panel.html("<input class='inp-title' type='text' />");
                $panel.find(".inp-title").focus().val(text);//关闭移到最右
            })
            //分类(回车保存)
            $content.on("keydown", ".inp-modify", function () {
                if (event.keyCode == 13) {
                    $(this).blur();//调用失去焦点事件
                }
            });
            //分类(回车保存)
            $content.on("keydown", ".add-input-type", function () {
                if (event.keyCode == 13) {
                    $(this).next().click();//调用失去焦点事件
                }
            });

            //【类型】拖动
            $content.on("dragstart", ".panel-title span", function () {
                dropObjTyep = "urlType";
                var obj = $(event.target).closest(".panel-title");
                obj.closest(".type-block").addClass("moveobj");
                event.dataTransfer.setData("Text", obj.data("id"));
            });
            //【类型】设置可以接收拖动
            $content.on("dragover", ".type-block", function () {
                if (dropObjTyep !== "urlType") return;
                event.preventDefault();
                $(".type-block.highlight").removeClass("highlight");
                var obj = $(event.target).closest("div.type-block");
                obj.addClass("highlight");
                dropReceiveObj = { obj: obj, type: "typeBlock" };
            });
            //【url】拖动
            $content.on("dragstart", ".a-url-address", function () {
                dropObjTyep = "url";
                var obj = $(event.target).closest(".cell");
                obj.addClass("moveobj");
                event.dataTransfer.setData("Text", obj.data("id"));
            });
            //【url】设置可以接收拖动
            $content.on("dragover", ".cell", function () {
                if (dropObjTyep !== "url")
                    return;
                if ($(event.target).closest("div.cell").find(".span-edit.btn-new").length)
                    return;//“新增”按钮不能被移动

                event.preventDefault();
                $(".cell.highlight").removeClass("highlight");
                $("div.div-block").removeClass("highlight");
                var obj = $(event.target).closest("div.cell");
                obj.addClass("highlight");
                dropReceiveObj = { obj: obj, type: "urlCell" };
            });
            //【url block】设置可以拖放
            $content.on("dragover", ".div-block ", function () {
                if (dropObjTyep !== "url") return;
                var cell = $(event.target).closest(".div-block").find(".cell.highlight");
                if (cell.length && !cell.hasClass("moveobj")) {//如果当前块已经有被选中替换对象
                    return;//
                }
                event.preventDefault();
                $(".cell.highlight").removeClass("highlight");
                $("div.div-block").removeClass("highlight");
                var obj = $(event.target).closest("div.div-block");
                obj.addClass("highlight");
                dropReceiveObj = { obj: obj, type: "urlBlock" };
            });
            //接收拖动数据
            $("html").on("drop", function () {
                event.preventDefault();

                //接收拖动数据【url】
                var dropUrl = function () {
                    var urlId = event.dataTransfer.getData("Text");
                    var moveEle = $(".cell[data-id='" + urlId + "']");//正在移动的对象
                    var receiveEle = $(dropReceiveObj.obj).closest("div.cell");//准备与之换位的对象

                    if (receiveEle.prev().data("id") === moveEle.data("id"))//如果本来就在对象的前面，则移到后面
                        receiveEle.after(moveEle);
                    else
                        receiveEle.before(moveEle);//移到对象的前面

                    var typeId = $(receiveEle).closest(".type-block").data("typeid");//类型id
                    $.ajax({
                        url: "/api/LoveCollection/ModifySort",
                        data: { "id": $(moveEle).data("id"), "typeId": typeId, "previd": $(moveEle).prev().data("id"), "nextid": $(moveEle).next().data("id") },
                        type: "post",
                        success: function (sData) {

                        }
                    })
                };

                //接收拖动数据【类型】
                var dropUrlType = function () {
                    var typeId = event.dataTransfer.getData("Text");
                    var moveEle = $(".type-block[data-typeid='" + typeId + "']");//正在移动的对象
                    var receiveEle = dropReceiveObj.obj;//准备与之换位的对象

                    if (receiveEle.prev().data("typeid") === moveEle.data("typeid"))//如果本来就在对象的前面，则移到后面
                        receiveEle.after(moveEle);
                    else
                        receiveEle.before(moveEle);//移到对象的前面
                    $.ajax({
                        url: "/api/LoveCollection/ModifyTypeSort",
                        data: { "id": $(moveEle).data("typeid"), "previd": $(moveEle).prev().data("typeid"), "nextid": $(moveEle).next().data("typeid") },
                        type: "post",
                        success: function (sData) {

                        }
                    });
                }

                //接收拖放数据【Url Block】
                var dropUrlBlock = function () {
                    var urlId = event.dataTransfer.getData("Text");
                    var moveEle = $(".cell[data-id='" + urlId + "']");//正在移动的对象
                    $(dropReceiveObj.obj).find(".span-edit.btn-new").closest(".cell").before(moveEle);
                    var typeId = $(dropReceiveObj.obj).closest(".type-block").data("typeid");//类型id

                    $.ajax({
                        url: "/api/LoveCollection/ModifySort",
                        data: { "id": $(moveEle).data("id"), "typeId": typeId, "previd": $(moveEle).prev().data("id"), "nextid": $(moveEle).next().data("id") },
                        type: "post",
                        success: function (sData) {

                        }
                    })
                }

                //接收拖动数据【url】
                if (dropReceiveObj.type === "urlCell") {
                    dropUrl();
                }
                //接收拖动数据【类型】
                else if (dropReceiveObj.type === "typeBlock") {
                    dropUrlType();
                }
                else if (dropReceiveObj.type === "urlBlock") {
                    dropUrlBlock();
                }

                $(".highlight").removeClass("highlight");
                $(".moveobj").removeClass("moveobj");
            });
            //设置可以拖放
            $("html").on("dragover", function () {
                event.preventDefault();
            });

            //回车保存
            $content.on("keydown", ".inp-title", function () {
                if (event.keyCode == 13) {
                    $(this).blur();//调用失去焦点事件
                }
            });
            //点击编辑【url】
            $content.on("click", ".btn-edit", function () {
                var $this = $(this);
                var $spanedit = $this.parent().find(".span-edit");
                var title = $spanedit.find("a").prop("title");
                var href = $spanedit.find("a").prop("href");
                var id = $this.closest(".cell").data("id");
                var typeId = $this.closest(".type-block").data("typeid");

                $(".modal input[value='" + typeId + "']").iCheck('check');
                $(".modal .value").val(title);
                $(".modal .url").val(href);
                $(".modal .id").val(id);
                $(".modal.displayNone").removeClass("displayNone");
            })
            //删除【url】
            $content.on("click", ".btn-del", function () {
                var $this = $(this);
                var id = $this.closest(".cell").data("id");
                if (confirm("确定要删除吗")) {
                    $this.closest(".cell").remove();
                    $.ajax({
                        url: "/api/LoveCollection/DelCollection?id=" + id,
                        success: function (sData) {

                        }
                    });
                }
            })
            //保存（保存修改）
            $content.on("blur", ".inp-modify", function () {
                var $this = $(this);
                var id = $this.closest(".cell").data("id");
                var title = $this.val();
                var href = $this.data("href");
                //if (strs.length !== 3 || !strs[2]) return;
                $this.parent().html(_pageData.getCellA(href, title));
                $.ajax({
                    url: "/api/LoveCollection/ModifyCollection",
                    type: "post",
                    data: { "id": id, "url": href, "title": title },
                    success: function (sData) {

                    }
                });
            })
            //鼠标进入（编辑）
            $content.on("mouseover", ".cell", function () {
                if ($(this).find("input").length) return;
                $(this).find(".btn-edit.visibilityHidden").removeClass("visibilityHidden");
                $(this).find(".btn-del.visibilityHidden").removeClass("visibilityHidden");
            });
            //鼠标移出
            $content.on("mouseout", ".cell", function () {
                $(this).find(".btn-edit").addClass("visibilityHidden");
                $(this).find(".btn-del").addClass("visibilityHidden");
            });

            //新增【url】
            $content.on("click", ".btn-new", (function () {
                var $this = $(this);
                if ($this.find(".inp-edit").length) return;
                if ($this.find("a").length) return;

                $this.html("<input class='inp-edit' type='text' />").find("input").focus();
                $this.removeClass("btn-new");
            }));
            //回车保存
            $content.on("keydown", ".inp-edit", function () {
                if (event.keyCode == 13) {
                    $(this).blur();//调用失去焦点事件
                }
            });
            //保存【url】
            $content.on("blur", "input.inp-edit", function () {
                var $this = $(this);
                var $oldCell = $this.closest(".cell");
                if (!$this.val()) {
                    $oldCell.find(".span-edit").html('新增').addClass("btn-new");
                    return;
                }
                var typeId = $this.closest(".type-block").data("typeid");
                var $divBlock = $this.closest(".div-block");
                var url = $this.val().indexOf("//") === -1 ? ("http://" + $this.val()) : $this.val();
                $oldCell.find(".span-edit").html('解析保存中...');
                $.ajax({
                    url: "/api/LoveCollection/AddCollection",
                    data: { "url": url, "typeId": typeId },
                    type: "post",
                    success: function (sData) {
                        $oldCell.remove();
                        $divBlock.append(_pageData.getCell(sData.id, sData.title, url));
                        $divBlock.append(_pageData.getCellNew());
                    },
                    error: function () {
                        $oldCell.find(".span-edit").html('新增');
                        //$this.parent().html('新增');
                    }
                });
            });
            //【新增按钮】鼠标进入（编辑）
            $content.on("mouseover", ".div-block", function () {
                $(this).find(".btn-new").removeClass("visibilityHidden");//.addClass("displayInlineBlock");
            });
            //【新增按钮】鼠标移出
            $content.on("mouseout", ".div-block", function () {
                $(this).find(".btn-new").addClass("visibilityHidden");//.removeClass("displayInlineBlock")
            });

            //登录/注册
            $(".btn-register").click(function () {
                var mail = $(".mail").val();
                var passwod = $(".passwod").val();
                $.ajax({
                    url: "/api/LoveCollection/Register",
                    type: "post",
                    data: { "mail": mail, "passwod": passwod },
                    success: function (sData) {
                        if (!sData.isSuccess) {
                            toastr.success(sData.message);
                        }
                        else {
                            location.reload(true);
                        }
                    },
                });
            });
            //回车登录
            $content.on("keydown", ".passwod", function () {
                if (event.keyCode == 13) {
                    $(".btn-register").click();
                }
            });

            //取消
            $(".modal .cancel").click(function () {
                $(".modal").addClass("displayNone");
            });
            //修改
            $(".modal .modify").click(function () {
                var $modal = $(".modal");
                var typeId = $modal.find('input:radio[name="loveCollectiontype"]:checked').val();
                var title = $modal.find(".value").val();
                var url = $modal.find(".url").val();
                var id = $modal.find(".id").val();
                $.ajax({
                    url: "/api/LoveCollection/ModifyCollection",
                    type: "post",
                    data: { "id": id, "url": url, "title": title, "typeId": typeId },
                    success: function (sData) {
                        toastr.success("修改成功");

                        var $cell = $(".cell[data-id='" + id + "']");
                        var oleTypeId = $cell.closest(".type-block").data("typeid");
                        if (oleTypeId != typeId) {
                            $(".type-block[data-typeid='" + typeId + "']").find(".btn-new").closest(".cell").before($cell);
                        }
                        $modal.addClass("displayNone");
                    }
                });
            });

            $(".modal").on("click", ".a-addType", function () {
                $(this).closest(".span-addType").html("<input type='text' class='ipt-addType' />");
            });
            //添加类型
            $(".modal").on("blur", ".ipt-addType", function () {
                var $this = $(this);
                //空类型判断
                if ($.trim($this.val()) === "") return;
                //重复类型判断
                var isHas = false;
                $(".modal .collectionTypes span").each(function (i, e) {
                    if ($.trim($(e).text()) === $.trim($this.val())) {
                        isHas = true;
                    }
                });
                if (isHas) {
                    toastr.warning("已经存在此类型，请更换名称");
                    return;
                }

                $(".span-addType").html("保存中...");
                $.ajax({
                    url: "/api/LoveCollection/AddType?name=" + $this.val(),
                    success: function (sData) {
                        $(".span-addType").html('<input type="radio" name="loveCollectiontype" value="' + sData + '" />' + $this.val());
                        $(".span-addType").find('input:radio[name="loveCollectiontype"]').iCheck({
                            checkboxClass: 'icheckbox_minimal-blue',
                            radioClass: 'iradio_minimal-blue',
                            increaseArea: '20%',
                        });
                    }
                });
            })
            $(".modal").on("keydown", ".ipt-addType", function () {
                if (event.keyCode == 13) {
                    $(this).blur();//调用失去焦点事件
                }
            })

            //目录(隐藏)
            $(".directoryItmeTypesContent").mouseout(function (e) {
                //阻止子元素相应mouseout事件
                evt = window.event || e;
                var obj = evt.toElement || evt.relatedTarget;
                var pa = this;
                if (pa.contains(obj)) return false;

                $(this).addClass("displayNone");
                $(".showDirectory").removeClass("displayNone");
            });
            //目录(显示)
            $(".showDirectory").mouseover(function () {
                $(this).addClass("displayNone");
                $(".directoryItmeTypesContent").removeClass("displayNone");
            });
            //目录(点击 隐藏)
            $(".directoryItmeTypes").on("click", ".apositioning", function () {
                var typeid = $(this).data("typeid");
                $(".showblock").removeClass("showblock");
                $("#" + typeid).parent().addClass("showblock");
                //$(".directoryItmeTypesDel").click();

                //隐藏
                $(".directoryItmeTypesContent").addClass("displayNone");
                $(".showDirectory").removeClass("displayNone");
            });

            //全部折叠展开
            $(".allFold").click(function () {
                if ($(this).html().trim() === "全部折叠") {
                    $(this).html("全部展开");
                    $(".collectionDetailedInfo")
                        .find(".type-block").removeClass("show")
                        .find(".div-block").hide()
                        .find(".btn-type-shwodisplay")
                        .find("img")
                        .attr("src", "/images/左.png");
                    localStorage.setItem("showTypeList", []);

                }
                else {
                    $(this).html("全部折叠");
                    $(".collectionDetailedInfo")
                        .find(".type-block").addClass("show")
                        .find(".div-block").show()
                        .find(".btn-type-shwodisplay")
                        .find("img").attr("src", "/images/下.png");
                    localStorage.setItem("showTypeList", types.map(function (item) { return item.Id; }));                  
                }
            });
            //折叠展开
            $(".collectionDetailedInfo").on("click", ".panel-title", function () {
                var $this = $(this).find(".btn-type-shwodisplay");
                if ($this.closest(".type-block").hasClass("show")) {
                    $this.find("img").attr("src", "/images/左.png");
                    $this.closest(".type-block").find(".div-block").hide(100, function () {
                        $this.closest(".type-block").removeClass("show");
                    });
                }
                else {
                    $this.find("img").attr("src", "/images/下.png");
                    $this.closest(".type-block").find(".div-block").show(100, function () {
                        $this.closest(".type-block").addClass("show");
                    });
                }
                //等动画完成后执行
                setTimeout(function () {
                    var typeids = $(".type-block.show").map(function (i, item) {
                        return $(item).data("typeid");
                    });
                    localStorage.setItem("showTypeList", typeids.toArray());
                }, 150);
            });
        },
        //方法
        method: {
            getCookie: function (name) {
                var arr, reg = new RegExp("(^| )" + name + "=([^;]*)(;|$)");
                if (arr = document.cookie.match(reg))
                    return unescape(arr[2]);
                else
                    return null;
            },
            delCookie: function (name) {
                var exp = new Date();
                exp.setTime(exp.getTime() - 1);
                var cval = this.getCookie(name);
                if (cval != null)
                    document.cookie = name + "=" + cval + ";expires=" + exp.toGMTString();
            },
            setCookie: function (name, value, time) {
                document.cookie = name + "=" + value;
            }
        }
    };
}();

$(function () {
    loveCollection.init();


});