//右键功能
chrome.contextMenus.create({
    title: "浏览查看爱收藏",
    contexts: ['page'],
    onclick: function (info, tab) {
        window.open("https://i.haojima.net");
    }
});

chrome.contextMenus.create({
    title: "添加网址到爱收藏",
    contexts: ['page'],
    onclick: function (info, tab) {             
        collection(info, function () {
            alert("收藏成功");
        });
    }
});

chrome.contextMenus.create({
    title: "添加链接到爱收藏",
    contexts: ['link'],
    onclick: function (info, tab) {
        collection(info, function () {
            alert("收藏成功");
        });
    }
});

chrome.contextMenus.create({
    title: "添加爱收藏并浏览",
    contexts: ['page','link'],
    onclick: function (info, tab) {     
        collection(info, function () {
            window.open("https://i.haojima.net");
        });
    }
});

function collection(info, callBack) {
    var url = info["linkUrl"] || info["pageUrl"];    
    chrome.cookies.get({ url: "https://i.haojima.net", name: "userId" }, function (cookie) {
        if (!cookie.value) {
            alert("请先登录爱收藏登录");
            window.open("https://i.haojima.net");
            return;
        }
        $.ajax({
            url: "https://i.haojima.net/api/LoveCollection/AddCollectionByCRX",
            data: { "url": url, "userToken": cookie.value },
            type: "post",
            success: function (sData) {
                $.isFunction(callBack) && callBack();
            },
            error: function (e) {
                //for (var m in e) {                       
                //}                   
            }
        });
    });
}