using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using LoveCollection.Models;
using LoveCollection.Dto;
using Microsoft.EntityFrameworkCore;

namespace LoveCollection.Controllers
{
    public class HomeController : Controller
    {
        private readonly CollectionDBCotext _collectionDBCotext;
        public static string DESKey { get; set; }

        public HomeController(CollectionDBCotext collectionDBCotext)
        {
            _collectionDBCotext = collectionDBCotext;
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
    }
}
