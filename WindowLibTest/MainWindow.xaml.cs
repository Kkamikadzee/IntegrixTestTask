using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DbLib;
using DbLib.IntegrixContext.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Windows.Threading;
using System.Configuration;

namespace WindowLibTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private class TestThread
        {
            private GroupBox _groupBox;
            private ProgressBar _progressBar;

            private Product _product;
            private int _minQuantity;
            private int _maxQuantity;

            private int _amountQuery;

            private string[] _queryMessages;

            private IntegrixDataBase _integrixDataBase;

            public GroupBox GroupBox => _groupBox;
            public ProgressBar ProgressBar => _progressBar;

            public int MinQuantity => _minQuantity;
            public int MaxQuantity => _maxQuantity;

            public int AmountQuery => _amountQuery;

            public TestThread(User user, Warehouse warehouse, Product product, int minQuantity, int maxQuantity, int amountQuery)
            {
                _product = product;
                _minQuantity = minQuantity;
                _maxQuantity = maxQuantity;

                _amountQuery = amountQuery;

                _queryMessages = new string[_amountQuery];

                _groupBox = new GroupBox()
                {
                    Header = user.Name
                };
                _progressBar = new ProgressBar() //Можно было прототип использовать
                {
                    Margin = new Thickness(4),
                    Width = 120,
                    Height = 20,

                    Maximum = 1
                };

                _groupBox.Content = _progressBar;

                _integrixDataBase = new IntegrixDataBase()
                {
                    SelectedUser = user,
                    SelectedWarehouse = warehouse
                };

                _groupBox.MouseDoubleClick += ShowMessagesHendler;
            }

            private void ShowMessages()
            {
                if (_queryMessages != null)
                {
                    var window = new ThreadResultWindow(_queryMessages);
                    window.Show();
                }
            }
            private void ShowMessagesHendler(object sender, MouseButtonEventArgs e)
            {
                ShowMessages();
            }

            public void TestQuery(int counter, int quantity)
            {
                try
                {
                    _queryMessages[counter] = _integrixDataBase.Reserve(_product, quantity);
                }
                catch (Exception ex)
                {
                    _queryMessages[counter] = ex.Message;
                }
            }
        }

        private Dictionary<string, Product> _products;
        private Dictionary<string, Warehouse> _warehouses;

        private TestThread[] _testThreads;

        public MainWindow()
        {
            ChechConnectionString();
            InitializeComponent();
        }

        private void ChechConnectionString()
        {
            if(!IntegrixDataBase.CheckConnection())
            {
                ShowCreateConnectionString();
            }
        }

        private void RefreshComboBoxes()
        {
            using (var db = new IntegrixContext())
            {
                _products = db.Products.ToDictionary(key => key.FullName);
                _warehouses = db.Warehouses.ToDictionary(key => key.Name);
            }
            cbProduct.ItemsSource = _products.Keys;
            cbWarehouse.ItemsSource = _warehouses.Keys;

        }

        private void sliderAmountThreads_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (tbAmountThreads != null)
            {
                tbAmountThreads.Text = sliderAmountThreads.Value.ToString();
            }
        }

        private void sliderAmountQuery_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if(tbAmountQuery != null)
            {
                tbAmountQuery.Text = sliderAmountQuery.Value.ToString();
            }
        }

        private void sliderMinQuantityProduct_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (tbMinQuantityProduct != null)
            {
                tbMinQuantityProduct.Text = sliderMinQuantityProduct.Value.ToString();
            }
            if(sliderMaxQuantityProduct != null)
            {
                sliderMaxQuantityProduct.Minimum = sliderMinQuantityProduct.Value;
            }
        }

        private void sliderMaxQuantityProduct_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (tbMaxQuantityProduct != null)
            {
                tbMaxQuantityProduct.Text = sliderMaxQuantityProduct.Value.ToString();
            }

        }

        private void SetStatus(string message)
        {
            tbStatus.Text = message;
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            cbProduct.SelectedItem = null;
            cbWarehouse.SelectedItem = null;
            RefreshComboBoxes();

            SetStatus("Данные из базы обновлены.");
        }

        private void CreateTables_Click(object sender, RoutedEventArgs e)
        {
            using (var db = new IntegrixContext())
            {
                try
                {
                    db.Database.ExecuteSqlRaw("CREATE TABLE public.users(    user_id SERIAL PRIMARY KEY,    name character varying(16) COLLATE pg_catalog.\"default\" NOT NULL);" +
                        "CREATE TABLE public.products(    product_id SERIAL PRIMARY KEY,    full_name character varying(16) COLLATE pg_catalog.\"default\" NOT NULL);" +
                        "CREATE TABLE public.warehouses(    warehouse_id SERIAL PRIMARY KEY,    name character varying(16) COLLATE pg_catalog.\"default\" NOT NULL);" +
                        "CREATE TABLE public.product_info_in_warehouse(    prod_info_wrhs_id SERIAL PRIMARY KEY,    product_id integer NOT NULL,    warehouse_id integer NOT NULL,    quantity_free_product integer NOT NULL,    quantity_reserved_product integer NOT NULL DEFAULT 0,    CONSTRAINT fk_product_and_prod_info_wrhs FOREIGN KEY (product_id)        REFERENCES public.products (product_id),    CONSTRAINT fk_warehouse_and_prod_info_wrhs FOREIGN KEY (warehouse_id)        REFERENCES public.warehouses (warehouse_id));" +
                        "CREATE TABLE public.orders(    order_id SERIAL PRIMARY KEY,    user_id integer NOT NULL,	warehouse_id integer NOT NULL,    order_date timestamp without time zone NOT NULL,    CONSTRAINT fk_user_and_orders FOREIGN KEY (user_id)        REFERENCES public.users (user_id),	CONSTRAINT fk_warehouse_and_orders FOREIGN KEY (warehouse_id)        REFERENCES public.warehouses (warehouse_id));" +
                        "CREATE TABLE public.order_items(    order_items_id SERIAL PRIMARY KEY,    product_id integer NOT NULL,    order_id integer NOT NULL,    quantity integer NOT NULL,    CONSTRAINT fk_order_and_oreder_items FOREIGN KEY (order_id)        REFERENCES public.orders (order_id),    CONSTRAINT fk_product_and_order_items FOREIGN KEY (product_id)        REFERENCES public.products (product_id));" +
                        "CREATE TABLE public.not_reserved_items(    not_reserved_items_id SERIAL PRIMARY KEY,	user_id integer NOT NULL,    product_id integer NOT NULL,    warehouse_id integer NOT NULL,    quantity integer NOT NULL,	reason character varying(128),	reservation_date timestamp without time zone NOT NULL,    CONSTRAINT fk_user_and_not_reserved_items FOREIGN KEY (user_id)        REFERENCES public.users (user_id),    CONSTRAINT fk_product_and_not_reserved_items FOREIGN KEY (product_id)        REFERENCES public.products (product_id),    CONSTRAINT fk_warehouse_and_not_reserved_items FOREIGN KEY (warehouse_id)        REFERENCES public.warehouses (warehouse_id));");
                    db.SaveChanges();

                    SetStatus("Таблицы созданы.");
                }
                catch
                {
                    SetStatus("Не удалось создать таблицы.");
                }
            }
        }


        private void DeleteAllTables(IntegrixContext db)
        {
            try
            {
                db.Database.ExecuteSqlRaw("DELETE FROM public.not_reserved_items;");
            }
            catch
            {
                SetStatus("Не удалось очистить таблицу not_reserved_items.");
            }
            try
            {
                db.Database.ExecuteSqlRaw("DELETE FROM public.order_items;");
            }
            catch
            {
                SetStatus("Не удалось очистить таблицу order_items.");
            }
            try
            {
                db.Database.ExecuteSqlRaw("DELETE FROM public.orders;");
            }
            catch
            {
                SetStatus("Не удалось очистить таблицу orders.");
            }
            try
            {
                db.Database.ExecuteSqlRaw("DELETE FROM public.product_info_in_warehouse;");
            }
            catch
            {
                SetStatus("Не удалось очистить таблицу product_info_in_warehouse.");
            }
            try
            {
                db.Database.ExecuteSqlRaw("DELETE FROM public.users;");
            }
            catch
            {
                SetStatus("Не удалось очистить таблицу users.");
            }
            try
            {
                db.Database.ExecuteSqlRaw("DELETE FROM public.warehouses;");
            }
            catch
            {
                SetStatus("Не удалось очистить таблицу warehouses.");
            }
            try
            {
                db.Database.ExecuteSqlRaw("DELETE FROM public.products;");
                db.SaveChanges();
            }
            catch
            {
                SetStatus("Не удалось очистить таблицу products.");
            }
        }

        private void Install_Click(object sender, RoutedEventArgs e)
        {
            using (var db = new IntegrixContext())
            {
                DeleteAllTables(db);

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

                var users = new User[(int)(sliderAmountThreads.Maximum)];
                for (int i = 0; i < users.Length; i++)
                {
                    users[i] = new User()
                    {
                        Name = "Thread_" + i.ToString()
                    };
                }
                db.Users.AddRange(users);
                db.SaveChanges();
            }

            threadsList.Items.Clear();

            SetStatus("Тестовые записи созданы.");
        }

        private void CreateTest(Warehouse warehouse, Product product, int amountThreads, int minQuantity, int maxQuantity, int amountQuery)
        {
            threadsList.Items.Clear();

            User[] users;
            using (var db = new IntegrixContext())
            {
                users = db.Users.Take(amountThreads).AsNoTracking().ToArray();
            }

            _testThreads = new TestThread[users.Length];
            for(int i = 0; i < users.Length; i++)
            {
                _testThreads[i] = new TestThread(users[i], warehouse, product,
                    minQuantity, maxQuantity, amountQuery);

                threadsList.Items.Add(_testThreads[i].GroupBox);
            }
        }

        private void CreateTest_Click(object sender, RoutedEventArgs e)
        {
            if((cbWarehouse.SelectedItem != null) && (cbProduct.SelectedItem != null))
            {
                CreateTest(_warehouses[cbWarehouse.SelectedItem as string], _products[cbProduct.SelectedItem as string],
                    (int)(sliderAmountThreads.Value),
                    (int)(sliderMinQuantityProduct.Value), (int)(sliderMaxQuantityProduct.Value),
                    (int)(sliderAmountQuery.Value));

                SetStatus("Созданы объекты для тестирования");
            }
            else
            {
                SetStatus("Выберете товар и склад");
            }
        }

        private void StartTest()
        {
            if (_testThreads != null)
            {
                Task[] tasks = new Task[_testThreads.Length];
                foreach (var (test, index) in _testThreads.Select((value, i) => (value, i)))
                {
                    Action action = () =>
                    {
                        var random = new Random();
                        for (int i = 0; i < test.AmountQuery; i++)
                        {
                            test.TestQuery(i, random.Next(test.MinQuantity, test.MaxQuantity + 1));

                            Thread.Sleep(5);

                            Dispatcher.Invoke(() =>
                            {
                                test.ProgressBar.Value = ((float)(i)) / test.AmountQuery;
                            });
                        }
                    };

                    tasks[index] = new Task(action);
                }

                foreach (var task in tasks)
                {
                    task.Start();
                }

                SetStatus("Тест запущен");
            }
            else
            {
                SetStatus("Создайте объекты");
            }
        }

        private void StartTest_Click(object sender, RoutedEventArgs e)
        {
            StartTest();
        }

        private void ShowOrderTable_Click(object sender, RoutedEventArgs e)
        {
            var orderTable = new OrderTableWindow()
            {
                Owner = this
            };
            orderTable.Show();
        }

        private void ShowProductInfoInWarehousesTable_Click(object sender, RoutedEventArgs e)
        {
            var piiwTable = new ProductInfoInWarehousesTableWindow()
            {
                Owner = this
            };
            piiwTable.Show();
        }

        private string GetSumQuantity()
        {
            int sum;

            using(var db = new IntegrixContext())
            {
                sum = db.OrderItems.Sum(value => value.Quantity);
            }

            return sum.ToString();
        }

        private void SumQuantity_Click(object sender, RoutedEventArgs e)
        {
            tbStatus.Text = "Sum = " + GetSumQuantity();
        }

        private void ShowCreateConnectionString()
        {
            var window = new CreateConnectionStringWindow();
            window.Show();

            this.Close();
        }

        private void CreateConnectionString_Click(object sender, RoutedEventArgs e)
        {
            ShowCreateConnectionString();
        }

        private void NotReservedItems_Click(object sender, RoutedEventArgs e)
        {
            var window = new NotReservedItemsWindow();
            window.Show();
        }
    }
}
