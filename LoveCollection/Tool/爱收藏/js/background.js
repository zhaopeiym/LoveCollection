var baseUrl = "http://i.haojima.net";
//baseUrl = "http://localhost:2728";

//右键功能
chrome.contextMenus.create({
    title: "浏览查看爱收藏",
    contexts: ['page'],
    onclick: function (info, tab) {
        window.open(baseUrl);
    }
});

chrome.contextMenus.create({
    title: "添加网址到爱收藏",
    contexts: ['page'],
    onclick: function (info, tab) {
        collection(info, function (urlValue, userToken) {
            chrome.tabs.executeScript(tab.id, { code: 'loadType("' + urlValue + '","' + userToken + '");' });
        });
    }
});

chrome.contextMenus.create({
    title: "添加链接到爱收藏",
    contexts: ['link'],
    onclick: function (info, tab) {
        collection(info, function (urlValue, userToken) {
            chrome.tabs.executeScript(tab.id, { code: 'loadType("' + urlValue + '","' + userToken + '");' });
            //chrome.tabs.executeScript(tab.id, { code: 'toastr.options = { "positionClass": "toast-top-center" };toastr.success("收藏成功");' });
        });
    }
});

//chrome.contextMenus.create({
//    title: "添加爱收藏并浏览",
//    contexts: ['page', 'link'],
//    onclick: function (info, tab) {
//        collection(info, function () {
//            window.open(baseUrl);
//        });
//    }
//});

//chrome.cookies.get({ url: baseUrl, name: "userId" }, function (cookie) {
//    if (cookie && cookie.value) {
//        //alert(cookie.value);
//        $.ajax({
//            url: baseUrl + "/api/LoveCollection/GetTypes?userToken=" + cookie.value,
//            success: function (sData) {              
//                for (var i = 0; i < sData.length; i++) {
//                    chrome.contextMenus.create({
//                        title: "添加网址到：" + sData[i].name,
//                        contexts: ['page'],
//                        onclick: function (info, tab) {
//                            collection(info, function (urlValue, userToken) {
//                                chrome.tabs.executeScript(tab.id, { code: 'loadType("' + urlValue + '","' + userToken + '");' });
//                            });
//                        }
//                    });
//                }
//            },
//            error: function (e) {
//                //var etemp = "";
//                //for (var v in e) {
//                //    etemp += v + ":" + e[v] + "\r\n";
//                //}
//                //alert(etemp);
//            }
//        });
//    }
//});

function collection(info, callBack) {
    var url = info["linkUrl"] || info["pageUrl"];
    chrome.cookies.get({ url: baseUrl, name: "userId" }, function (cookie) {
        if (cookie === null || !cookie.value) {
            alert("请先登录爱收藏");
            window.open(baseUrl);
            return;
        }
        $.isFunction(callBack) && callBack(url, cookie.value);
        //$.ajax({
        //    url: baseUrl + "/api/LoveCollection/AddCollectionByCRX",
        //    data: { "url": url, "userToken": cookie.value },
        //    type: "post",
        //    success: function (sData) {
        //        $.isFunction(callBack) && callBack(cookie.value);
        //    },
        //    error: function (e) {
        //        //for (var m in e) {                       
        //        //}                   
        //    }
        //});
    });
}