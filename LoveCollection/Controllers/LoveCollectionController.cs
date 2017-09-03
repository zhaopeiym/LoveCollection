using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using AngleSharp.Parser.Html;
using Microsoft.EntityFrameworkCore;
using LoveCollection.Entities;
using LoveCollection.Dto;
using Newtonsoft.Json;
using System.Web;
using Microsoft.AspNetCore.Http;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace LoveCollection.Controllers
{

    [Route("api/[controller]/[action]")]
    public class LoveCollectionController : Controller
    {
        public static string DESKey { get; set; }
        private readonly CollectionDBCotext _collectionDBCotext;
        public LoveCollectionController(CollectionDBCotext collectionDBCotext)
        {
            _collectionDBCotext = collectionDBCotext;
            if (string.IsNullOrWhiteSpace(DESKey))
                DESKey = ConfigurationManager.GetSection("DESKey");
        }

        /// <summary>
        /// 获取内容集合根据类型id
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<List<CollectionOutput>> GetCollectionByTypeId(int typeId)
        {
            var userId = GetUserId();
            return await _collectionDBCotext.Collections
                   .Where(t => t.UserId == userId && t.TypeId == typeId)
                   .OrderBy(t => t.Sort)
                   .Select(t => new CollectionOutput()
                   {
                       Id = t.Id,
                       Sort = t.Sort,
                       Title = t.Title,
                       Url = t.Url
                   })
                   .ToListAsync();
        }

        public async Task<List<CollectionOutput>> GetCollections()
        {
            var userId = GetUserId();
            return await _collectionDBCotext.Collections
                   .Where(t => t.UserId == userId)
                   .OrderBy(t => t.Sort)
                   .Select(t => new CollectionOutput()
                   {
                       Id = t.Id,
                       Sort = t.Sort,
                       Title = t.Title,
                       Url = t.Url
                   })
                   .ToListAsync();
        }

        /// <summary>
        /// 移动位置排序
        /// </summary>
        /// <param name="id"></param>
        /// <param name="nextid"></param>
        /// <param name="previd"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task ModifySort(int id, int typeId, int nextid, int previd)
        {
            var collection = await _collectionDBCotext.Collections.Where(t => t.Id == id).FirstOrDefaultAsync();
            if (nextid == 0 && previd != 0)//最后面
            {
                var tempSort = await _collectionDBCotext.Collections.Where(t => t.Id == previd).Select(t => t.Sort).FirstAsync();
                collection.Sort = tempSort + 1;
            }
            else if (nextid != 0 && previd == 0)//最前面
            {
                var tempSort = await _collectionDBCotext.Collections.Where(t => t.Id == nextid).Select(t => t.Sort).FirstAsync();
                collection.Sort = tempSort / 2;
            }
            else
            {
                var sort = await _collectionDBCotext.Collections.Where(t => t.Id == nextid || t.Id == previd).SumAsync(t => t.Sort);
                collection.Sort = sort / 2;
            }
            collection.TypeId = typeId;
            await _collectionDBCotext.SaveChangesAsync();
        }

        /// <summary>
        /// 新增 Url 内容
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<object> AddCollection(string url, int typeId)
        {
            var userId = GetUserId();
            if (await _collectionDBCotext.Collections.AnyAsync(t => t.Url == url))
                return string.Empty;

            using (HttpClient http = new HttpClient())
            {
                var htmlString = await http.GetStringAsync(url);
                HtmlParser htmlParser = new HtmlParser();
                var title = htmlParser.Parse(htmlString)
                    .QuerySelector("title")?.TextContent ?? url;

                var sort = 0.0;
                if (await _collectionDBCotext.Collections.AnyAsync(t => t.UserId == userId))
                    sort = await _collectionDBCotext.Collections.Where(t => t.UserId == userId).MaxAsync(t => t.Sort);
                var urlObj = _collectionDBCotext.Collections.Add(new Collection()
                {
                    Url = url,
                    Title = title,
                    UserId = userId,
                    TypeId = typeId,
                    Sort = sort + 1024
                });
                await _collectionDBCotext.SaveChangesAsync();
                return new
                {
                    id = urlObj.Entity.Id,
                    title = title,
                };
            }
        }

        /// <summary>
        /// 修改 Url 内容
        /// </summary>
        /// <param name="id"></param>
        /// <param name="url"></param>
        /// <param name="title"></param>
        /// <returns></returns>

        public async Task ModifyCollection(int id, string url, string title)
        {
            var collection = await _collectionDBCotext.Collections.Where(t => t.Id == id).FirstOrDefaultAsync();
            collection.Url = url;
            collection.Title = title;
            await _collectionDBCotext.SaveChangesAsync();
        }

        /// <summary>
        /// 删除 url 内容
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task DelCollection(int id)
        {
            var collection = await _collectionDBCotext.Collections.Where(t => t.Id == id).FirstOrDefaultAsync();
            _collectionDBCotext.Entry(collection).State = EntityState.Deleted;
            await _collectionDBCotext.SaveChangesAsync();
        }

        /// <summary>
        /// 添加类型
        /// </summary>
        /// <param name="name"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<int> AddType(string name)
        {
            var userId = GetUserId();
            var typeSort = 0.0;
            if (await _collectionDBCotext.Types.AnyAsync(t => t.UserId == userId))
                typeSort = await _collectionDBCotext.Types.Where(t => t.UserId == userId).MaxAsync(t => t.Sort);
            var type = _collectionDBCotext.Types.Add(new Entities.Type()
            {
                Name = name,
                UserId = userId,
                Sort = typeSort + 1024
            });
            await _collectionDBCotext.SaveChangesAsync();
            return type.Entity.Id;
        }

        /// <summary>
        /// 获取类型集合
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<List<TypesOutput>> GetTypes()
        {
            var userId = GetUserId();
            return await _collectionDBCotext.Types
                   .Where(t => t.UserId == userId)
                   .OrderBy(t => t.Sort)
                   .Select(t => new TypesOutput()
                   {
                       Id = t.Id,
                       Name = t.Name
                   })
                   .ToListAsync();
        }

        /// <summary>
        /// 修改类型名字
        /// </summary>
        /// <param name="typeId"></param>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public async Task ModifyTypeNameById(int typeId, string typeName)
        {
            (await _collectionDBCotext.Types.Where(t => t.Id == typeId).FirstAsync()).Name = typeName;
            await _collectionDBCotext.SaveChangesAsync();
        }


        /// <summary>
        /// 修改类型排序
        /// </summary>
        /// <param name="id"></param>
        /// <param name="nextid"></param>
        /// <param name="previd"></param>
        /// <returns></returns>
        public async Task ModifyTypeSort(int id, int nextid, int previd)
        {
            var collection = await _collectionDBCotext.Types.Where(t => t.Id == id).FirstOrDefaultAsync();
            if (nextid == 0 && previd != 0)//最下面
            {
                var tempSort = await _collectionDBCotext.Types.Where(t => t.Id == previd).Select(t => t.Sort).FirstAsync();
                collection.Sort = tempSort + 1;
            }
            else if (nextid != 0 && previd == 0)//最上面
            {
                var tempSort = await _collectionDBCotext.Types.Where(t => t.Id == nextid).Select(t => t.Sort).FirstAsync();
                collection.Sort = tempSort / 2;
            }
            else
            {
                var sort = await _collectionDBCotext.Types.Where(t => t.Id == nextid || t.Id == previd).SumAsync(t => t.Sort);
                collection.Sort = sort / 2;
            }

            await _collectionDBCotext.SaveChangesAsync();
        }

        /// <summary>
        /// 删除类型和类型下的所有链接
        /// </summary>
        /// <param name="typeId"></param>
        /// <returns></returns>
        public async Task DelType(int typeId)
        {
            var userId = GetUserId();
            var collections = await _collectionDBCotext.Collections.Where(t => t.UserId == userId && t.TypeId == typeId).ToListAsync();
            foreach (var collection in collections)
            {
                _collectionDBCotext.Entry(collection).State = EntityState.Deleted;
            }

            var type = await _collectionDBCotext.Types.Where(t => t.Id == typeId).FirstOrDefaultAsync();
            _collectionDBCotext.Entry(type).State = EntityState.Deleted;

            await _collectionDBCotext.SaveChangesAsync();
        }

        /// <summary>
        /// 登录、注册
        /// </summary>
        /// <param name="mail"></param>
        /// <param name="passwod"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<RequestMessage> Register(string mail, string passwod)
        {
            var requestMessage = new RequestMessage() { IsSuccess = true };
            var user = await _collectionDBCotext.Users
                .Where(t => t.Mail == mail)
                .Select(t => new { t.Id, t.Passwod })
                .FirstOrDefaultAsync();

            if (user == null || user.Passwod != passwod)//注册 或 修改密码
            {
                requestMessage.IsSuccess = false;
                if (user == null)
                    requestMessage.Message = "第一次登录，验证链接已发邮箱。";
                else
                    requestMessage.Message = "您的密码有变更，验证链接已发邮箱。";

                var data = JsonConvert.SerializeObject(new User() { Mail = mail, Passwod = passwod });
                var DESString = HttpUtility.UrlEncode(EncryptDecryptExtension.DES3Encrypt(data, DESKey));
                EmailHelper email = new EmailHelper();
                email.MailToArray = new string[] { mail };
                var checkUrl = Request.Host.Value + "/api/LoveCollection/CheckLogin?desstring=" + DESString;
                email.MailBody = EmailHelper.TempBody(mail, "请复制打开链接(或者右键新标签中打开)，完成验证。", "<a style='word-wrap: break-word;word-break: break-all;' href='http://" + checkUrl + "'>"+ checkUrl + "</a>");
                email.Send();
            }
            else
            {
                SaveCookie(new Entities.User() { Id = user.Id, Mail = mail });
            }
            return requestMessage;
        }

        /// <summary>
        /// 验证登录（密码错误和未注册用户）
        /// </summary>
        /// <param name="desstring"></param>
        /// <returns></returns>
        public async Task CheckLogin(string desstring)
        {
            var jsonString = EncryptDecryptExtension.DES3Decrypt(desstring, DESKey);
            var dataUser = JsonConvert.DeserializeObject<User>(jsonString);
            var user = await _collectionDBCotext.Users.Where(t => t.Mail == dataUser.Mail).FirstOrDefaultAsync();
            if (user != null)//修改密码
            {
                user.Passwod = dataUser.Passwod;
            }
            else//新增用户
            {
                user = dataUser;
                _collectionDBCotext.Users.Add(user);
                await _collectionDBCotext.SaveChangesAsync();
                _collectionDBCotext.Types.Add(new Entities.Type() { Name = "常用链接", UserId = user.Id, Sort = 1024 });
            }
            await _collectionDBCotext.SaveChangesAsync();
            SaveCookie(user);
            Response.Redirect("/");
        }

        /// <summary>
        /// 保存cookie
        /// </summary>
        /// <param name="user"></param>
        private void SaveCookie(User user)
        {
            Response.Cookies.Append("userName", user.Mail, new CookieOptions() { Expires = new DateTimeOffset(DateTime.Now.AddYears(1)) });
            Response.Cookies.Append("userId", EncryptDecryptExtension.DES3Encrypt(user.Id.ToString(), DESKey), new CookieOptions()
            {
                Expires = new DateTimeOffset(DateTime.Now.AddMonths(1))
            });
        }

        /// <summary>
        /// 获取登录用户id
        /// </summary>
        /// <returns></returns>
        private int GetUserId()
        {
            var userIdCookie = Request.Cookies.First(t => t.Key == "userId").Value;
            var userIdString = EncryptDecryptExtension.DES3Decrypt(userIdCookie, DESKey);
            return int.Parse(userIdString);
        }
    }
}
