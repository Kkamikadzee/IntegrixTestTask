using System;
using System.Collections.Generic;
using System.Configuration;
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

        private enum ReserveResultCode
        {
            Successful = 0,
            WarehouseDoesNotHaveProduct = 1,
            Error = 255
        }

        private User _user;
        private Warehouse _warehouse;

        private string[] _resultMessages;

        private Thread _thread;

        public Thread Thread => _thread;

        public IntegrixDataBase _integrixDataBase;

        public TestThread(User user, Warehouse warehouse)
        {
            _user = user;
            _warehouse = warehouse;
            _integrixDataBase = new IntegrixDataBase()
            {
                SelectedUser = user,
                SelectedWarehouse = warehouse
            };
        }
        private string ReserveHendler(User user, Product product, Warehouse warehouse, int amount)
        {
            ReserveResultCode resultCode;
            string resultMessage = string.Empty;
            using (var db = new IntegrixContext())
            {
                using (var transaction = db.Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted))
                {
                    try
                    {
                        int numberOfRowInserteed;

                        numberOfRowInserteed = db.Database.ExecuteSqlRaw(
                            "UPDATE public.product_info_in_warehouse" +
                            "  SET quantity_free_product=quantity_free_product-{0}, " +
                            "      quantity_reserved_product=quantity_reserved_product+{0}" +
                            "  WHERE product_id={1} and warehouse_id={2} and quantity_free_product >= {0};",
                            amount, product.ProductId, warehouse.WarehouseId);

                        db.SaveChanges();
                        transaction.Commit();

                        if (numberOfRowInserteed != 0)
                        {
                            resultCode = ReserveResultCode.Successful;
                        }
                        else
                        {
                            resultCode = ReserveResultCode.WarehouseDoesNotHaveProduct;
                        }
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        resultCode = ReserveResultCode.Error;
                        if (ex.InnerException != null)
                        {
                            resultMessage = ("Failed to reserve the product. " + ex.Message + ex.InnerException.InnerException.Message);
                        }
                        else
                        {
                            resultMessage = ("Failed to reserve the product. " + ex.Message);
                        }
                    }
                }

                if (resultCode == ReserveResultCode.Error)
                {
                    return resultMessage;
                }
                else if (resultCode == ReserveResultCode.Successful)
                {
                    using (var transaction = db.Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted))
                    {
                        try
                        {
                            var order = new Order
                            {
                                UserId = user.UserId,
                                WarehouseId = warehouse.WarehouseId,
                                OrderDate = DateTimeOffset.Now.ToUniversalTime().DateTime
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
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            resultCode = ReserveResultCode.Error;
                            resultMessage = ("Failed to reserve the product. " + ex.Message);
                        }
                    }
                }

                if (resultCode == ReserveResultCode.WarehouseDoesNotHaveProduct)
                {
                    using (var transaction = db.Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted))
                    {
                        try
                        {
                            var nri = new NotReservedItem
                            {
                                UserId = user.UserId,
                                ProductId = product.ProductId,
                                WarehouseId = warehouse.WarehouseId,
                                Quantity = amount,
                                Reason = "Selected warehouse does not have selected product.",
                                ReservationDate = DateTimeOffset.Now.ToUniversalTime().DateTime
                            };
                            db.NotReservedItems.Add(nri);

                            db.SaveChanges();
                            transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            resultCode = ReserveResultCode.Error;
                            if (ex.InnerException != null)
                            {
                                resultMessage = ("Failed to write information about a non-reserved item. " + ex.Message + ex.InnerException.InnerException.Message);
                            }
                            else
                            {
                                resultMessage = ("Failed to write information about a non-reserved item. " + ex.Message);
                            }
                        }
                    }
                }
            }

            switch (resultCode)
            {
                case ReserveResultCode.Successful:
                    return "Successful.";
                case ReserveResultCode.WarehouseDoesNotHaveProduct:
                    return "Selected warehouse does not have selected product.";
                case ReserveResultCode.Error:
                    return resultMessage;
                default:
                    return "Unknown error.";
            }
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
                    //_resultMessages[i] = Reserve(parameters.Product, random.Next(parameters.AmountMin, parameters.AmountMax + 1));;
                    _resultMessages[i] = _integrixDataBase.Reserve(parameters.Product, random.Next(parameters.AmountMin, parameters.AmountMax + 1));
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
            //User[] users;
            //Product product;
            //Warehouse warehouse;
            //PrintProductAndTheirQuanity();
            //using (var db = new IntegrixContext())
            //{
            //    users = db.Users.AsNoTracking().ToArray();
            //    product = db.Products.AsNoTracking().FirstOrDefault();
            //    warehouse = db.Warehouses.AsNoTracking().FirstOrDefault();
            //    //var dns_3060ti = new ProductInfoInWarehouse
            //    //{
            //    //    Product = product,
            //    //    Warehouse = warehouse,
            //    //    QuantityFreeProduct = 100
            //    //};
            //    //db.ProductInfoInWarehouses.Add(dns_3060ti);
            //    //db.SaveChanges();
            //}

            //var countThreads = 4; // users.Length

            //TestThread[] testThreads = new TestThread[countThreads];

            //for(int i = 0; i < countThreads; i++)
            //{
            //    testThreads[i] = new TestThread(users[i], warehouse);
            //}

            //foreach(var test in testThreads)
            //{
            //    test.Start(100, product, 1, 4);
            //}

            //foreach (var test in testThreads)
            //{
            //    test.Thread.Join();
            //}

            //foreach (var test in testThreads)
            //{
            //    test.PrintResultInFile($"Result\\{test.Thread.Name}.txt");
            //}

            //PrintProductAndTheirQuanity();

            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            config.ConnectionStrings.ConnectionStrings.Add(new ConnectionStringSettings(
                name: "Database", connectionString: "Host=localhost;Port=5432;Database=integrix;Username=postgres;Password=kekpek"));

            config.Save(ConfigurationSaveMode.Modified);

            Console.WriteLine(config.ConnectionStrings.ConnectionStrings["Database"].ConnectionString);
            config.ConnectionStrings.ConnectionStrings["Database"].ConnectionString = "Host=localhost;Port=5432;Database=integrix;Username=postgres;Password=kekpek";
            Console.WriteLine(ConfigurationManager.ConnectionStrings["Database"].ConnectionString);
        }
    }
}
