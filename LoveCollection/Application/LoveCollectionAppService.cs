using LoveCollection.Entities;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        /// 新增 或 获取 类型id
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<int> GetOrAddTypeIdByUserIdAsync(string typeName, int userId)
        {
            var collectionType = GetTypeByUserId(userId).Where(t => t.Name == typeName).FirstOrDefault();
            if (collectionType == null)
            {
                var typeSort = 0.0;
                if (await GetTypeByUserId(userId).AnyAsync())
                    typeSort = await GetTypeByUserId(userId).MaxAsync(t => t.Sort);
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
        public async Task SaveCollectionAsync(string title, string url, int typeId, int userId)
        {
            //忽略 已经存在 或 已经被导入过的链接 
            if (await GetCollectionsByUserId(userId).Where(t => t.Url == url).AnyAsync())
                return;

            var sort = 0.0;
            if (await GetCollectionsByUserId(userId).AnyAsync())
                sort = await GetCollectionsByUserId(userId).MaxAsync(t => t.Sort);
            var urlObj = collectionDBCotext.Collections.Add(new Collection()
            {
                Url = url,
                Title = title,
                UserId = userId,
                TypeId = typeId,
                Sort = sort + 1024
            });
        }

        /// <summary>
        /// 保存
        /// </summary>
        /// <returns></returns>
        public async Task SaveChangesAsync() => await collectionDBCotext.SaveChangesAsync();

        #region MyRegion

        private IQueryable<Entities.Type> GetTypeByUserId(int userId)
        {
            return collectionDBCotext.Types.Where(t => t.UserId == userId);
        }

        private IQueryable<Collection> GetCollectionsByUserId(int userId)
        {
            return collectionDBCotext.Collections.Where(t => t.UserId == userId);
        }

        #endregion
    }
}
