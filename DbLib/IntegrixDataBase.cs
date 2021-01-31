using DbLib.IntegrixContext.Models;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace DbLib
{
    public class IntegrixDataBase
    {
        private enum ReserveResultCode
        {
            Successful = 0,
            WarehouseDoesNotHaveProduct = 1,
            Error = 255
        }

        public User SelectedUser { get; set; }
        public Warehouse SelectedWarehouse { get; set; }

        private string ReserveResultCodeToString(ReserveResultCode resultCode, string resultMessage)
        {
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

        private ReserveResultCode ReserveProduct(Product product, int quantity, IntegrixContext.Models.IntegrixContext db, ref string resultMessage)
        {
            ReserveResultCode resultCode;
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
                        quantity, product.ProductId, SelectedWarehouse.WarehouseId);

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

            return resultCode;
        }

        private ReserveResultCode CreateOrder(Product product, int quantity, IntegrixContext.Models.IntegrixContext db, ref string resultMessage)
        {
            ReserveResultCode resultCode;
            using (var transaction = db.Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted))
            {
                try
                {
                    var order = new Order
                    {
                        UserId = SelectedUser.UserId,
                        WarehouseId = SelectedWarehouse.WarehouseId,
                        OrderDate = DateTimeOffset.Now.ToUniversalTime().DateTime
                    };
                    db.Orders.Add(order);

                    db.SaveChanges();

                    var orderItem = new OrderItem
                    {
                        ProductId = product.ProductId,
                        OrderId = order.OrderId,
                        Quantity = quantity
                    };
                    db.OrderItems.Add(orderItem);

                    db.SaveChanges();

                    transaction.Commit();

                    resultCode = ReserveResultCode.Successful;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    resultCode = ReserveResultCode.Error;
                    resultMessage = ("Failed to reserve the product. " + ex.Message);
                }
            }

            return resultCode;
        }

        private ReserveResultCode CreateNotReservedItem(Product product, int quantity, IntegrixContext.Models.IntegrixContext db, ref string resultMessage)
        {
            ReserveResultCode resultCode;
            using (var transaction = db.Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted))
            {
                try
                {
                    var nri = new NotReservedItem
                    {
                        UserId = SelectedUser.UserId,
                        ProductId = product.ProductId,
                        WarehouseId = SelectedWarehouse.WarehouseId,
                        Quantity = quantity,
                        Reason = "Selected warehouse does not have selected product.",
                        ReservationDate = DateTimeOffset.Now.ToUniversalTime().DateTime
                    };
                    db.NotReservedItems.Add(nri);

                    db.SaveChanges();
                    transaction.Commit();

                    resultCode = ReserveResultCode.WarehouseDoesNotHaveProduct;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    resultCode = ReserveResultCode.Error;
                    if (ex.InnerException != null)
                    {
                        resultMessage = ("Failed to write information about a non-reserved item. " + ex.Message + ex.InnerException.Message);
                    }
                    else
                    {
                        resultMessage = ("Failed to write information about a non-reserved item. " + ex.Message);
                    }
                }
            }

            return resultCode;
        }

        public string Reserve(Product product, int quantity)
        {
            ReserveResultCode resultCode;
            string resultMessage = string.Empty;
            using (var db = new IntegrixContext.Models.IntegrixContext())
            {
                resultCode = ReserveProduct(product, quantity, db, ref resultMessage);

                if (resultCode == ReserveResultCode.Error)
                {
                    return resultMessage;
                }
                else if (resultCode == ReserveResultCode.Successful)
                {
                    resultCode = CreateOrder(product, quantity, db, ref resultMessage);
                }
                else if (resultCode == ReserveResultCode.WarehouseDoesNotHaveProduct)
                {
                    resultCode = CreateNotReservedItem(product, quantity, db, ref resultMessage);
                }
            }

            return ReserveResultCodeToString(resultCode, resultMessage);
        }

        static public bool CheckConnection()
        {
            bool result;
            try
            {
                using (var db = new IntegrixContext.Models.IntegrixContext())
                {
                    result = db.Database.CanConnect();
                }
            }
            catch
            {
                result = false;
            }

            return result;
        }
    }
}
