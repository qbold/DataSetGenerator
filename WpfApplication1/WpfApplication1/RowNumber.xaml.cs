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

namespace WpfApplication1
{
    /// <summary>
    /// Логика взаимодействия для RowNumber.xaml
    /// </summary>
    public partial class RowNumber : Window
    {

        private int n;
        public int GetNumber { get { return n; } }

        public RowNumber()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string s = textBox.Text.Trim();
            if (s.Length > 3)
            {
                MessageBox.Show("Введите в поле число длины не более 3 символов.");
                return;
            }
            if (!int.TryParse(s, out n))
            {
                MessageBox.Show("Введите в поле число.");
                return;
            }
            DialogResult = true;
        }
    }
}
