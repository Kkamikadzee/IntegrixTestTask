using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Логика взаимодействия для ThreadResultWindow.xaml
    /// </summary>
    public partial class ThreadResultWindow : Window
    {
        public ThreadResultWindow(string[] messages)
        {
            InitializeComponent();

            foreach(var (value, index) in messages.Select((value, index) => (value, index)))
            {
                mainListBox.Items.Add(new TextBlock()
                {
                    Text = $"{index.ToString()}. {value}"
                });
            }

        }
    }
}
