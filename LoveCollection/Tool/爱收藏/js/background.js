var baseUrl = "http://i.haojima.net";
//baseUrl = "http://localhost:2728";

//右键功能
chrome.contextMenus.create({
    title: "浏览查看",
    contexts: ['page'],
    onclick: function (info, tab) {
        window.open(baseUrl);
    }
});


chrome.contextMenus.create({
    title: "添加到默认分类",
    contexts: ['page'],
    onclick: function (info, tab) {
        collection(info, function (urlValue, userToken) {           
            chrome.tabs.executeScript(tab.id, { code: 'addCollection("' + urlValue + '","' + userToken + '");' });
        });
    }
});

chrome.contextMenus.create({
    title: "添加到自定义分类",
    contexts: ['page', 'link'],
    onclick: function (info, tab) {
        collection(info, function (urlValue, userToken) {
            chrome.tabs.executeScript(tab.id, { code: 'loadType("' + urlValue + '","' + userToken + '");' });
        });
    }
});

function collection(info, callBack) {
    var url = info["linkUrl"] || info["pageUrl"];
    chrome.cookies.get({ url: baseUrl, name: "userId" }, function (cookie) {
        if (cookie === null || !cookie.value) {
            alert("请先登录爱收藏");
            window.open(baseUrl);
            return;
        }
        $.isFunction(callBack) && callBack(url, cookie.value);       
    });
}