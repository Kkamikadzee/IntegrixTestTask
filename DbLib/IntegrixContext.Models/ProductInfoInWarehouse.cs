using System;
using System.Collections.Generic;

#nullable disable

namespace DbLib.IntegrixContext.Models
{
    public partial class ProductInfoInWarehouse
    {
        public int ProdInfoWrhsId { get; set; }
        public int ProductId { get; set; }
        public int WarehouseId { get; set; }
        public int QuantityFreeProduct { get; set; }
        public int QuantityReservedProduct { get; set; }

        public virtual Product Product { get; set; }
        public virtual Warehouse Warehouse { get; set; }
    }
}
