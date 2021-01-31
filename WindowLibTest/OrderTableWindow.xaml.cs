using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using DbLib.IntegrixContext.Models;
using System.Linq;
using System.Collections.ObjectModel;

namespace WindowLibTest
{
    /// <summary>
    /// Логика взаимодействия для OrderTableWindow.xaml
    /// </summary>
    public partial class OrderTableWindow : Window
    {
        private class TableRow
        {
            public string ProductName { get; set; }
            public DateTime OrderDate { get; set; }
            public string UserName { get; set; }
            public int Quantity { get; set; }
        }

        ObservableCollection<TableRow> table;

        public OrderTableWindow()
        {
            InitializeComponent();

            using(var db = new IntegrixContext())
            {
                var query = from orderItems in db.OrderItems
                            join orders in db.Orders
                                on orderItems.OrderId equals orders.OrderId
                            join products in db.Products
                                on orderItems.ProductId equals products.ProductId
                            join users in db.Users
                                on orders.UserId equals users.UserId
                            orderby orders.OrderDate ascending
                            select new TableRow
                            {
                                ProductName = products.FullName,
                                OrderDate = orders.OrderDate,
                                UserName = users.Name,
                                Quantity = orderItems.Quantity
                            };

                table = new ObservableCollection<TableRow>(query.ToArray());
            }
            mainDataGrid.DataContext = table;
            mainDataGrid.ItemsSource = table;
        }
    }
}
