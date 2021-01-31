using DbLib;
using DbLib.IntegrixContext.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WindowLibTest
{
    /// <summary>
    /// Логика взаимодействия для CreateConnectionStringWindow.xaml
    /// </summary>
    public partial class CreateConnectionStringWindow : Window
    {
        public CreateConnectionStringWindow()
        {
            InitializeComponent();
        }

        private void CreateConnectionString_Click(object sender, RoutedEventArgs e)
        {
            if(ConnectionString.Text == string.Empty)
            {
                return;
            }

            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            if(config.ConnectionStrings.ConnectionStrings["Database"] != null)
            {
                config.ConnectionStrings.ConnectionStrings.Remove("Database");
            }

            config.ConnectionStrings.ConnectionStrings.Add(new ConnectionStringSettings(
                name: "Database", connectionString: ConnectionString.Text));

            config.Save(ConfigurationSaveMode.Modified);

            if(!IntegrixDataBase.CheckConnection())
            {
                var messageBox = MessageBox.Show("Не удалось подключиться к базе, используя данную строку подключения.",
                    "Ошибка подключения к бд");
            }
            else
            {
                var mainWindow = new MainWindow();
                mainWindow.Show();

                this.Close();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
