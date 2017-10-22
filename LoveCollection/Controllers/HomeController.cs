using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using LoveCollection.Models;
using LoveCollection.Dto;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Text;
using AngleSharp.Parser.Html;
using LoveCollection.Application;
using Newtonsoft.Json;

namespace LoveCollection.Controllers
{
    public class HomeController : Controller
    {

        private readonly LoveCollectionAppService loveCollectionAppService;
        public static string DESKey { get; set; }

        public HomeController(LoveCollectionAppService loveCollectionAppService)
        {
            this.loveCollectionAppService = loveCollectionAppService;
            if (string.IsNullOrWhiteSpace(DESKey))
                DESKey = ConfigurationManager.GetSection("DESKey");
        }
        public async Task<IActionResult> Index()
        {
            var userId = GetUserId();
            ViewBag.UserInfo = new UserInfoModel()
            {
                UserMail = Request.Cookies.FirstOrDefault(t => t.Key == "userName").Value,
                UserId = Request.Cookies.FirstOrDefault(t => t.Key == "userId").Value
            };

            if (userId > 0)
            {
                ViewBag.Types = JsonConvert.SerializeObject(await loveCollectionAppService.GetAllTypeAsync(userId));
                ViewBag.Collections = JsonConvert.SerializeObject(await loveCollectionAppService.GetAllCollectionAsync(userId));
            }
            return View();
        }

        private int GetUserId()
        {
            var userIdCookie = Request.Cookies.FirstOrDefault(t => t.Key == "userId").Value;
            if (string.IsNullOrWhiteSpace(userIdCookie))
                return 0;
            var userIdString = EncryptDecryptExtension.DES3Decrypt(userIdCookie, DESKey);
            return int.Parse(userIdString);
        }

        /// <summary>
        /// 关于
        /// </summary>
        /// <returns></returns>
        public IActionResult About()
        {
            return View();
        }

        public IActionResult CheckLogin(string desstring)
        {
            return View();
        }

        public IActionResult LogOff()
        {
            Response.Cookies.Delete("userId");
            return Redirect("/");
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        /// <summary>
        /// 导入书签
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> ImportBookmark(IFormFile file)
        {
            var userId = GetUserId();
            if (userId == 0)
                return Redirect("/");
            using (var stream = file.OpenReadStream())
            {
                byte[] buffer = new byte[stream.Length];
                await stream.ReadAsync(buffer, 0, (int)stream.Length);
                var htmlString = Encoding.UTF8.GetString(buffer);
                HtmlParser htmlParser = new HtmlParser();
                var urls = await loveCollectionAppService.GetCollectionUrlsByUserIdAsync(userId);
                var tempTypeId = await loveCollectionAppService.GetOrAddTypeIdByUserIdAsync("未分类", userId);

                #region 按类型导入
                foreach (var childNode in htmlParser.Parse(htmlString).QuerySelector("DL DL").ChildNodes)
                {
                    if (childNode is AngleSharp.Dom.IElement)
                    {
                        var element = childNode as AngleSharp.Dom.IElement;
                        var typeName = element.QuerySelectorAll("H3").FirstOrDefault()?.TextContent;
                        var typeId = tempTypeId;
                        if (!string.IsNullOrWhiteSpace(typeName))
                            typeId = await loveCollectionAppService.GetOrAddTypeIdByUserIdAsync(typeName, userId);
                        var collections = element.QuerySelectorAll("A").ToList();
                        foreach (var collection in collections)
                        {
                            var url = collection.Attributes.FirstOrDefault(f => f.Name == "href")?.Value;
                            url = url.Length >= 500 ? url.Substring(0, 500) : url;
                            if (urls.Contains(url))//忽略 已经存在 或 已经被导入过的链接 
                                continue;
                            var value = collection.TextContent;
                            value = value.Length >= 300 ? value.Substring(0, 300) : value;
                            await loveCollectionAppService.SaveCollectionAsync(value, url, typeId, userId);
                            urls.Add(url);
                        }
                        await loveCollectionAppService.SaveChangesAsync();
                    }
                }
                #endregion

                #region 重新检测漏网之鱼
                foreach (var collection in htmlParser.Parse(htmlString).QuerySelectorAll("A"))
                {
                    var url = collection.Attributes.FirstOrDefault(f => f.Name == "href")?.Value;
                    url = url.Length >= 500 ? url.Substring(0, 500) : url;
                    if (urls.Contains(url))//忽略 已经存在 或 已经被导入过的链接 
                        continue;
                    var value = collection.TextContent;
                    value = value.Length >= 300 ? value.Substring(0, 300) : value;
                    await loveCollectionAppService.SaveCollectionAsync(value, url, tempTypeId, userId);
                    urls.Add(url);
                }
                await loveCollectionAppService.SaveChangesAsync();
                #endregion
            }
            await loveCollectionAppService.UpdateAllCollectionToRedisAsync(userId);
            await loveCollectionAppService.UpdateAllTypeToRedisAsync(userId);
            return View();
        }
    }
}
