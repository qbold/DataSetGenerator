using System.Windows;
using System.Windows.Controls;
using System.Data;
using Microsoft.Win32;
using System;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using libsvm;
using System.Windows.Controls.Primitives;
using Google.API.Search;
using System.Net;
using System.IO;
using Bing;
using System.Data.Services.Client;

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
            : this(true)
        {
        }

        public UserControl1(bool is_add)
        {
            this.DataContext = this;

            this.InitializeComponent();

            CreateDataSet();
            for (int i = 0; i < 9; i++)
                AddColumn();

            if (is_add)
            {
                for (int i = 0; i < 17; i++)
                {
                    dtable.Rows.Add(dtable.NewRow());
                }
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

            int min = int.MaxValue, max = int.MinValue;
            foreach (DataGridCellInfo inf2 in l)
            {
                int ps = inf2.Column.DisplayIndex;
                if (ps > max)
                {
                    max = ps;
                }
                if (ps < min) min = ps;
            }

            SVM[] s = new SVM[max - min + 1];
            if (l.Count > 1)
            {
                List<List<double>>[] l2 = new List<List<double>>[s.Length];
                for (int i = 0; i < l2.Length; i++)
                {
                    l2[i] = new List<List<double>>(l.Count / l2.Length);
                }
                for (int i = 0; i < l.Count; i++)
                {
                    DataGridCellInfo inf = l[i];
                    try
                    {
                        List<double> l3 = new List<double>(Array.ConvertAll(((string)dtable.Rows[grid.Items.IndexOf(inf.Item)][grid.Columns.IndexOf(inf.Column)]).Split(new char[] { ';' }), a => double.Parse(a.Trim())));
                        l3.Insert(0, double.Parse((string)(dtable.Rows[grid.Items.IndexOf(inf.Item)][max + 1])));
                        l2[inf.Column.DisplayIndex - min].Add(l3);
                    }
                    catch (Exception e2)
                    {
                        MessageBox.Show("Ошибка." + e2.Message);
                    }
                }
                try
                {
                    for (int i = 0; i < l2.Length; i++)
                        s[i] = ML.getSVMObject(l2[i]);
                }
                catch (Exception jh)
                {
                    MessageBox.Show("Некорректный формат файла. " + jh.Message);
                    return;
                }
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
        /// Открыть диалоговое окно для записи ключевых слов для поиска изображений по базе images.google.ru
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void HogActive3(object sender, RoutedEventArgs e)
        {
            Window4 wn = new Window4();
            if (wn.ShowDialog() == true)
            {
                byte type_hash = (byte)(wn.TypeFilter - 1);
                bool hashing = wn.TypeFilter != 0;
                byte search_source = wn.SearchSource;
                int parameter = wn.HammingParameter;

                string tx = wn.textBox.Text.Trim();
                string[] queries = tx.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                List<ulong> hashes = new List<ulong>(wn.Images.Length);
                if (hashing)
                {
                    foreach (string stri in wn.Images)
                    {
                        var bytes = File.ReadAllBytes(stri);
                        BitmapImage bmt = new BitmapImage();
                        bmt.BeginInit();
                        bmt.StreamSource = new MemoryStream(bytes);
                        bmt.CacheOption = BitmapCacheOption.OnLoad;
                        bmt.DecodePixelHeight = 144;
                        bmt.DecodePixelWidth = 176;
                        bmt.EndInit();
                        hashes.Add(bmt.GetHashImage(type_hash));
                    }
                }

                List<BitmapImage> imgs = new List<BitmapImage>(queries.Length * wn.Count);

                // Находим минимальный номер колонки выделенных ячеек и оставляем только ячейки с таким номером
                IList<DataGridCellInfo> l4 = grid.SelectedCells;
                int min = int.MaxValue;
                foreach (DataGridCellInfo infd in l4)
                {
                    if (infd.Column.DisplayIndex < min) min = infd.Column.DisplayIndex;
                }
                List<DataGridCellInfo> l = new List<DataGridCellInfo>(l4.Count);
                foreach (DataGridCellInfo dt in l4)
                {
                    if (dt.Column.DisplayIndex == min) l.Add(dt);
                }


                // Ищем изображения
                int index = 0;
                switch (search_source)
                {
                    case Window4.GOOGLE_SEARCH:
                        GimageSearchClient cl = null;
                        IList<IImageResult> results = null;

                        foreach (string q in queries)
                        {
                            if (index >= l.Count) break;

                            try
                            {
                                cl = new GimageSearchClient("http://www.google.com");
                                results = cl.Search(q, wn.Count);
                            }
                            catch (Exception except)
                            {
                                MessageBox.Show("Ошибка: " + except.Message);
                                return;
                            }

                            foreach (IImageResult ig in results)
                            {
                                if (index >= l.Count) break;

                                var bytes = new WebClient().DownloadData(new Uri(ig.TbImage.Url));

                                BitmapImage b = new BitmapImage();
                                // Загружаем изображение и уменьшаем его до 176х144
                                b.BeginInit();
                                b.StreamSource = new MemoryStream(bytes);
                                b.CacheOption = BitmapCacheOption.OnLoad;
                                b.DecodePixelHeight = 144;
                                b.DecodePixelWidth = 176;
                                b.EndInit();

                                ulong hs = 0;

                                if (hashing)
                                    hs = b.GetHashImage(type_hash);

                                if (!hashing || hashes.Exists(a => Functions.CompareImages(hs, a, parameter)))
                                {
                                    index++;
                                    imgs.Add(b);
                                }
                            }
                        }
                        break;
                    case Window4.BING_SEARCH:
                        string rootUri = "https://api.datamarket.azure.com/Bing/Search";
                        string accountKey = "qv7zZ3lHtZjK1QLxlAgpxs07tYtEO6vuljsYJY5hEGM";

                        BingSearchContainer bingContainer = new BingSearchContainer(new Uri(rootUri));

                        foreach (string q in queries)
                        {
                            if (index >= l.Count) break;

                            bingContainer.Credentials = new NetworkCredential(accountKey, accountKey);

                            var imageQuery = bingContainer.Image(q, null, null, null, null, null, null);
                            var imageResults = imageQuery.Execute();

                            foreach (ImageResult r in imageResults)
                            {
                                if (index >= l.Count) break;

                                var bytes = new WebClient().DownloadData(new Uri(r.Thumbnail.MediaUrl));

                                BitmapImage b = new BitmapImage();
                                // Загружаем изображение и уменьшаем его до 176х144
                                b.BeginInit();
                                b.StreamSource = new MemoryStream(bytes);
                                b.CacheOption = BitmapCacheOption.OnLoad;
                                b.DecodePixelHeight = 144;
                                b.DecodePixelWidth = 176;
                                b.EndInit();

                                ulong hs = 0;

                                if (hashing)
                                    hs = b.GetHashImage(type_hash);

                                if (!hashing || hashes.Exists(a => Functions.CompareImages(hs, a, parameter)))
                                {
                                    index++;
                                    imgs.Add(b);
                                }
                            }
                        }
                        break;
                }

                if (imgs.Count == 0)
                {
                    MessageBox.Show("Не найдено ни одного изображения!");
                    return;
                }

                // Окно настройки параметров
                Window3 w = new Window3();
                w.Count = imgs.Count;

                if (w.ShowDialog() == true)
                {
                    DataGridCellInfo inf;

                    index = 0;

                    foreach (BitmapImage bti in imgs)
                    {
                        GenDo(bti, w, l, ref index, inf);
                    }

                    MainWindow.window.Change(this);
                }
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

                    IList<DataGridCellInfo> l4 = grid.SelectedCells; // список выбранных ячеек
                    int min = int.MaxValue;
                    foreach (DataGridCellInfo infd in l4)
                    {
                        if (infd.Column.DisplayIndex < min) min = infd.Column.DisplayIndex;
                    }
                    List<DataGridCellInfo> l = new List<DataGridCellInfo>(l4.Count);
                    foreach (DataGridCellInfo dt in l4)
                    {
                        if (dt.Column.DisplayIndex == min) l.Add(dt);
                    }

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

                        GenDo(b, w, l, ref index, inf);
                    }
                    MainWindow.window.Change(this);
                }
            }
        }

        private void GenDo(BitmapImage b, Window3 w, List<DataGridCellInfo> l, ref int index, DataGridCellInfo inf)
        {
            b.StartTransforms(w.CountScales, w.CountShifts, w.CountBlurs, w.CountRotates, w.MaxScale, w.MinAngle, w.MaxAngle, w.Red, w.Green, w.Blue, w.CountMargins, w.MinMargin, w.MaxMargin);

            // Если есть искажённые копии
            while (b.HasNext())
            {
                if (index >= l.Count) break;
                inf = l[index++];

                // Генерируем HoG искажённой копии
                double[][] vec = b.GenerateNext();
                // MessageBox.Show(vec.Length + "");

                //int el = grid.Items.IndexOf(grid.CurrentItem);
                //   if (dtable.Rows.Count == el)
                //  {
                //       dtable.Rows.InsertAt(dtable.NewRow(), dtable.Rows.Count);
                //   }
                //   dtable.Rows[el][grid.CurrentColumn.DisplayIndex] = string.Join(";", vec);
                int a = grid.Items.IndexOf(inf.Item), b1 = grid.Columns.IndexOf(inf.Column);
                if (a >= dtable.Rows.Count) dtable.Rows.Add(dtable.NewRow());
                for (int ds = 0; ds < vec.Length; ds++)
                {
                    if (b1 + ds >= dtable.Columns.Count) continue;

                    dtable.Rows[a][b1 + ds] = string.Join(";", vec[ds]);
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

        /// <summary>
        /// Переопределение операции сортировки столбца (замена выделением)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void grid_ColumnReordering(object sender, DataGridSortingEventArgs e)
        {
            e.Handled = true;
            grid.SelectedCells.Clear();
            foreach (var item in grid.Items)
            {
                DataGridCellInfo inf = new DataGridCellInfo(item, e.Column);
                grid.SelectedCells.Add(inf);
            }
        }
    }
}