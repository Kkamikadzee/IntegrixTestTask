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
    /// Логика взаимодействия для NotReservedItemsWindow.xaml
    /// </summary>
    public partial class NotReservedItemsWindow : Window
    {
        private class TableRow
        {
            public string Username { get; set; }
            public string ProductName { get; set; }
            public string WarehouseName { get; set; }
            public int Quantity { get; set; }
            public string Reason { get; set; }
            public DateTime ReservationDate { get; set; }
        }

        ObservableCollection<TableRow> table;

        public NotReservedItemsWindow()
        {
            InitializeComponent();
            using (var db = new IntegrixContext())
            {
                var query = from nri in db.NotReservedItems
                            join products in db.Products
                                on nri.ProductId equals products.ProductId
                            join users in db.Users
                                on nri.UserId equals users.UserId
                            join warehouse in db.Warehouses
                                on nri.WarehouseId equals warehouse.WarehouseId
                            select new TableRow
                            {
                                Username = users.Name,
                                ProductName = products.FullName,
                                WarehouseName = warehouse.Name,
                                Quantity = nri.Quantity,
                                Reason = nri.Reason,
                                ReservationDate = nri.ReservationDate
                            };

                table = new ObservableCollection<TableRow>(query.ToArray());
            }
            mainDataGrid.DataContext = table;
            mainDataGrid.ItemsSource = table;
        }
    }
}
