using LoveCollection.EntityFramework.EntityFramework;
using System;
using System.Linq;

namespace Tool
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Write("请输入数据库连接字符串：");
            var connection = Console.ReadLine();
            CollectionDBCotext collectionDBCotext = new CollectionDBCotext(connection);

            var userIds = collectionDBCotext.Users.Select(t => t.Id).ToList();
            foreach (var userId in userIds)
            {
                var typeIds = collectionDBCotext.Types.Where(t => t.UserId == userId).Select(t => t.Id).ToList();
                foreach (var typeId in typeIds)
                {
                    var colletions = collectionDBCotext.Collections.Where(t => t.UserId == userId && t.TypeId == typeId).ToList();
                    var sort = 1;
                    foreach (var colletion in colletions)
                    {
                        colletion.Sort = sort++ * 1024;
                        Console.WriteLine("修正一条");
                    }
                }
            }
            collectionDBCotext.SaveChanges();
            Console.WriteLine("已全部修正");
        }
    }
}
