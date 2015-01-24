﻿using System.Windows;
using System.Windows.Controls;
using System.Data;
using Microsoft.Win32;
using System;
using System.Windows.Media.Imaging;
using System.Collections.Generic;

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

            dset = new DataSet();
            dset.Tables.Add("Set1");
            dtable = dset.Tables["Set1"];

            grid.DataContext = dset;

            for (int i = 0; i < 20; i++)
                AddColumn();
            for (int i = 0; i < 40; i++)
            {
                dtable.Rows.Add(dtable.NewRow());
            }
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
                    dtable.Rows[grid.Items.IndexOf(inf.Item)][grid.Columns.IndexOf(inf.Column)] = "";
                }
                MainWindow.window.Change(this);
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
                    dtable.Rows[grid.Items.IndexOf(inf.Item)][grid.Columns.IndexOf(inf.Column)] = wind.textBox.Text;
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
                BitmapImage b = null;
                string[] names = open_image.FileNames;

                IList<DataGridCellInfo> l = grid.SelectedCells; // список выбранных ячеек
                DataGridCellInfo inf;
                int index = 0;

                foreach (string file_name in names)
                {
                    if (index >= l.Count) break;
                    inf = l[index++];

                    b = new BitmapImage();
                    // Загружаем изображение и уменьшаем его до 176х144
                    b.BeginInit();
                    b.UriSource = new System.Uri(file_name);
                    b.DecodePixelHeight = 144;
                    b.DecodePixelWidth = 176;
                    b.EndInit();

                    double[] vec = HoG(b);

                    //int el = grid.Items.IndexOf(grid.CurrentItem);
                    //   if (dtable.Rows.Count == el)
                    //  {
                    //       dtable.Rows.InsertAt(dtable.NewRow(), dtable.Rows.Count);
                    //   }
                    //   dtable.Rows[el][grid.CurrentColumn.DisplayIndex] = string.Join(";", vec);
                    dtable.Rows[grid.Items.IndexOf(inf.Item)][grid.Columns.IndexOf(inf.Column)] = string.Join(";", vec);
                }
                MainWindow.window.Change(this);
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
        /// Построение гистограммы ориентированных градиентов
        /// </summary>
        /// <param name="pix"></param>
        /// <returns></returns>
        private double[] HoG(BitmapImage b)
        {
            // Копируем пиксели
            int channels = b.Format.BitsPerPixel / 8;
            int stride = b.PixelWidth * channels;
            byte[] pix = new byte[stride * b.PixelHeight];
            b.CopyPixels(pix, stride, 0);

            int count_pixels_in_cell = 8;
            int count_cells_in_block = 4;
            int step_block = 2;
            int count_bins = 9;

            // Вычисляем grayscale
            for (int i = 1; i < pix.Length; i += channels)
            {
                pix[i] = (byte)(0.299 * pix[i] + 0.587 * pix[i + 1] + 0.114 * pix[i + 2]);
            }

            // Вычисляем направления (углы) градиента изображения и записываем в массив гистограмму по ячейке
            int count_cells_x = b.PixelWidth / count_pixels_in_cell; // количество ячеек по x
            int count_cells_y = b.PixelHeight / count_pixels_in_cell; // количество ячеек по y

            int[,] cells = new int[count_bins, count_cells_x * count_cells_y]; // массив, в котором будут храниться гистограммы для каждой ячейки
            for (int j = 0; j < b.PixelHeight; j++)
            {
                int sess_index_y = j * b.PixelWidth;
                int next_index_y = (j + 1 >= b.PixelHeight ? j : j + 1) * b.PixelWidth;
                int prev_index_y = (j - 1 <= 0 ? 0 : j - 1) * b.PixelWidth;
                for (int i = 0; i < b.PixelWidth; i++)
                {
                    int i2 = i + 1;
                    if (i2 >= b.PixelWidth) i2 = b.PixelWidth - 1;
                    int i1 = i - 1;
                    if (i1 < 0) i1 = 0;
                    int Dx = pix[(i2 + sess_index_y) * channels + 1] - pix[(i1 + sess_index_y) * channels + 1]; // градиент по x
                    int Dy = pix[(i + next_index_y) * channels + 1] - pix[(i + prev_index_y) * channels + 1]; // градиент по y

                    //  if (Dx * Dx + Dy * Dy < 0.225)
                    //  {
                    //      Dx = 0;
                    //      Dy = 0;
                    //  }

                    // Прибавляем на 1 значение в нужном бине гистограммы данной ячейки
                    // Math.Atan2 возвращает угол из интервала (-PI; PI). Делаем значение из интервала (0; 2*PI), затем приводим к виду (0; 1)
                    // Чтобы получить номер бина гистограммы, домножим значение из интервала (0; 1) на количество бинов и возьмём целую часть

                    // Второй аргумент - индекс ячейки. Целочисленное деление i / count_pixels_in_cell даёт номер ячейки по x, j / count_pixels_in_cell - по y
                    // MessageBox.Show(""+ Math.Atan2(Dy, Dx));
                    // if (Math.Abs(Math.Atan2(Dy, Dx)) < 0.1) MessageBox.Show(Dy + " " + Dx + " " + Math.Atan2(Dy, Dx) + " " + i + " " + j);
                    int bin = (int)(((double)(Math.Atan2(Dy, Dx) + Math.PI) / (2d * Math.PI)) * count_bins);
                    if (bin == count_bins) bin--;
                    cells[bin, i / count_pixels_in_cell + (j / count_pixels_in_cell) * count_cells_x]++;
                }
            }

            // Построение гистограммы

            // Всего ячеек в ширину count_cells_x. Каждый блок состоит из count_cells_in_block ячеек
            // Блоки идут с шагом step_block. Чтобы узнать количество блоков по каждой оси, нужно найти максимальную позицию, которую может занимать блок
            // (т.к. он имеет заданную ширину и он не может выходить за пределы изображения)
            int count_blocks_x = (count_cells_x - count_cells_in_block + step_block) / step_block; // количество блоков по x
            int count_blocks_y = (count_cells_y - count_cells_in_block + step_block) / step_block; // количество блоков по y

            int veclen = count_blocks_x * count_blocks_y; // Всего блоков
            double[] vec = new double[count_bins * veclen]; // Вектор бинов; double потому что будем нормализовать; На каждый блок по count_bins значений - бины гистограммы

            // Перебираем все блоки и формируем итоговый массив
            for (int i = 0; i < veclen; i++)
            {
                int x_block = (i % count_blocks_x) * step_block; // координата ячейки начала блока x
                int y_block = (i / count_blocks_x) * step_block; // координата ячейки начала блока y
                int ind_i = i * count_bins;
                // Перебираем все ячейки из блока и суммируем данные гистограмм каждой ячейки
                for (int y = 0; y < count_cells_in_block; y++)
                {
                    int ind_y = (y + y_block) * count_cells_x;
                    for (int x = 0; x < count_cells_in_block; x++)
                    {
                        int ind_x = x + x_block;
                        for (int k = 0; k < count_bins; k++)
                        {
                            vec[k + ind_i] += cells[k, ind_x + ind_y];
                        }
                    }
                }
                // Нормируем вектор
                double norm = 0.001;
                for (int k = 0; k < count_bins; k++)
                {
                    norm += Math.Abs(vec[k + ind_i]);// *vec[k + ind_i];
                }
                //norm = Math.Sqrt(norm);
                for (int k = 0; k < count_bins; k++)
                {
                    vec[k + ind_i] /= norm;
                    vec[k + ind_i] = Math.Sqrt(vec[k + ind_i]);
                }
            }
            return vec;
        }

        /// <summary>
        /// Изменение значения ячейки
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void grid_CurrentCellChanged(object sender, DataGridCellEditEndingEventArgs e)
        {
            MainWindow.window.Change(this);
        }
    }
}