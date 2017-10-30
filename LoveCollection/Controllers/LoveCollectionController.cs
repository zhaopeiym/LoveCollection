using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using AngleSharp.Parser.Html;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Web;
using Microsoft.AspNetCore.Http;
using Talk.Redis;
using Microsoft.AspNetCore.Cors;
using LoveCollection.Application;
using LoveCollection.EntityFramework.EntityFramework;
using LoveCollection.Core.Entities;
using LoveCollection.Application.Dto;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace LoveCollection.Controllers
{

    [Route("api/[controller]/[action]")]
    public class LoveCollectionController : Controller
    {
        public static string DESKey { get; set; }
        private readonly CollectionDBCotext _collectionDBCotext;
        private readonly LoveCollectionAppService loveCollectionAppService;
        public LoveCollectionController(CollectionDBCotext collectionDBCotext, LoveCollectionAppService loveCollectionAppService)
        {
            _collectionDBCotext = collectionDBCotext;
            this.loveCollectionAppService = loveCollectionAppService;
            if (string.IsNullOrWhiteSpace(DESKey))
                DESKey = ConfigurationManager.GetSection("DESKey");
        }

        #region 注释
        /// <summary>
        /// 获取内容集合根据类型id
        /// </summary>
        /// <returns></returns>
        //[HttpGet]
        //public async Task<List<CollectionOutput>> GetCollectionByTypeId(int typeId)
        //{
        //    var userId = GetUserId();
        //    return await _collectionDBCotext.Collections
        //           .Where(t => t.UserId == userId && t.TypeId == typeId)
        //           .OrderBy(t => t.Sort)
        //           .Select(t => new CollectionOutput()
        //           {
        //               Id = t.Id,
        //               Sort = t.Sort,
        //               Title = t.Title,
        //               Url = t.Url
        //           })
        //           .ToListAsync();
        //} 
        #endregion

        //获取所有的收藏
        public async Task<List<CollectionOutput>> GetCollections()
        {
            var userId = GetUserId();
            return await loveCollectionAppService.GetAllCollectionAsync(userId);
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
            var userId = GetUserId();
            var collection = await loveCollectionAppService.GetCollectionByIdAsync(userId, id);
            if (nextid == 0 && previd != 0)//最后面
            {
                var tempSort = await loveCollectionAppService.CollectionQuery(userId)
                    .Where(t => t.Id == previd).Select(t => t.Sort).FirstAsync();
                collection.Sort = tempSort + 1;
            }
            else if (nextid != 0 && previd == 0)//最前面
            {
                var tempSort = await loveCollectionAppService.CollectionQuery(userId)
                    .Where(t => t.Id == nextid).Select(t => t.Sort).FirstAsync();
                collection.Sort = tempSort / 2;
            }
            else
            {
                var sort = await loveCollectionAppService.CollectionQuery(userId)
                    .Where(t => t.Id == nextid || t.Id == previd).SumAsync(t => t.Sort);
                if (sort == 0)
                    collection.Sort = 1024;
                else
                    collection.Sort = sort / 2;
            }
            collection.TypeId = typeId;
            await loveCollectionAppService.SaveChangesAsync();
            await loveCollectionAppService.UpdateAllCollectionToRedisAsync(userId);
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
            if (await loveCollectionAppService.CollectionQuery(userId).AnyAsync(t => t.Url == url))
                return string.Empty;
            return await AddCollectionByUserAndType(url, userId, typeId);
        }

        /// <summary>
        /// 插件提交保存
        /// </summary>
        /// <param name="url"></param>
        /// <param name="userToken"></param>
        /// <returns></returns>
        [EnableCors("AllowSameDomain")]
        [HttpPost]
        public async Task<object> AddCollectionByCRX(string url, string title = null, string userToken = null, int? typeId = null)
        {
            userToken = userToken == null ? null : HttpUtility.UrlDecode(userToken);
            var userId = GetUserId(userToken);
            var collection = await loveCollectionAppService.CollectionQuery(userId).Where(t => t.Url == url).FirstOrDefaultAsync();
            if (collection != null)
            {
                if (typeId.HasValue)
                    collection.TypeId = typeId.Value;
                await loveCollectionAppService.SaveChangesAsync();
                await loveCollectionAppService.UpdateAllCollectionToRedisAsync(userId);
                return string.Empty;
            }
            //if (await loveCollectionAppService.CollectionQuery(userId).AnyAsync(t => t.Url == url))
            //    return string.Empty;
            if (!typeId.HasValue)
            {
                typeId = await loveCollectionAppService.TypeQuery(userId)
                   .OrderBy(t => t.Sort)
                   .Select(t => t.Id)
                   .FirstOrDefaultAsync();
            }
            var obj = await AddCollectionByUserAndType(url, userId, typeId.Value, title);
            return obj;
        }

        /// <summary>
        /// 保存
        /// </summary>
        /// <param name="url"></param>
        /// <param name="userId"></param>
        /// <param name="typeId"></param>
        /// <returns></returns>
        private async Task<object> AddCollectionByUserAndType(string url, int userId, int typeId, string titleName = null)
        {
            using (HttpClient http = new HttpClient())
            {
                var title = url;
                //如果直接传了名称过来，则不去主动获取
                if (!string.IsNullOrWhiteSpace(titleName))
                    title = titleName;
                else
                {
                    try
                    {
                        var htmlString = await http.GetStringAsync(url);
                        HtmlParser htmlParser = new HtmlParser();
                        title = htmlParser.Parse(htmlString)
                            .QuerySelector("title")?.TextContent ?? url;
                    }
                    catch (Exception) { }
                }
                title = title.Split('-')[0];
                var sort = 0.0;
                if (await loveCollectionAppService.CollectionQuery(userId).AnyAsync())
                    sort = await loveCollectionAppService.CollectionQuery(userId).MaxAsync(t => t.Sort);
                var collection = new Collection()
                {
                    Url = url,
                    Title = title,
                    UserId = userId,
                    TypeId = typeId,
                    Sort = sort + 1024
                };
                var obj = await loveCollectionAppService.AddCollection(collection);
                await loveCollectionAppService.UpdateAllCollectionToRedisAsync(userId);
                return obj;
            }
        }

        /// <summary>
        /// 修改 Url 内容
        /// </summary>
        /// <param name="id"></param>
        /// <param name="url"></param>
        /// <param name="title"></param>
        /// <returns></returns>

        public async Task ModifyCollection(int id, string url, string title, int? typeId = null)
        {
            var userId = GetUserId();
            try
            {
                var collection = await loveCollectionAppService.CollectionQuery(userId)
               .Where(t => t.Id == id)
               .FirstOrDefaultAsync();
                collection.Url = url;
                collection.Title = title;
                if (typeId.HasValue)
                    collection.TypeId = typeId.Value;
                await _collectionDBCotext.SaveChangesAsync();
            }
            catch (Exception)
            {
                await loveCollectionAppService.UpdateAllTypeToRedisAsync(userId);
                throw;
            }
            finally
            {
                await loveCollectionAppService.UpdateAllCollectionToRedisAsync(userId);
            }
        }

        /// <summary>
        /// 获取url的类型id
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        [EnableCors("AllowSameDomain")]
        public async Task<int> GetTypeIdByByUrlCRX(string url, string userToken = null)
        {
            var userId = GetUserId(userToken);
            int typeId = await loveCollectionAppService.CollectionQuery(userId)
                .Where(t => t.Url == url)
                .Select(t => t.TypeId)
                .FirstOrDefaultAsync();
            return typeId;
        }

        /// <summary>
        /// 删除 url 内容
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task DelCollection(int id)
        {
            var userId = GetUserId();
            var collection = await loveCollectionAppService.CollectionQuery(userId)
                .Where(t => t.Id == id)
                .FirstOrDefaultAsync();
            //_collectionDBCotext.Collections.Where(t => t.Id == id).FirstOrDefaultAsync();
            _collectionDBCotext.Entry(collection).State = EntityState.Deleted;
            await _collectionDBCotext.SaveChangesAsync();
            await loveCollectionAppService.UpdateAllCollectionToRedisAsync(userId);

        }

        /// <summary>
        /// 添加类型
        /// </summary>
        /// <param name="name"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        [EnableCors("AllowSameDomain")]
        public async Task<int> AddType(string name, string userToken = null)
        {
            var userId = GetUserId(userToken);
            var typeSort = 0.0;
            if (await loveCollectionAppService.TypeQuery(userId).AnyAsync())
                typeSort = await loveCollectionAppService.TypeQuery(userId).MaxAsync(t => t.Sort);
            var type = _collectionDBCotext.Types.Add(new Core.Entities.Type()
            {
                Name = name,
                UserId = userId,
                Sort = typeSort + 1024
            });
            await _collectionDBCotext.SaveChangesAsync();
            await loveCollectionAppService.UpdateAllTypeToRedisAsync(userId);
            return type.Entity.Id;
        }

        /// <summary>
        /// 获取类型集合
        /// </summary>
        /// <returns></returns>
        [EnableCors("AllowSameDomain")]
        [HttpGet]
        public async Task<List<TypesOutput>> GetTypes(string userToken = null)
        {
            var userId = GetUserId(userToken);
            return await loveCollectionAppService.GetAllTypeAsync(userId);
        }

        /// <summary>
        /// 修改类型名字
        /// </summary>
        /// <param name="typeId"></param>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public async Task ModifyTypeNameById(int typeId, string typeName)
        {
            var userId = GetUserId();
            try
            {
                (await loveCollectionAppService.TypeQuery(userId).Where(t => t.Id == typeId).FirstAsync()).Name = typeName;
                await _collectionDBCotext.SaveChangesAsync();
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                await loveCollectionAppService.UpdateAllTypeToRedisAsync(userId);
            }
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
            var userId = GetUserId();
            var collection = await loveCollectionAppService.TypeQuery(userId).Where(t => t.Id == id).FirstOrDefaultAsync();
            if (nextid == 0 && previd != 0)//最下面
            {
                var tempSort = await loveCollectionAppService.TypeQuery(userId).Where(t => t.Id == previd).Select(t => t.Sort).FirstAsync();
                collection.Sort = tempSort + 1;
            }
            else if (nextid != 0 && previd == 0)//最上面
            {
                var tempSort = await loveCollectionAppService.TypeQuery(userId).Where(t => t.Id == nextid).Select(t => t.Sort).FirstAsync();
                collection.Sort = tempSort / 2;
            }
            else
            {
                var sort = await loveCollectionAppService.TypeQuery(userId).Where(t => t.Id == nextid || t.Id == previd).SumAsync(t => t.Sort);
                collection.Sort = sort / 2;
            }

            await _collectionDBCotext.SaveChangesAsync();
            await loveCollectionAppService.UpdateAllTypeToRedisAsync(userId);
        }

        /// <summary>
        /// 删除类型和类型下的所有链接
        /// </summary>
        /// <param name="typeId"></param>
        /// <returns></returns>
        public async Task DelType(int typeId)
        {
            var userId = GetUserId();
            var collections = await loveCollectionAppService.CollectionQuery(userId).Where(t => t.UserId == userId && t.TypeId == typeId).ToListAsync();
            foreach (var collection in collections)
            {
                _collectionDBCotext.Entry(collection).State = EntityState.Deleted;
            }

            var type = await loveCollectionAppService.TypeQuery(userId).Where(t => t.Id == typeId).FirstOrDefaultAsync();
            _collectionDBCotext.Entry(type).State = EntityState.Deleted;

            await _collectionDBCotext.SaveChangesAsync();
            await loveCollectionAppService.UpdateAllTypeToRedisAsync(userId);

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

                RedisHelper reids = new RedisHelper(3);
                var key = mail;
                var number = await reids.GetStringIncrAsync(key);
                if (number >= 3)
                {
                    requestMessage.Message = "请勿频繁注册，请查看垃圾邮件或换一个邮箱注册！";
                    return requestMessage;
                }
                //30分钟内有效(标记邮件激活30分钟内有效)
                await reids.SetStringIncrAsync(key, TimeSpan.FromMinutes(30));

                if (user == null)
                    requestMessage.Message = "第一次登录，验证链接已发邮箱。";
                else
                    requestMessage.Message = "您的密码有变更，验证链接已发邮箱。";

                var data = JsonConvert.SerializeObject(new User() { Mail = mail, Passwod = passwod });
                var DESString = HttpUtility.UrlEncode(EncryptDecryptExtension.DES3Encrypt(data, DESKey));
                EmailHelper email = new EmailHelper();
                email.MailToArray = new string[] { mail };
                var checkUrl = Request.Scheme + "://" + Request.Host.Value + "/Home/CheckLogin?desstring=" + DESString;
                email.MailSubject = "欢迎您注册 爱收藏";
                email.MailBody = EmailHelper.TempBody(mail, "请复制打开链接(或者右键'在新标签页中打开')，完成验证。", "<a style='word-wrap: break-word;word-break: break-all;' href='" + checkUrl + "'>" + checkUrl + "</a>");
                email.Send(t =>
                {
                    //string aa = "成功";
                }, t =>
                {
                    //string aa = "失败";
                });
            }
            else
            {
                SaveCookie(new Core.Entities.User() { Id = user.Id, Mail = mail });
            }
            return requestMessage;
        }

        /// <summary>
        /// 验证登录（密码错误和未注册用户）
        /// </summary>
        /// <param name="desstring"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<RequestMessage> CheckLogin(string desstring)
        {
            var requestMessage = new RequestMessage();
            var jsonString = string.Empty;
            try
            {
                //这里有点妖啊。
                //如果是url直接跳转过来的就不需要HttpUtility.UrlDecode
                //如果是ajax异步传过来的就需要HttpUtility.UrlDecode
                jsonString = EncryptDecryptExtension.DES3Decrypt(HttpUtility.UrlDecode(desstring), DESKey);
            }
            catch (Exception)
            {
                jsonString = EncryptDecryptExtension.DES3Decrypt(desstring, DESKey);
            }
            var dataUser = JsonConvert.DeserializeObject<User>(jsonString);

            RedisHelper reids = new RedisHelper(3);
            if (!await reids.KeyExistsAsync(dataUser.Mail, RedisTypePrefix.String))
            {
                requestMessage.IsSuccess = false;
                requestMessage.Message = "激活链接已失效";
                return requestMessage;//
            }

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
                _collectionDBCotext.Types.Add(new Core.Entities.Type() { Name = "稍候阅读", UserId = user.Id, Sort = 1024 });
            }
            await _collectionDBCotext.SaveChangesAsync();
            SaveCookie(user);

            await reids.DeleteKeyAsync(dataUser.Mail, RedisTypePrefix.String);//删除缓存，使验证过的邮件失效            
            return requestMessage;
        }

        /// <summary>
        /// 保存cookie
        /// </summary>
        /// <param name="user"></param>
        private void SaveCookie(User user)
        {
            Response.Cookies.Append("userName", user.Mail,
                new CookieOptions()
                {
                    Expires = new DateTimeOffset(DateTime.Now.AddYears(1)),
                    HttpOnly = true
                });
            //这里好像做了自动url编码
            Response.Cookies.Append("userId", EncryptDecryptExtension.DES3Encrypt(user.Id.ToString(), DESKey),
                new CookieOptions()
                {
                    Expires = new DateTimeOffset(DateTime.Now.AddMonths(1)),
                    HttpOnly = true
                });
        }

        /// <summary>
        /// 获取登录用户id
        /// </summary>
        /// <returns></returns>
        private int GetUserId(string userId = null)
        {
            var userIdCookie = userId ?? Request.Cookies.First(t => t.Key == "userId").Value;
            var userIdString = EncryptDecryptExtension.DES3Decrypt(userIdCookie, DESKey);
            return int.Parse(userIdString);
        }
    }
}
