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

namespace LoveCollection.Controllers
{
    public class HomeController : Controller
    {
        private readonly CollectionDBCotext _collectionDBCotext;
        private readonly LoveCollectionAppService loveCollectionAppService;
        public static string DESKey { get; set; }

        public HomeController(CollectionDBCotext collectionDBCotext, LoveCollectionAppService loveCollectionAppService)
        {
            _collectionDBCotext = collectionDBCotext;
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
                ViewBag.Types = await _collectionDBCotext.Types
                       .Where(t => t.UserId == userId)
                       .OrderBy(t => t.Sort)
                       .Select(t => new TypesOutput()
                       {
                           Id = t.Id,
                           Name = t.Name
                       })
                       .ToListAsync();

                ViewBag.Collections = await _collectionDBCotext.Collections
                       .Where(t => t.UserId == userId)
                       .OrderBy(t => t.Sort)
                       .Select(t => new CollectionOutput()
                       {
                           Id = t.Id,
                           Sort = t.Sort,
                           Title = t.Title,
                           Url = t.Url,
                           TypeId = t.TypeId
                       })
                       .ToListAsync();
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
            using (var stream = file.OpenReadStream())
            {
                byte[] buffer = new byte[stream.Length];
                await stream.ReadAsync(buffer, 0, (int)stream.Length);
                var htmlString = Encoding.UTF8.GetString(buffer);
                HtmlParser htmlParser = new HtmlParser();
                foreach (var childNode in htmlParser.Parse(htmlString).QuerySelector("DL DL").ChildNodes)
                {
                    if (childNode is AngleSharp.Dom.IElement)
                    {
                        var element = childNode as AngleSharp.Dom.IElement;
                        var typeName = element.QuerySelectorAll("H3").FirstOrDefault()?.TextContent;
                        if (string.IsNullOrWhiteSpace(typeName))
                            typeName = "未分类";
                        var typeId = await loveCollectionAppService.GetOrAddTypeIdByUserIdAsync(typeName, userId);
                        var collections = element.QuerySelectorAll("A").ToList();
                        foreach (var collection in collections)
                        {
                            var url = collection.Attributes.FirstOrDefault(f => f.Name == "href")?.Value;
                            url = url.Length >= 500 ? url.Substring(0, 499) : url;
                            var value = collection.TextContent;
                            await loveCollectionAppService.SaveCollectionAsync(value, url, typeId, userId);
                        }
                        await loveCollectionAppService.SaveChangesAsync();
                    }
                }

            }
            return View();
        }
    }
}
