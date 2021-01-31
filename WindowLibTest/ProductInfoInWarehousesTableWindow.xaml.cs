using DbLib.IntegrixContext.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Linq;

namespace WindowLibTest
{
    /// <summary>
    /// Логика взаимодействия для ProductInfoInWarehousesTableWindow.xaml
    /// </summary>
    public partial class ProductInfoInWarehousesTableWindow : Window
    {
        private class TableRow
        {
            public string ProductName { get; set; }
            public string WarehouseName { get; set; }
            public int QuantityFree { get; set; }
            public int QuantityReserved { get; set; }
        }

        ObservableCollection<TableRow> table;

        public ProductInfoInWarehousesTableWindow()
        {
            InitializeComponent();
            using (var db = new IntegrixContext())
            {
                var query = from piiw in db.ProductInfoInWarehouses
                            join products in db.Products
                                on piiw.ProductId equals products.ProductId
                            join warehouse in db.Warehouses
                                on piiw.WarehouseId equals warehouse.WarehouseId
                            select new TableRow
                            {
                                ProductName = products.FullName,
                                WarehouseName = warehouse.Name,
                                QuantityFree = piiw.QuantityFreeProduct,
                                QuantityReserved = piiw.QuantityReservedProduct
                            };

                table = new ObservableCollection<TableRow>(query.ToArray());
            }
            mainDataGrid.DataContext = table;
            mainDataGrid.ItemsSource = table;
        }
    }
}
