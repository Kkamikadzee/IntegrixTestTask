using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using DbLib;
using DbLib.IntegrixContext.Models;
using Microsoft.EntityFrameworkCore;

namespace ConsoleLibTest
{
    public class TestThread
    {
        private class TestParamers
        {
            public int CountTests { get; set; }
            public Product Product { get; set; }
            public int AmountMin { get; set; }
            public int AmountMax { get; set; }
        }

        private User _user;
        private Warehouse _warehouse;

        private string[] _resultMessages;

        private Thread _thread;

        public Thread Thread => _thread;

        public TestThread(User user, Warehouse warehouse)
        {
            _user = user;
            _warehouse = warehouse;
        }
        private string ReserveHendler(User user, Product product, Warehouse warehouse, int amount)
        {
            string result = string.Empty;
            using (var db = new IntegrixContext())
            {
                var strategy = db.Database.CreateExecutionStrategy();

                strategy.Execute(() =>
                {
                    using (var transaction = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
                    {
                        try
                        {
                            var piiw = db.ProductInfoInWarehouses.
                                FirstOrDefault(value =>
                                (value.ProductId == product.ProductId) &&
                                (value.WarehouseId == warehouse.WarehouseId));

                            if (piiw == null)
                            {
                                throw new Exception("Selected product not found in selected warehouse.");
                            }
                            else if (piiw.QuantityFreeProduct < amount)
                            {
                                throw new Exception
                                    ("The selected warehouse does not have the required quantity of the product.");
                            }

                            piiw.QuantityFreeProduct -= amount;
                            piiw.QuantityReservedProduct += amount;

                            db.SaveChanges();

                            var order = new Order
                            {
                                UserId = user.UserId,
                                OrderData = DateTimeOffset.Now.ToUniversalTime().DateTime
                            };
                            db.Orders.Add(order);

                            db.SaveChanges();

                            var orderItem = new OrderItem
                            {
                                ProductId = product.ProductId,
                                OrderId = order.OrderId,
                                Quantity = amount
                            };
                            db.OrderItems.Add(orderItem);

                            db.SaveChanges();

                            transaction.Commit();

                            result = "Successful.";
                        }
                        catch(Exception ex)
                        {
                            transaction.Rollback();
                            result = ("Failed to reserve the product. " + ex.Message);
                        }
                    }
                });
            }

            return result;
        }
        public string Reserve(Product product, int amount)
        {
            return ReserveHendler(_user, product, _warehouse, amount);
        }


        private void Test(object param)
        {
            var parameters = param as TestParamers;

            _resultMessages = new string[parameters.CountTests];

            var random = new Random();
            for (int i = 0; i < parameters.CountTests; i++)
            {
                try
                {
                    _resultMessages[i] = Reserve(parameters.Product, random.Next(parameters.AmountMin, parameters.AmountMax + 1));;
                }
                catch(Exception ex)
                {
                    _resultMessages[i] = ex.Message;
                }

                Thread.Sleep(10);
            }
        }

        public void PrintResultInFile(string path)
        {
            using (var sw = new StreamWriter(path))
            {
                for(int i = 0; i < _resultMessages.Length; i++)
                {
                    sw.WriteLine("{0}. {1}", i, _resultMessages[i]);
                }
            }
        }

        public void Start(int countTests, Product product, int amountMin, int amountMax)
        {
            var param = new TestParamers
            {
                CountTests = countTests,
                Product = product,
                AmountMin = amountMin,
                AmountMax = amountMax
            };

            _thread = new Thread(new ParameterizedThreadStart(Test));

            _thread.Start(param);
            _thread.Name = _user.Name;
        }
    }
    class Program
    {
        static void InsertData()
        {
            using (var db = new IntegrixContext())
            {
                var productA = new Product
                {
                    FullName = "RTX 3060Ti"
                };
                db.Products.Add(productA);
                db.SaveChanges();

                var someWarehouse = new Warehouse
                {
                    Name = "DNS"
                };
                db.Warehouses.Add(someWarehouse);
                db.SaveChanges();

                var dns_3060ti = new ProductInfoInWarehouse
                {
                    Product = productA,
                    Warehouse = someWarehouse,
                    QuantityFreeProduct = 100
                };
                db.ProductInfoInWarehouses.Add(dns_3060ti);
                db.SaveChanges();
            }
        }

        static void PrintProductAndTheirQuanity()
        {
            using (var db = new IntegrixContext())
            {
                var result = from product in db.Products
                             join prodInfoInWrhs in db.ProductInfoInWarehouses
                             on product.ProductId equals prodInfoInWrhs.ProductId
                             join warehouse in db.Warehouses
                             on prodInfoInWrhs.WarehouseId equals warehouse.WarehouseId
                             select new
                             {
                                 FullName = product.FullName,
                                 WarehouseName = warehouse.Name,
                                 QuantityFreeProduct = prodInfoInWrhs.QuantityFreeProduct,
                                 QuantityReservedProduct = prodInfoInWrhs.QuantityReservedProduct
                             };

                var groupedResult = from res in result.ToList()
                                    group res by res.FullName;

                foreach (var group in groupedResult)
                {
                    Console.WriteLine("Product fullname: {0}", group.Key);
                    foreach (var info in group)
                    {
                        Console.WriteLine("{0} : free-{1} reserved-{2}",
                            info.WarehouseName,
                            info.QuantityFreeProduct,
                            info.QuantityReservedProduct);
                    }
                    Console.WriteLine();
                }
            }
        }

        static void PrintUsers()
        {
            using (var db = new IntegrixContext())
            {
                var users = db.Users.ToList();
                Console.WriteLine("User list:");
                foreach (var user in users)
                {
                    Console.WriteLine($"{user.UserId}.{user.Name}");
                }
            }
        }


        static void Main(string[] args)
        {
            User[] users;
            Product product;
            Warehouse warehouse;
            PrintProductAndTheirQuanity();
            using (var db = new IntegrixContext())
            {
                users = db.Users.AsNoTracking().ToArray();
                product = db.Products.AsNoTracking().FirstOrDefault();
                warehouse = db.Warehouses.AsNoTracking().FirstOrDefault();
                //var dns_3060ti = new ProductInfoInWarehouse
                //{
                //    Product = product,
                //    Warehouse = warehouse,
                //    QuantityFreeProduct = 100
                //};
                //db.ProductInfoInWarehouses.Add(dns_3060ti);
                //db.SaveChanges();
            }

            var countThreads = 2; // users.Length

            TestThread[] testThreads = new TestThread[countThreads];

            for(int i = 0; i < countThreads; i++)
            {
                testThreads[i] = new TestThread(users[i], warehouse);
            }

            foreach(var test in testThreads)
            {
                test.Start(10, product, 1, 10);
            }

            foreach (var test in testThreads)
            {
                test.Thread.Join();
            }

            foreach (var test in testThreads)
            {
                test.PrintResultInFile($"Result\\{test.Thread.Name}.txt");
            }

            PrintProductAndTheirQuanity();
        }
    }
}
