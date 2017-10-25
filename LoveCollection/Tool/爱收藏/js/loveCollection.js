var baseUrl = "https://i.haojima.net";
//baseUrl = "http://localhost:2728";

function loadType(urlValue, userToken) {
    toastr.options = { "positionClass": "toast-top-center" };
    $.ajax({
        url: baseUrl + "/api/LoveCollection/GetTypes?userToken=" + userToken,
        success: function (sData) {
            var htmlType = "<div class='loveCollectionType'>";
            for (var i = 0; i < sData.length; i++) {
                htmlType += '<span><input type="radio" name="loveCollectiontype" value="' + sData[i].id + '" />' + sData[i].name + "</span>";
            }
            htmlType += "<span class='span-addType'><a class='a-addType' href='javascript:;'>新建分类</a></span>";
            htmlType += '<div class="div-save"><button class="btn-cancel" type="button">取消</button><button class="btn-save" type="button">收藏</button></div>';
            htmlType += "</div>";
            $("body").append(htmlType);

            $('input:radio[name="loveCollectiontype"]').iCheck({
                checkboxClass: 'icheckbox_minimal-blue',
                radioClass: 'iradio_minimal-blue',
                increaseArea: '20%',
            });
            //加载绑定类型
            $.ajax({
                url: baseUrl + "/api/LoveCollection/GetTypeIdByByUrlCRX?userToken=" + userToken + "&url=" + urlValue,
                success: function (sDataTypeId) {
                    if (sDataTypeId > 0)
                        $('input:radio[value="' + sDataTypeId + '"]').iCheck('check');
                    else
                        $('input:radio[value="' + sData[0].id + '"]').iCheck('check');
                }
            });

            //
            $(".a-addType").click(function () {
                $(this).closest(".span-addType").html("<input type='text' class='ipt-addType' />");
            });
            //添加类型
            $(".span-addType").on("blur", ".ipt-addType", function () {
                var $this = $(this);
                //空类型判断
                if ($.trim($this.val()) === "") return;
                //重复类型判断
                var isHas = false;
                $(".loveCollectionType span").each(function (i, e) {
                    if ($.trim($(e).text()) === $.trim($this.val())) {
                        isHas = true;
                    }
                });
                if (isHas) {
                    toastr.success("已经存在此类型，请更换名称");
                    return;
                }
                $(".span-addType").html("保存中...");
                $.ajax({
                    url: baseUrl + "/api/LoveCollection/AddType?name=" + $this.val() + "&userToken=" + userToken,
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
            $(".span-addType").on("keydown", ".ipt-addType", function () {
                if (event.keyCode == 13) {
                    $(this).blur();//调用失去焦点事件
                }
            })

            //保存
            $(".btn-save").click(function () {
                var typeId = $('input:radio[name="loveCollectiontype"]:checked').val();
                $(".loveCollectionType").remove();
                $.ajax({
                    url: baseUrl + "/api/LoveCollection/AddCollectionByCRX",
                    data: { "url": urlValue, "title": $("title").text(), "userToken": userToken, "typeId": typeId },
                    type: "post",
                    success: function (sData) {
                        toastr.success("收藏成功");
                    },
                    error: function (e) {
                        toastr.success("收藏失败");
                    }
                });
            });

            // 取消
            $(".btn-cancel").click(function () {
                $(".loveCollectionType").remove();
            });
        },
        error: function (e) {
            //for (var m in e) {                       
            //}                   
        }
    });
}

function addCollection(urlValue, userToken) {
    toastr.options = { "positionClass": "toast-top-center" };
    $.ajax({
        url: baseUrl + "/api/LoveCollection/AddCollectionByCRX",
        data: { "url": urlValue, "title": $("title").text(), "userToken": userToken },
        type: "post",
        success: function (sData) {
            toastr.success("收藏成功")
        },
        error: function (e) {
            toastr.error("收藏失败");
            //var temp = "";
            //for (var m in e) {
            //    temp += "m:" + e[m] + "\r\n";
            //}
            //alert(temp);
        }
    });
}