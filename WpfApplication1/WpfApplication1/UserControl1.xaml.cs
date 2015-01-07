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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Data;
using System.ComponentModel;
using Microsoft.Win32;

namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class UserControl1 : UserControl
    {

        public DataSet dset;
        public DataTable dtable;

        private static OpenFileDialog open_image;

        static UserControl1()
        {
            open_image = new OpenFileDialog();
            open_image.Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp;*.gif;";
        }

        public UserControl1()
        {
            this.DataContext = this;

            this.InitializeComponent();

            dset = new DataSet();
            dset.Tables.Add("Set1");
            dtable = dset.Tables["Set1"];

            grid.DataContext = dset;

            AddColumn();
            AddColumn();
            AddColumn();
            AddColumn();
            AddColumn();
            AddColumn();
            AddColumn();
            AddColumn();
        }

        /// <summary>
        /// Клик по кнопке контекстного меню "Добавить столбец"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddColumnClick(object sender, RoutedEventArgs e)
        {
            AddColumn();
        }

        /// <summary>
        /// Добавить колонку
        /// </summary>
        private void AddColumn()
        {
            string name = NextColumn();
            dtable.Columns.Add(new DataColumn(name));
        }

        /// <summary>
        /// Возвращает свободное имя для новой колонки 
        /// </summary>
        /// <returns></returns>
        private string NextColumn()
        {
            int i = 1;
            bool s = true;
            while (s)
            {
                s = false;
                foreach (DataColumn c in dtable.Columns)
                {
                    if (c.ColumnName.Equals("Column" + i)) { s = true; i++; break; }
                }
            }

            return "Column" + i;
        }

        /// <summary>
        /// Открыть диалоговое окно для чтения изображения и установки параметров построения гистограммы ориентированных градиентов
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void HogActive(object sender, RoutedEventArgs e)
        {
            bool? s = open_image.ShowDialog();

            if (s == true)
            {
                //
            }
        }
    }
}