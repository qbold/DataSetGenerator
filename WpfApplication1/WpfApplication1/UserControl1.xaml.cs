using System.Windows;
using System.Windows.Controls;
using System.Data;
using Microsoft.Win32;
using System;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using libsvm;
using System.Windows.Controls.Primitives;

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
            open_image.Multiselect = true;
        }

        public UserControl1()
        {
            this.DataContext = this;

            this.InitializeComponent();

            CreateDataSet();

            for (int i = 0; i < 9; i++)
                AddColumn();
            for (int i = 0; i < 17; i++)
            {
                dtable.Rows.Add(dtable.NewRow());
            }

            grid.DataContext = dset;
        }

        /// <summary>
        /// Инициализация DataSet
        /// </summary>
        private void CreateDataSet()
        {
            dset = new DataSet();
            dset.Tables.Add("Set1");
            dtable = dset.Tables["Set1"];
        }

        /// <summary>
        /// Клик по кнопке контекстного меню "Добавить столбец" (Кнопка убрана)
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
        /// Очистить содержимое ячеек
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Clear(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Really clear the selected area?", "Clear", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                IList<DataGridCellInfo> l = grid.SelectedCells; // список выбранных ячеек
                foreach (DataGridCellInfo inf in l)
                {
                    int a = grid.Items.IndexOf(inf.Item), b = grid.Columns.IndexOf(inf.Column);
                    if (a >= dtable.Rows.Count) dtable.Rows.Add(dtable.NewRow());
                    dtable.Rows[a][b] = "";
                }
                MainWindow.window.Change(this);
            }
        }

        /// <summary>
        /// SVM
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SVMgo(object sender, RoutedEventArgs e)
        {
            IList<DataGridCellInfo> l = grid.SelectedCells; // список выбранных ячеек

            SVM s = null;
            if (l.Count > 1)
            {
                List<List<double>> l2 = new List<List<double>>(l.Count);
                for (int i = 0; i < l.Count; i++)
                {
                    try
                    {
                        DataGridCellInfo inf = l[i];
                        List<double> l3 = new List<double>(Array.ConvertAll(((string)dtable.Rows[grid.Items.IndexOf(inf.Item)][grid.Columns.IndexOf(inf.Column)]).Split(new char[] { ';' }), a => double.Parse(a.Trim())));
                        l3.Insert(0, double.Parse((string)(dtable.Rows[grid.Items.IndexOf(inf.Item)][grid.Columns.IndexOf(inf.Column) + 1])));
                        l2.Add(l3);
                    }
                    catch (Exception e2)
                    {
                    }
                }
                s = ML.getSVMObject(l2);
            }

            Window2 w = new Window2();
            w.svm = s;
            // w.Start();
            w.Show();
        }

        /// <summary>
        /// Добавить строки в таблицу
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void RowAdd(object sender, RoutedEventArgs e)
        {
            RowNumber r = new RowNumber();
            if (r.ShowDialog() == true)
            {
                int s = r.GetNumber;
                for (int i = 0; i < s; i++)
                {
                    dtable.Rows.Add(dtable.NewRow());
                }
            }
        }

        /// <summary>
        /// Добавить колонки в таблицу
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ColAdd(object sender, RoutedEventArgs e)
        {
            RowNumber r = new RowNumber();
            if (r.ShowDialog() == true)
            {
                int s = r.GetNumber;

                DataTable t = dtable.Copy();

                int col = dtable.Columns.Count;
                int rw = dtable.Rows.Count;

                CreateDataSet();

                for (int i = 0; i < col + s; i++)
                {
                    AddColumn();
                }

                for (int i = 0; i < rw; i++)
                {
                    dtable.Rows.Add(dtable.NewRow());
                }

                for (int i = 0; i < col; i++)
                {
                    for (int j = 0; j < rw; j++)
                    {
                        dtable.Rows[j][dtable.Columns[i]] = t.Rows[j][t.Columns[i]];
                    }
                }

                grid.DataContext = dset;
            }
        }

        /// <summary>
        /// Заполнить выделенные ячейки текстом
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void FillText(object sender, RoutedEventArgs e)
        {
            Window1 wind = new Window1();
            bool? b = wind.ShowDialog();
            if (b == true)
            {
                IList<DataGridCellInfo> l = grid.SelectedCells; // список выбранных ячеек
                foreach (DataGridCellInfo inf in l)
                {
                    int a = grid.Items.IndexOf(inf.Item), b1 = grid.Columns.IndexOf(inf.Column);
                    if (a >= dtable.Rows.Count) dtable.Rows.Add(dtable.NewRow());
                    dtable.Rows[a][b1] = wind.textBox.Text;
                }
                MainWindow.window.Change(this);
            }
        }

        /// <summary>
        /// Открыть диалоговое окно для чтения изображений и заполнения таблицы данными гистограмм ориентированных градиентов
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void HogActive2(object sender, RoutedEventArgs e)
        {
            bool? s = open_image.ShowDialog();

            if (s == true)
            {
                string[] names = open_image.FileNames;

                Window3 w = new Window3();
                w.Count = names.Length;
                if (w.ShowDialog() == true)
                {
                    BitmapImage b = null;

                    IList<DataGridCellInfo> l = grid.SelectedCells; // список выбранных ячеек
                    DataGridCellInfo inf;
                    int index = 0;

                    foreach (string file_name in names)
                    {
                        if (index >= l.Count) break;
                        // inf = l[index++];

                        b = new BitmapImage();
                        // Загружаем изображение и уменьшаем его до 176х144
                        b.BeginInit();
                        b.UriSource = new System.Uri(file_name);
                        b.DecodePixelHeight = 144;
                        b.DecodePixelWidth = 176;
                        b.EndInit();

                        b.StartTransforms(w.CountScales, w.CountShifts, w.CountBlurs, w.CountRotates);

                        // Если есть искажённые копии
                        while (b.HasNext())
                        {
                            if (index >= l.Count) break;
                            inf = l[index++];

                            // Генерируем HoG искажённой копии
                            double[] vec = b.GenerateNext();

                            //int el = grid.Items.IndexOf(grid.CurrentItem);
                            //   if (dtable.Rows.Count == el)
                            //  {
                            //       dtable.Rows.InsertAt(dtable.NewRow(), dtable.Rows.Count);
                            //   }
                            //   dtable.Rows[el][grid.CurrentColumn.DisplayIndex] = string.Join(";", vec);
                            int a = grid.Items.IndexOf(inf.Item), b1 = grid.Columns.IndexOf(inf.Column);
                            if (a >= dtable.Rows.Count) dtable.Rows.Add(dtable.NewRow());
                            dtable.Rows[a][b1] = string.Join(";", vec);
                        }
                    }
                    MainWindow.window.Change(this);
                }
            }
        }

        /* /// <summary>
         /// Открыть диалоговое окно для чтения изображения и построения гистограммы ориентированных градиентов
         /// </summary>
         /// <param name="sender"></param>
         /// <param name="e"></param>
         public void HogActive(object sender, RoutedEventArgs e)
         {
             open_image.Multiselect = false;
             bool? s = open_image.ShowDialog();

             if (s == true)
             {
                 BitmapImage b = new BitmapImage();
                 // Загружаем изображение и уменьшаем его до 176х144
                 b.BeginInit();
                 b.UriSource = new System.Uri(open_image.FileName);
                 b.DecodePixelHeight = 144;
                 b.DecodePixelWidth = 176;
                 b.EndInit();

                 double[] vec = HoG(b);

                 int el = grid.Items.IndexOf(grid.CurrentItem);
                 if (dtable.Rows.Count == el)
                 {
                     dtable.Rows.InsertAt(dtable.NewRow(), dtable.Rows.Count);
                 }
                 dtable.Rows[el][grid.CurrentColumn.DisplayIndex] = string.Join(";", vec);
             }
         }*/

        /// <summary>
        /// Изменение значения ячейки
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void grid_CurrentCellChanged(object sender, DataGridCellEditEndingEventArgs e)
        {
            MainWindow.window.Change(this);
        }

        /// <summary>
        /// Выделение колонки
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void columnHeader_Click(object sender, RoutedEventArgs e)
        {
            var columnHeader = sender as DataGridColumnHeader;
           // MessageBox.Show("sa");
            if (columnHeader != null)
            {
               // MessageBox.Show("sa2");
                grid.SelectedCells.Clear();
                foreach (var item in grid.Items)
                {
                    grid.SelectedCells.Add(new DataGridCellInfo(item, columnHeader.Column));
                }
            }
        }
    }
}