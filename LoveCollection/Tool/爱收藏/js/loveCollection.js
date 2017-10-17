var baseUrl = "https://i.haojima.net";

function loadType(urlValue,userToken) {    
    $.ajax({
        url: baseUrl + "/api/LoveCollection/GetTypes?userToken=" + userToken,
        success: function (sData) {
            var htmlType = "<div class='loveCollectionType'>";
            for (var i = 0; i < sData.length; i++) {
                htmlType += '<span><input type="radio" name="loveCollectiontype" value="' + sData[i].id + '" />' + sData[i].name + "</span>";
            }
            htmlType += '<div class="div-save"><button class="btn-cancel" type="button">取消</button><button class="btn-save" type="button">收藏</button></div>';
            htmlType += "</div>";
            $("body").append(htmlType);

            $('input:radio[name="loveCollectiontype"]').iCheck({
                checkboxClass: 'icheckbox_minimal-blue',
                radioClass: 'iradio_minimal-blue',
                increaseArea: '20%',
            });

            //保存
            $(".btn-save").click(function () {
                var typeId = $('input:radio[name="loveCollectiontype"]:checked').val();
                $(".loveCollectionType").remove();    
                toastr.options = { "positionClass": "toast-top-center" };
                $.ajax({
                    url: baseUrl + "/api/LoveCollection/AddCollectionByCRX",
                    data: { "url": urlValue, "userToken": userToken, "typeId": typeId},
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