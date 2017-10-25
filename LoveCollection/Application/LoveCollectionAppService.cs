using LoveCollection.Dto;
using LoveCollection.Entities;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Talk.Redis;

namespace LoveCollection.Application
{
    public class LoveCollectionAppService
    {
        private readonly CollectionDBCotext collectionDBCotext;
        public LoveCollectionAppService(CollectionDBCotext collectionDBCotext)
        {
            this.collectionDBCotext = collectionDBCotext;
        }

        /// <summary>
        /// 获取用户下的所有类型
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<List<TypesOutput>> GetAllTypeAsync(int userId)
        {
            RedisHelper reids = new RedisHelper(3);
            var jsonString = await reids.GetStringAsync("type-userid:" + userId);
            if (!string.IsNullOrWhiteSpace(jsonString))
                return JsonConvert.DeserializeObject<List<TypesOutput>>(jsonString);
            await UpdateAllTypeToRedisAsync(userId);
            return await GetAllTypeAsync(userId);
        }

        /// <summary>
        /// 获取用户下所有收藏
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<List<CollectionOutput>> GetAllCollectionAsync(int userId)
        {
            RedisHelper reids = new RedisHelper(3);
            var jsonString = await reids.GetStringAsync("collection-userid:" + userId);
            if (!string.IsNullOrWhiteSpace(jsonString))
                return JsonConvert.DeserializeObject<List<CollectionOutput>>(jsonString);
            await UpdateAllCollectionToRedisAsync(userId);
            return await GetAllCollectionAsync(userId);
        }

        /// <summary>
        /// 根据id获取收藏内容
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<Collection> GetCollectionByIdAsync(int userId, int id)
        {
            return await CollectionQuery(userId).Where(t => t.Id == id).FirstOrDefaultAsync();
        }

        public async Task<object> AddCollection(Collection entity)
        {
            var entityEntry = collectionDBCotext.Collections.Add(entity);
            await collectionDBCotext.SaveChangesAsync();
            return new
            {
                id = entityEntry.Entity.Id,
                title = entity.Title,
            };
        }

        /// <summary>
        /// 新增 或 获取 类型id
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<int> GetOrAddTypeIdByUserIdAsync(string typeName, int userId)
        {
            var collectionType = TypeQuery(userId).Where(t => t.Name == typeName).FirstOrDefault();
            if (collectionType == null)
            {
                var typeSort = 0.0;
                if (await TypeQuery(userId).AnyAsync())
                    typeSort = await TypeQuery(userId).MaxAsync(t => t.Sort);
                var entityEntry = collectionDBCotext.Types.Add(new Entities.Type()
                {
                    Name = typeName,
                    UserId = userId,
                    Sort = typeSort + 1024
                });
                await collectionDBCotext.SaveChangesAsync();
                collectionType = entityEntry.Entity;
            }
            return collectionType.Id;
        }

        /// <summary>
        /// 保存链接
        /// </summary>
        /// <param name="title"></param>
        /// <param name="url"></param>
        /// <param name="typeId"></param>
        /// <param name="userId"></param>
        public async Task SaveCollectionAsync(string title, string url, int typeId, int userId,double addSort = 1024)
        {
            ////忽略 已经存在 或 已经被导入过的链接 
            //if (await GetCollectionsByUserId(userId).Where(t => t.Url == url).AnyAsync())
            //    return;
            var sort = 0.0;
            if (await CollectionQuery(userId).AnyAsync())
                sort = await CollectionQuery(userId).MaxAsync(t => t.Sort);
            var urlObj = collectionDBCotext.Collections.Add(new Collection()
            {
                Url = url,
                Title = title,
                UserId = userId,
                TypeId = typeId,
                Sort = sort + addSort
            });
        }

        /// <summary>
        /// 获取 已经存在的url集合
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<List<string>> GetCollectionUrlsByUserIdAsync(int userId)
            => await CollectionQuery(userId).Select(t => t.Url).ToListAsync();

        /// <summary>
        /// 保存
        /// </summary>
        /// <returns></returns>
        public async Task SaveChangesAsync() => await collectionDBCotext.SaveChangesAsync();

        #region 更新到redis
        /// <summary>
        /// 更新收藏类型到redis
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task UpdateAllTypeToRedisAsync(int userId)
        {
            var types = await TypeQuery(userId)
                           .OrderBy(t => t.Sort)
                           .Select(t => new TypesOutput()
                           {
                               Id = t.Id,
                               Name = t.Name
                           })
                           .ToListAsync();
            var json = JsonConvert.SerializeObject(types);
            RedisHelper reids = new RedisHelper(3);
            await reids.SetStringAsync("type-userid:" + userId, json);
        }

        /// <summary>
        /// 更新用户下的收藏内容到redis
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task UpdateAllCollectionToRedisAsync(int userId)
        {
            var collections = await CollectionQuery(userId).OrderBy(t => t.Sort)
                  .Select(t => new CollectionOutput()
                  {
                      Id = t.Id,
                      Title = t.Title,
                      Url = t.Url,
                      TypeId = t.TypeId
                  })
                  .ToListAsync();
            var json = JsonConvert.SerializeObject(collections);
            RedisHelper reids = new RedisHelper(3);
            await reids.SetStringAsync("collection-userid:" + userId, json);
        }
        #endregion

        #region IQueryable

        public IQueryable<Entities.Type> TypeQuery(int userId)
        {
            return collectionDBCotext.Types.Where(t => t.UserId == userId);
        }

        public IQueryable<Collection> CollectionQuery(int userId)
        {
            return collectionDBCotext.Collections.Where(t => t.UserId == userId);
        }

        #endregion
    }
}
