using libsvm;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WpfApplication1
{
    /// <summary>
    /// Описывает функции, применяемые к изображениям
    /// </summary>
    public static class Functions
    {
        public static int ImageWidth { set; get; }
        public static int ImageHeight { set; get; }

        private static Dictionary<BitmapImage, TransformsState> data; // Хранит данные о количестве сгенерированных трансформаций

        private static byte[,] computed_colors_hog;
        //private static int[, ,] computed_cells;
        private static BitmapImage computed_image;
        public static Random random;

        private static double[] EMPTY_ARRAY;

        public static bool CHANNELS_ALL = true;

        static Functions()
        {
            EMPTY_ARRAY = new double[0];

            random = new Random();
            data = new Dictionary<BitmapImage, TransformsState>();
            ImageWidth = 176;
            ImageHeight = 144;

            TABLE_GAUSS = ComputeTableGauss(0.7f);
        }

        /// <summary>
        /// Возвращает массив grayscale пикселов
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static byte[] GrayScale(this BitmapImage b)
        {
            int channels = b.Format.BitsPerPixel / 8;
            int stride = b.PixelWidth * channels;
            byte[] pix = new byte[stride * b.PixelHeight];
            b.CopyPixels(pix, stride, 0);

            // Вычисляем grayscale
            for (int i = 0; i < pix.Length; i += channels)
            {
                pix[i] = (byte)(0.299 * pix[i] + 0.587 * pix[i + 1] + 0.114 * pix[i + 2]);
            }

            return pix;
        }

        /// <summary>
        /// Возвращает массив значений grayscale
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static byte[] Gray(this BitmapImage b)
        {
            int channels = b.Format.BitsPerPixel / 8;
            int stride = b.PixelWidth * channels;
            byte[] pix = new byte[stride * b.PixelHeight];
            b.CopyPixels(pix, stride, 0);

            byte[] ms = new byte[b.PixelWidth * b.PixelHeight];

            // Вычисляем grayscale
            for (int i = 0; i < ms.Length; i++)
            {
                ms[i] = (byte)(0.299 * pix[i * channels] + 0.587 * pix[i * channels + 1] + 0.114 * pix[i * channels + 2]);
            }

            return ms;
        }

        /// <summary>
        /// Возвращает массив пикселов
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static byte[] Pixels(this BitmapImage b)
        {
            int channels = b.Format.BitsPerPixel / 8;
            int stride = b.PixelWidth * channels;
            byte[] pix = new byte[stride * b.PixelHeight];
            b.CopyPixels(pix, stride, 0);
            return pix;
        }

        /// <summary>
        /// Генерация HoG
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static double[,] HoG(this BitmapImage b)
        {
            HoG(CHANNELS_ALL ? Pixels(b) : GrayScale(b), b);
            return GetHistogram(b, 0, 0, b.PixelWidth, b.PixelHeight);
        }

        /// <summary>
        /// Генерация HoG для заданного окна
        /// </summary>
        /// <param name="b"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        public static double[] HoG(this BitmapImage b, int x, int y, int w, int h, int ch = 0)
        {
            if (b != computed_image)
            {
                computed_image = b;
                computed_image.HoG();
            }
            if (x == w && y == h) return EMPTY_ARRAY;
            return GetHistogram(b, x, y, w, h, ch);
        }

        /// <summary>
        /// Вычисление значений computed_cells и computed_colors
        /// </summary>
        /// <param name="pix"></param>
        /// <returns></returns>
        private static void HoG(byte[] pix, BitmapImage b)
        {
            int w = b.PixelWidth;
            int h = b.PixelHeight;

            int channels = b.Format.BitsPerPixel / 8;

            int count_pixels_in_cell = 8;
            int count_bins = 8;

            // Вычисляем направления (углы) градиента изображения и записываем в массив гистограмму по ячейке
            int count_cells_x = w / count_pixels_in_cell; // количество ячеек по x
            int count_cells_y = h / count_pixels_in_cell; // количество ячеек по y

            byte[,] pix2 = new byte[CHANNELS_ALL ? channels : 1, pix.Length];

            for (int k = 0; k < channels; k++)
            {
                if (!CHANNELS_ALL && k > 0) break;
                for (int j = 0; j < h; j++)
                {
                    int sess_index_y = j * w;
                    int next_index_y = (j + 1 >= h ? j : j + 1) * w;
                    int prev_index_y = (j - 1 <= 0 ? 0 : j - 1) * w;
                    for (int i = 0; i < w; i++)
                    {
                        int i2 = i + 1;
                        if (i2 >= w) i2 = w - 1;
                        int i1 = i - 1;
                        if (i1 < 0) i1 = 0;
                        int Dx = pix[(i2 + sess_index_y) * channels + k] - pix[(i1 + sess_index_y) * channels + k]; // градиент по x
                        int Dy = pix[(i + next_index_y) * channels + k] - pix[(i + prev_index_y) * channels + k]; // градиент по y

                        Dx *= Dx;
                        Dy *= Dy;

                        int bin = (int)(((double)(Math.Atan2(Dy, Dx) + Math.PI) / (2d * Math.PI)) * count_bins);
                        bin--;

                        int px = (i + sess_index_y) * channels;
                        pix2[k, px] = (byte)bin;
                    }
                }
            }
            computed_colors_hog = pix2;
        }

        // public static int numa, numb, numc, numd;

        private static double[] GetHistogram(BitmapImage b, int x2, int y2, int w2, int h2, int k = 0)
        {
            int w = b.PixelWidth;
            int h = b.PixelHeight;

            int channels = b.Format.BitsPerPixel / 8;

            float count_pixels_in_cell_x = (float)w2 / 22f;//8;
            float count_pixels_in_cell_y = (float)h2 / 18f;//8;
            int count_cells_in_block = 4;
            int step_block = 2;
            int count_bins = 8;

            // Вычисляем направления (углы) градиента изображения и записываем в массив гистограмму по ячейке
            int count_cells_x = 22;//w / count_pixels_in_cell_x; // количество ячеек по x
            int count_cells_y = 18;// h / count_pixels_in_cell_y; // количество ячеек по y

            int[,] cells = new int[count_bins, count_cells_x * count_cells_y]; // массив, в котором будут храниться гистограммы для каждой ячейки

            for (int j = 0; j < h2; j++)
            {
                int j2 = j + y2;
                int sess_index_y = j2 * w;
                int next_index_y = (j2 + 1 >= h ? j2 : j2 + 1) * w;
                int prev_index_y = (j2 - 1 <= 0 ? 0 : j2 - 1) * w;
                for (int i = 0; i < w2; i++)
                {
                    int i2 = i + x2;
                    int px = (i2 + sess_index_y) * channels;
                    float x_cell = (float)i / count_pixels_in_cell_x;
                    float y_cell = (float)j / count_pixels_in_cell_y;
                    int i_x = (int)x_cell;
                    int i_y = (int)y_cell;
                    float nx = x_cell - i_x;
                    float ny = y_cell - i_y;
                    cells[computed_colors_hog[k, px], i_x + i_y * count_cells_x]++;
                }
            }

            // Построение гистограммы

            // Всего ячеек в ширину count_cells_x. Каждый блок состоит из count_cells_in_block ячеек
            // Блоки идут с шагом step_block. Чтобы узнать количество блоков по каждой оси, нужно найти максимальную позицию, которую может занимать блок
            // (т.к. он имеет заданную ширину и он не может выходить за пределы изображения)
            int count_blocks_x = (count_cells_x - count_cells_in_block + step_block) / step_block; // количество блоков по x
            int count_blocks_y = (count_cells_y - count_cells_in_block + step_block) / step_block; // количество блоков по y

            int veclen = count_blocks_x * count_blocks_y; // Всего блоков
            int sz = count_bins * veclen;
            double[] vec = new double[sz]; // Вектор бинов; double потому что будем нормализовать; На каждый блок по count_bins значений - бины гистограммы

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
                        for (int k2 = 0; k2 < count_bins; k2++)
                        {
                            vec[k2 + ind_i] += cells[k2, ind_x + ind_y];
                        }
                    }
                }
                // Нормируем вектор
                double norm = 0.001;
                for (int k1 = 0; k1 < count_bins; k1++)
                {
                    norm += Math.Abs(vec[k1 + ind_i]);// *vec[k + ind_i];
                }
                for (int k1 = 0; k1 < count_bins; k1++)
                {
                    vec[k1 + ind_i] /= norm;
                    vec[k1 + ind_i] = Math.Sqrt(vec[k1 + ind_i]);
                }
            }

            return vec;
        }

        private static double[,] GetHistogram(BitmapImage b, int x2, int y2, int w2, int h2)
        {
            int w = b.PixelWidth;
            int h = b.PixelHeight;

            int channels = b.Format.BitsPerPixel / 8;

            float count_pixels_in_cell_x = (float)w2 / 22f;//8;
            float count_pixels_in_cell_y = (float)h2 / 18f;//8;
            int count_cells_in_block = 4;
            int step_block = 2;
            int count_bins = 8;

            // Вычисляем направления (углы) градиента изображения и записываем в массив гистограмму по ячейке
            int count_cells_x = 22;//w / count_pixels_in_cell_x; // количество ячеек по x
            int count_cells_y = 18;// h / count_pixels_in_cell_y; // количество ячеек по y

            int[, ,] cells = new int[channels, count_bins, count_cells_x * count_cells_y]; // массив, в котором будут храниться гистограммы для каждой ячейки

            for (int k = 0; k < channels; k++)
            {
                if (!CHANNELS_ALL && k > 0) break;
                for (int j = 0; j < h2; j++)
                {
                    int j2 = j + y2;
                    int sess_index_y = j2 * w;
                    int next_index_y = (j2 + 1 >= h ? j2 : j2 + 1) * w;
                    int prev_index_y = (j2 - 1 <= 0 ? 0 : j2 - 1) * w;
                    for (int i = 0; i < w2; i++)
                    {
                        int i2 = i + x2;
                        int px = (i2 + sess_index_y) * channels;
                        float x_cell = (float)i / count_pixels_in_cell_x;
                        float y_cell = (float)j / count_pixels_in_cell_y;
                        int i_x = (int)x_cell;
                        int i_y = (int)y_cell;
                        float nx = x_cell - i_x;
                        float ny = y_cell - i_y;
                        cells[k, computed_colors_hog[k, px], i_x + i_y * count_cells_x]++;
                    }
                }
            }

            // Построение гистограммы

            // Всего ячеек в ширину count_cells_x. Каждый блок состоит из count_cells_in_block ячеек
            // Блоки идут с шагом step_block. Чтобы узнать количество блоков по каждой оси, нужно найти максимальную позицию, которую может занимать блок
            // (т.к. он имеет заданную ширину и он не может выходить за пределы изображения)
            int count_blocks_x = (count_cells_x - count_cells_in_block + step_block) / step_block; // количество блоков по x
            int count_blocks_y = (count_cells_y - count_cells_in_block + step_block) / step_block; // количество блоков по y

            int veclen = count_blocks_x * count_blocks_y; // Всего блоков
            int sz = count_bins * veclen;
            double[,] vec = new double[channels, sz]; // Вектор бинов; double потому что будем нормализовать; На каждый блок по count_bins значений - бины гистограммы

            // Перебираем все блоки и формируем итоговый массив
            for (int md = 0; md < channels; md++)
            {
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
                                vec[md, k + ind_i] += cells[md, k, ind_x + ind_y];
                            }
                        }
                    }
                    // Нормируем вектор
                    double norm = 0.001;
                    for (int k = 0; k < count_bins; k++)
                    {
                        norm += Math.Abs(vec[md, k + ind_i]);// *vec[k + ind_i];
                    }
                    for (int k = 0; k < count_bins; k++)
                    {
                        vec[md, k + ind_i] /= norm;
                        vec[md, k + ind_i] = Math.Sqrt(vec[md, k + ind_i]);
                    }
                }
            }

            return vec;
        }

        /// <summary>
        /// Сохранение изображения в формате Jpeg
        /// </summary>
        /// <param name="img"></param>
        private static void SaveJpeg(byte[] data, int w, int h, int stride, string filename)
        {
            using (FileStream stream = new FileStream(filename, FileMode.Create))
            {
                JpegBitmapEncoder coder = new JpegBitmapEncoder();
                PixelFormat format = PixelFormats.Bgr32; //RGB + alpha
                WriteableBitmap wbm = new WriteableBitmap(w, h, 96, 96, format, null);
                wbm.WritePixels(new Int32Rect(0, 0, w, h), data, stride, 0);
                coder.Frames.Add(BitmapFrame.Create(wbm));
                coder.Save(stream);
            }
        }

        public const byte NO_COMPRESSION = 0, HUFFMAN = 1;//, LZ77 = 2, LZ78 = 3, LZW = 4, LZSS = 5;

        /// <summary>
        /// Экспортировать классификатор в бинарный файл
        /// </summary>
        /// <param name="svm"></param>
        /// <param name="name"></param>
        public static void ExportBinary(this SVM svm, string name, byte mask)
        {
            svm_model m = typeof(SVM).GetField("model", System.Reflection.BindingFlags.GetField |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic).GetValue(svm) as svm_model;
            svm_parameter p = m.param;

            MemoryStream stream = new MemoryStream();

            // Params
            stream.WriteInt(p.svm_type);
            stream.WriteInt(p.kernel_type);
            stream.WriteInt(p.degree);
            stream.WriteDouble(p.gamma);
            stream.WriteDouble(p.coef0);
            stream.WriteDouble(p.cache_size);
            stream.WriteDouble(p.eps);
            stream.WriteDouble(p.C);
            stream.WriteInt(p.nr_weight);
            stream.WriteDouble(p.nu);
            stream.WriteDouble(p.p);
            stream.WriteInt(p.shrinking);
            stream.WriteInt(p.probability);

            // Model
            stream.WriteInt(m.nr_class);
            stream.WriteInt(m.l);
            stream.WriteInt(m.SV.Length);
            for (int i = 0; i < m.SV.Length; i++)
            {
                stream.WriteInt(m.SV[i].Length);
                for (int j = 0; j < m.SV[0].Length; j++)
                {
                    stream.WriteInt(m.SV[i][j].index);
                    stream.WriteDouble(m.SV[i][j].value);
                }
            }
            stream.WriteInt(m.sv_coef.Length);
            for (int i = 0; i < m.sv_coef.Length; i++)
            {
                stream.WriteInt(m.sv_coef[i].Length);
                for (int j = 0; j < m.sv_coef[i].Length; j++)
                {
                    stream.WriteDouble(m.sv_coef[i][j]);
                }
            }
            stream.WriteInt(m.rho.Length);
            for (int i = 0; i < m.rho.Length; i++)
            {
                stream.WriteDouble(m.rho[i]);
            }
            stream.WriteInt(m.sv_indices.Length);
            for (int i = 0; i < m.sv_indices.Length; i++)
            {
                stream.WriteInt(m.sv_indices[i]);
            }
            stream.WriteInt(m.label.Length);
            for (int i = 0; i < m.label.Length; i++)
            {
                stream.WriteInt(m.label[i]);
            }
            stream.WriteInt(m.nSV.Length);
            for (int i = 0; i < m.nSV.Length; i++)
            {
                stream.WriteInt(m.nSV[i]);
            }

            byte[] dat = stream.ToArray();

            switch (mask)
            {
                case HUFFMAN: dat = EncodeHuffman(dat); break;
                //case LZ77: dat = EncodeLZ77(dat); break;
                //case LZ78: dat = EncodeLZ78(dat); break;
                //case LZW: dat = EncodeLZW(dat); break;
                //case LZSS: dat = EncodeLZSS(dat); break;
            }

            using (FileStream ts = new FileStream(name, FileMode.Create))
            {
                ts.Write(dat, 0, dat.Length);
            }
        }

        /// <summary>
        /// Кодирование Хаффманом
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static byte[] EncodeHuffman(byte[] src)
        {
            BindedData[] count = new BindedData[256];
            BindedData[] B = new BindedData[256];
            //byte[] codes = new byte[256];
            //byte[] lens = new byte[256];

            //int[] powers2 = new int[8];
            //powers2[0] = 1;
            //for (int i = 1; i < powers2.Length; i++)
            //{
            //    powers2[i] = powers2[i - 1] << 1;
            //}

            for (int i = 0; i < count.Length; i++)
            {
                count[i] = new BindedData(0, (byte)i);
            }
            for (int i = 0; i < src.Length; i++)
            {
                count[src[i]].Key++;
            }

            RadixSort(ref count);

            int i1 = 0, j = 0, n = 0;
            BindedData d1 = new BindedData();
            while (i1 < count.Length || j < n)
            {
                int s = int.MaxValue;
                // byte[] elems = null;
                int state = -1;
                if (i1 + 1 < count.Length)
                {
                    s = count[i1].Key + count[i1 + 1].Key;
                    state = 0;
                    //elems = new byte[2] { (byte)count[i1].Data, (byte)count[i1 + 1].Data };
                }
                if (j < n && j + 1 < n)
                {
                    int d = B[j].Key + B[j + 1].Key;
                    if (s > d)
                    {
                        state = 1;
                        s = d;
                        //byte[] Aa = (byte[])(B[j].Data), Bb = (byte[])(B[j + 1].Data);
                        //elems = new byte[Aa.Length + Bb.Length];
                        //Array.Copy(Aa, elems, Aa.Length);
                        //Array.Copy(Bb, 0, elems, Aa.Length, Bb.Length);
                    }
                }
                if (j < n && count.Length > i1)
                {
                    int d = B[j].Key + count[i1].Key;
                    if (s > d)
                    {
                        state = 2;
                        s = d;
                        //byte[] Aa = (byte[])(B[j].Data);
                        //elems = new byte[Aa.Length + 1];
                        //Array.Copy(Aa, elems, Aa.Length);
                        //elems[elems.Length - 1] = (byte)count[i1].Data;
                    }
                }
                switch (state)
                {
                    case -1:
                        d1 = B[j++];
                        break;
                    case 0:
                        //  codes[Math.Max((byte)count[i1].Data, (byte)count[i1 + 1].Data)] = 1;
                        if ((byte)count[i1 + 1].Data > (byte)count[i1].Data)
                        {
                            d1.Data = new BindedData[] { count[i1], count[i1 + 1] };
                        }
                        else
                        {
                            d1.Data = new BindedData[] { count[i1 + 1], count[i1] };
                        }
                        i1 += 2;
                        break;
                    case 1:
                        //if ((byte)B[j + 1].Data > (byte)B[j].Data)
                        //{
                        //    foreach (byte b in (byte[])B[j + 1].Data)
                        //    {
                        //        codes[b] |= powers2[lens[b]++];
                        //    }
                        //    foreach (byte b in (byte[])B[j].Data)
                        //    {
                        //        lens[b]++;
                        //    }
                        //}
                        //else
                        //{
                        //    foreach (byte b in (byte[])B[j].Data)
                        //    {
                        //        codes[b] |= powers2[lens[b]++];
                        //    }
                        //    foreach (byte b in (byte[])B[j + 1].Data)
                        //    {
                        //        lens[b]++;
                        //    }
                        //}
                        if ((byte)B[j + 1].Data > (byte)B[j].Data)
                        {
                            d1.Data = new BindedData[] { B[j], B[j + 1] };
                        }
                        else
                        {
                            d1.Data = new BindedData[] { B[j + 1], B[j] };
                        }
                        j += 2;
                        break;
                    case 2:
                        //if ((byte)count[i1].Data > (byte)B[j].Data)
                        //{
                        //    codes[(byte)count[i1].Data] = 1;
                        //    foreach (byte b in (byte[])B[j].Data)
                        //    {
                        //        lens[b]++;
                        //    }
                        //}
                        //else
                        //{
                        //    foreach (byte b in (byte[])B[j].Data)
                        //    {
                        //        codes[b] |= powers2[lens[b]++];
                        //    }
                        //}
                        if ((byte)count[i1].Data > (byte)B[j].Data)
                        {
                            d1.Data = new BindedData[] { B[j], count[i1] };
                        }
                        else
                        {
                            d1.Data = new BindedData[] { count[i1], B[j] };
                        }
                        i1++;
                        j++;
                        break;
                }
                B[n++] = d1;
            }

            // d - корень дерева Хаффмана

            byte[] outp = null;
            using (MemoryStream stream = new MemoryStream())
            {
                outp = stream.ToArray();
            }

            return outp;
        }

        private static int[] digit_powers;

        /// <summary>
        /// Посчитать степени десяти
        /// </summary>
        /// <param name="m"></param>
        private static void BuildDigitPowers(int m)
        {
            digit_powers = new int[m];
            digit_powers[0] = 1;
            for (int i = 1; i < m; i++)
            {
                digit_powers[i] = 10 * digit_powers[i - 1];
            }
        }

        /// <summary>
        /// Поразрядная сортировка. (За O(nk))
        /// </summary>
        /// <param name="A"></param>
        public static void RadixSort<T>(ref T[] A) where T : ISortable
        {
            int min = int.MaxValue;
            for (int i = 0; i < A.Length; i++)
            {
                if (min > A[i].Key) min = A[i].Key;
            }
            for (int i = 0; i < A.Length; i++)
            {
                A[i].Key -= min;
            }

            int m = 0;
            for (int i = 0; i < A.Length; i++)
            {
                if (Math.Abs(A[i].Key) > m) m = A[i].Key;
            }
            m = Math.Abs(m);
            m = (m + "").Length;
            BuildDigitPowers(m);
            RadixSort(ref A, 0, A.Length, 0, 10, m);

            for (int i = 0; i < A.Length; i++)
            {
                A[i].Key += min;
            }
        }

        /// <summary>
        /// Поразрядная сортировка (За O(nk))
        /// </summary>
        /// <param name="A">Массив</param>
        /// <param name="l">Левая граница сортируемой области в массиве</param>
        /// <param name="r">Правая граница</param>
        /// <param name="d">Индекс разряда</param>
        /// <param name="k">Основание системы счисления</param>
        /// <param name="m">Макс. количество цифр</param>
        public static void RadixSort<T>(ref T[] A, int l, int r, int d, int k, int m) where T : ISortable
        {
            if (digit_powers == null || digit_powers != null && digit_powers.Length < m) BuildDigitPowers(m);
            if (d >= m || l >= r) { return; }
            int[] pos = new int[k];
            T[] B = new T[A.Length];
            for (int i = l; i < r; i++)
            {
                pos[Digit(A[i].Key, d)]++;
            }
            for (int i = pos.Length - 1; i > 0; i--)
            {
                pos[i] = pos[i - 1];
            }
            pos[0] = 0;
            for (int i = 1; i < pos.Length; i++)
            {
                pos[i] += pos[i - 1];
            }
            for (int i = l; i < r; i++)
            {
                B[l + pos[Digit(A[i].Key, d)]++] = A[i];
            }
            for (int i = l; i < r; i++)
            {
                A[i] = B[i];
            }
            for (int i = 0; i < pos.Length; i++)
            {
                int left = (i == 0 ? 0 : pos[i - 1]) + l, right = l + pos[i];
                RadixSort(ref A, left, right, d + 1, k, m);
            }
        }

        /// <summary>
        /// Возвращает значение k-го разряда числа
        /// </summary>
        /// <param name="number"></param>
        /// <param name="k"></param>
        /// <returns></returns>
        private static int Digit(int number, int k)
        {
            return (number / digit_powers[digit_powers.Length - k - 1]) % 10;
        }

        /// <summary>
        /// Сортировка подсчётом (За O(n+k))
        /// </summary>
        /// <param name="src"></param>
        public static void CountingSort(ref int[] src)
        {
            int k = int.MaxValue, k2 = int.MinValue;
            for (int i = 0; i < src.Length; i++)
            {
                if (src[i] > k2) k2 = src[i];
                if (src[i] < k) k = src[i];
            }
            CountingSort(ref src, k, k2);
        }

        /// <summary>
        /// Сортировка подсчётом (За O(n+k))
        /// </summary>
        /// <param name="src"></param>
        public static void CountingSort(ref int[] src, int min, int max)
        {
            int[] d = new int[max - min + 1];
            for (int i = 0; i < src.Length; i++)
            {
                d[src[i] - min]++;
            }
            int j = 0;
            for (int i = 0; i < d.Length; i++)
            {
                int z = i + min;
                for (int k = 0; k < d[i]; k++)
                {
                    src[j++] = z;
                }
            }
        }

        /// <summary>
        /// Записать целое число (4 байта) в поток
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="data"></param>
        public static void WriteInt(this Stream stream, int data)
        {
            stream.WriteByte((byte)((data >> 24) & 0xff));
            stream.WriteByte((byte)((data >> 16) & 0xff));
            stream.WriteByte((byte)((data >> 8) & 0xff));
            stream.WriteByte((byte)((data) & 0xff));
        }

        /// <summary>
        /// Записать целое число (8 байт) в поток
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="data"></param>
        public static void WriteLong(this Stream stream, long data)
        {
            stream.WriteInt((int)((data >> 32) & 0xffffffff));
            stream.WriteInt((int)(data & 0xffffffff));
        }

        /// <summary>
        /// Записать число с плавающей запятой в поток
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="data"></param>
        public static void WriteDouble(this Stream stream, double data)
        {
            stream.WriteLong(BitConverter.DoubleToInt64Bits(data));
        }

        /// <summary>
        /// Сохранение изображения в формате Jpeg
        /// </summary>
        /// <param name="img"></param>
        public static void SaveJpeg(BitmapSource s, string filename)
        {
            using (FileStream stream = new FileStream(filename, FileMode.Create))
            {
                JpegBitmapEncoder coder = new JpegBitmapEncoder();
                coder.Frames.Add(BitmapFrame.Create(s));
                coder.Save(stream);
            }
        }

        /// <summary>
        /// Расстояние Хэмминга
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int HammingDistance(ulong a, ulong b)
        {
            int d = 0;
            ulong st = 1;
            for (int i = 0; i < 64; i++, st *= 2)
            {
                if ((a & st) != (b & st)) d++;
            }
            return d;
        }

        /// <summary>
        /// Вычисление Average Hash изображения
        /// </summary>
        /// <param name="img"></param>
        /// <param name="img"></param>
        /// <returns></returns>
        public static ulong AverageHash(this BitmapImage img)
        {
            int sta = img.Format.BitsPerPixel / 8;
            bool gray = sta == 1;
            // уменьшаем изображение
            WriteableBitmap b = new WriteableBitmap(img);
            b = b.Resize(8, 8, WriteableBitmapExtensions.Interpolation.Bilinear);

            int stride = b.PixelWidth * sta;

            byte[] pixels = new byte[stride * b.PixelWidth];
            b.CopyPixels(pixels, stride, 0);

            int median = 0;

            // бинаризуем
            if (!gray)
            {
                for (int y = 0; y < b.PixelHeight; y++)
                {
                    int ps = y * stride;
                    for (int x = 0; x < b.PixelWidth; x++, ps += sta)
                    {
                        pixels[ps] = (byte)(0.299 * pixels[ps] + 0.587 * pixels[ps + 1] + 0.114 * pixels[ps + 2]);
                        median += pixels[ps];
                    }
                }
            }
            median /= 64;
            for (int y = 0; y < b.PixelHeight; y++)
            {
                int ps = y * stride;
                for (int x = 0; x < b.PixelWidth; x++, ps += sta)
                {
                    pixels[ps] = (byte)(pixels[ps] < median ? 0 : 1);
                }
            }

            ulong h = 0;
            ulong st = 1;
            for (int i = 0; i < 64; i++, st *= 2)
            {
                if (pixels[i * sta] > 0) h |= st;
            }
            return h;
        }

        /// <summary>
        /// Вычисление Difference Hash изображения
        /// </summary>
        /// <param name="img"></param>
        /// <returns></returns>
        public static ulong DifferenceHash(this BitmapImage img)
        {
            int sta = img.Format.BitsPerPixel / 8;
            bool gray = sta == 1;
            // уменьшаем изображение
            WriteableBitmap b = new WriteableBitmap(img);
            b = b.Resize(9, 8, WriteableBitmapExtensions.Interpolation.Bilinear);

            int stride = b.PixelWidth * sta;

            byte[] pixels = new byte[stride * b.PixelHeight];
            b.CopyPixels(pixels, stride, 0);

            // бинаризуем
            if (!gray)
            {
                for (int y = 0; y < b.PixelHeight; y++)
                {
                    int ps = y * stride;
                    for (int x = 0; x < b.PixelWidth; x++, ps += sta)
                    {
                        pixels[ps] = (byte)(0.299 * pixels[ps] + 0.587 * pixels[ps + 1] + 0.114 * pixels[ps + 2]);
                    }
                }
            }
            byte[] pixels2 = new byte[(b.PixelWidth - 1) * (b.PixelHeight)];
            for (int y = 0; y < b.PixelHeight; y++)
            {
                int ps = y * stride;
                int ps2 = y * (b.PixelWidth - 1);
                for (int x = 0; x < b.PixelWidth - 1; x++, ps += sta)
                {
                    pixels2[ps2++] = (byte)(pixels[ps + sta] < pixels[ps] ? 0 : 1);
                }
            }

            ulong h = 0;
            ulong st = 1;
            for (int i = 0; i < 64; i++, st *= 2)
            {
                if (pixels2[i] > 0) h |= st;
            }
            return h;
        }

        /// <summary>
        /// Перцептивный хеш
        /// </summary>
        /// <param name="img"></param>
        /// <returns></returns>
        public static ulong PerceptualHash(this BitmapImage img)
        {
            int sta = img.Format.BitsPerPixel / 8;
            bool gray = sta == 1;
            // уменьшаем изображение
            WriteableBitmap b = new WriteableBitmap(img.Clone());
            b = b.Resize(32, 32, WriteableBitmapExtensions.Interpolation.Bilinear);

            int stride = b.PixelWidth * sta;

            byte[] pixels = new byte[stride * b.PixelHeight];
            b.CopyPixels(pixels, stride, 0);

            int[,] px = new int[32, 32];
            if (!gray)
            {
                for (int y = 0; y < b.PixelHeight; y++)
                {
                    int ps = y * stride;
                    for (int x = 0; x < b.PixelWidth; x++, ps += sta)
                    {
                        px[x, y] = (int)(0.299 * pixels[ps] + 0.587 * pixels[ps + 1] + 0.114 * pixels[ps + 2]) - 128;
                    }
                }
            }

            float[,] dct = DCT(px, 32);
            dct[0, 0] = 0;

            float median = 0;

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    median += dct[i, j];
                }
            }
            median /= 64f;

            ulong h = 0;
            ulong st = 1;
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++, st *= 2)
                {
                    if (dct[i, j] > median) h |= st;
                }
            }
            return h;
        }

        private static float[, , ,] dct_cache;

        /// <summary>
        /// Очистить кэш дискретного косинусного преобразования
        /// </summary>
        public static void FreeDCTCache()
        {
            dct_cache = null;
            GC.Collect();
        }

        /// <summary>
        /// Вычислить кэшируемые данные дискретного косинусного преобразования параметра N
        /// </summary>
        /// <param name="N"></param>
        public static void ComputeDCTCache(int N)
        {
            dct_cache = new float[N, N, N, N];
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    for (int x = 0; x < N; x++)
                    {
                        for (int y = 0; y < N; y++)
                        {
                            dct_cache[i, j, x, y] = (float)(Math.Cos((2 * x + 1) * i * Math.PI / (2 * N)) * Math.Cos((2 * y + 1) * j * Math.PI / (2 * N)));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Дискретное косинусное преобразование
        /// </summary>
        /// <param name="f"></param>
        /// <param name="N"></param>
        /// <returns></returns>
        public static float[,] DCT(int[,] f, int N, bool cache = true)
        {
            float[,] DCT = new float[N, N];
            // Кэширование повторяющихся элементов
            if (cache)
                if (dct_cache == null || dct_cache.GetLength(0) != N)
                {
                    ComputeDCTCache(N);
                }

            // Вычисление DCT
            float mn = (float)(Math.Sqrt(2 * N));
            float sq2 = (float)Math.Sqrt(2);
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    float fq = 0;
                    for (int x = 0; x < N; x++)
                    {
                        for (int y = 0; y < N; y++)
                        {
                            fq += f[x, y] * (cache ? dct_cache[i, j, x, y] : (float)(Math.Cos((2 * x + 1) * i * Math.PI / (2 * N)) * Math.Cos((2 * y + 1) * j * Math.PI / (2 * N))));
                        }
                    }
                    fq /= mn;
                    if (i == 0) fq /= sq2;
                    if (j == 0) fq /= sq2;
                    DCT[i, j] = fq;
                }
            }
            return DCT;
        }

        /// <summary>
        /// Обратное дискретное косинусное преобразование
        /// </summary>
        /// <param name="f"></param>
        /// <param name="N"></param>
        /// <returns></returns>
        public static int[,] DCT_Inv(float[,] DCT, int N, bool cache = true)
        {
            int[,] f = new int[N, N];
            // Кэширование повторяющихся элементов
            if (cache)
                if (dct_cache == null || dct_cache.GetLength(0) != N)
                {
                    ComputeDCTCache(N);
                }

            // Вычисление DCT
            float mn = (float)(Math.Sqrt(2 * N));
            float sq2 = (float)Math.Sqrt(2);
            for (int x = 0; x < N; x++)
            {
                for (int y = 0; y < N; y++)
                {
                    float ft = 0;
                    for (int i = 0; i < N; i++)
                    {
                        for (int j = 0; j < N; j++)
                        {
                            float rt = 1;
                            if (i == 0) rt /= sq2;
                            if (j == 0) rt /= sq2;
                            rt *= DCT[i, j] * (cache ? dct_cache[i, j, x, y] : (float)(Math.Cos((2 * x + 1) * i * Math.PI / (2 * N)) * Math.Cos((2 * y + 1) * j * Math.PI / (2 * N))));
                            ft += rt;
                        }
                    }
                    ft /= mn;
                    f[x, y] = (int)ft;
                }
            }
            return f;
        }

        /// <summary>
        /// Получить хеш изображения указанным методом
        /// </summary>
        /// <param name="img"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static ulong GetHashImage(this BitmapImage img, byte type = aHash)
        {
            switch (type)
            {
                case aHash:
                    return img.AverageHash();
                case dHash:
                    return img.DifferenceHash();
                case pHash:
                    return img.PerceptualHash();
            }
            return 0;
        }

        /// <summary>
        /// Сравнивает два изображения
        /// </summary>
        /// <param name="i1"></param>
        /// <param name="i2"></param>
        /// <param name="method"></param>
        /// <param name="threshold"></param>
        /// <returns></returns>
        public static bool CompareImages(BitmapImage i1, BitmapImage i2, byte method = aHash, int threshold = 10)
        {
            return CompareImages(i1.GetHashImage(method), i2.GetHashImage(method), threshold);
        }

        /// <summary>
        /// Сравнивает два изображения, используя известный хеш
        /// </summary>
        /// <param name="i1"></param>
        /// <param name="i2"></param>
        /// <param name="method"></param>
        /// <param name="threshold"></param>
        /// <returns></returns>
        public static bool CompareImages(ulong hash1, ulong hash2, int threshold = 10)
        {
            return HammingDistance(hash1, hash2) < threshold;
        }

        public const byte aHash = 0, dHash = 1, pHash = 2;

        private static double[] GAUSS;
        private static double[,] TABLE_GAUSS;

        /// <summary>
        /// Гауссово сглаживание
        /// </summary>
        /// <param name="img"></param>
        /// <returns></returns>
        public static BitmapImage Gauss(byte[] img, BitmapImage im, int k)
        {
            byte[] outp = img;
            for (int i = 0; i < k; i++)
            {
                outp = gs(outp, im);
            }

            PixelFormat format = PixelFormats.Bgr32; //RGB + alpha
            WriteableBitmap wbm = new WriteableBitmap(im.PixelWidth, im.PixelHeight, 96, 96, format, null);
            wbm.WritePixels(new Int32Rect(0, 0, im.PixelWidth, im.PixelHeight), outp, im.PixelWidth * 4, 0);
            return FromBitmapSource(wbm);
        }

        /// <summary>
        /// Гаусс
        /// </summary>
        /// <param name="img"></param>
        /// <param name="im"></param>
        /// <returns></returns>
        private static byte[] gs(byte[] img, BitmapImage im)
        {
            byte[] outp = new byte[img.Length];
            int w = im.PixelWidth;
            int h = im.PixelHeight;
            int channels = im.Format.BitsPerPixel / 8;

            if (GAUSS == null)
            {
                GAUSS = ComputeGauss(1);
            }

            Parallel.ForEach(Partitioner.Create(0, im.PixelWidth), a =>
            {
                for (int x = a.Item1; x < a.Item2; x++)
                {
                    for (int y = 0; y < h; y++)
                    {
                        double[] d = new double[channels];
                        double sum = 0;
                        for (int k1 = 0; k1 < GAUSS.Length; k1++)
                        {
                            int x_r = k1 - GAUSS.Length / 2 + x;
                            if (x_r < 0) x_r = 0;
                            if (x_r >= w) x_r = w - 1;

                            int ind = (x_r + y * w) * channels;

                            for (int mn = 0; mn < d.Length; mn++)
                            {
                                d[mn] += GAUSS[k1] * img[ind + mn];
                            }
                            sum += GAUSS[k1];
                        }

                        for (int mn = 0; mn < d.Length; mn++)
                        {
                            d[mn] /= sum;
                            if (d[mn] > 255) d[mn] = 255;
                            if (d[mn] < 0) d[mn] = 0;
                        }

                        int index = (x + y * w) * channels;

                        for (int mn = 0; mn < d.Length; mn++)
                        {
                            outp[index + mn] = (byte)d[mn];
                        }
                    }
                }
            });
            Parallel.ForEach(Partitioner.Create(0, im.PixelWidth), a =>
            {
                for (int x = a.Item1; x < a.Item2; x++)
                {
                    for (int y = 0; y < h; y++)
                    {
                        double[] d = new double[channels];
                        double sum = 0;
                        for (int k1 = 0; k1 < GAUSS.Length; k1++)
                        {
                            int x_r = k1 - GAUSS.Length / 2 + y;
                            if (x_r < 0) x_r = 0;
                            if (x_r >= h) x_r = h - 1;

                            int ind = (x + x_r * w) * channels;

                            for (int mn = 0; mn < d.Length; mn++)
                            {
                                d[mn] += GAUSS[k1] * outp[ind + mn];
                            }

                            sum += GAUSS[k1];
                        }

                        for (int mn = 0; mn < d.Length; mn++)
                        {
                            d[mn] /= sum;
                            if (d[mn] > 255) d[mn] = 255;
                            if (d[mn] < 0) d[mn] = 0;
                        }

                        int index = (x + y * w) * channels;

                        for (int mn = 0; mn < d.Length; mn++)
                        {
                            img[index + mn] = (byte)d[mn];
                        }
                    }
                }
            });
            return img;
        }


        /// <summary>
        /// Старт трансформаций
        /// </summary>
        /// <param name="img"></param>
        /// <param name="count_scales">Количество масштабов для каждой трансформации</param>
        /// <param name="count_shifts">Количество смещений для каждой трансформации</param>
        public static void StartTransforms(this BitmapImage img2, int count_scales2, int count_shifts2, int count_blurs2, int count_rotates2, float max_scal2, float from_angle2, float to_angle2, bool r2, bool g2, bool b2, int count_margins2, int from_margins2, int to_margins2)
        {
            CHANNELS_ALL = r2 || g2 || b2;
            data.Add(img2, new TransformsState() { count_scales = count_scales2, count_shifts = count_shifts2, img = img2, pix = !r2 && !g2 && !b2 ? img2.GrayScale() : img2.Pixels(), count_blurs = count_blurs2, count_rotates = count_rotates2, count_margins = count_margins2, from_margin = from_margins2, to_margin = to_margins2, count = count_shifts2 * count_scales2 * count_blurs2 * count_shifts2 * count_rotates2 * count_margins2, max_scale = max_scal2, from_angle = from_angle2, to_angle = to_angle2, r = r2, g = g2, b = b2, channel_n = r2 && g2 && b2 ? 3 : (r2 ^ g2 ^ b2 ? 1 : (r2 || g2 || b2 ? 2 : 0)) });
        }

        /// <summary>
        /// Проверяет, можно ли ещё сгенерировать что-нибудь для объекта
        /// </summary>
        /// <param name="img"></param>
        /// <returns></returns>
        public static bool HasNext(this BitmapImage img)
        {
            if (!data.ContainsKey(img)) return false;

            TransformsState state = data[img];
            return state.index_transform < state.count;
        }

        /// <summary>
        /// Пересчитать таблицу со значениями функции Гаусса
        /// </summary>
        /// <returns></returns>
        private static double[] ComputeGauss(float sigma)
        {
            int discrete_count = (int)(6 * sigma + 1);
            double[] dt = new double[discrete_count];
            float sigma_sq = sigma * sigma;
            for (int x = 0; x < discrete_count; x++)
            {
                int x_r = x - discrete_count / 2;
                dt[x] = Math.Sqrt(Math.Exp(-(x_r * x_r) / (sigma_sq)) / (Math.Sqrt(2 * Math.PI) * sigma));
            }
            return dt;
        }

        /// <summary>
        /// Пересчитать таблицу со значениями функции Гаусса
        /// </summary>
        /// <returns></returns>
        private static double[,] ComputeTableGauss(float sigma)
        {
            int discrete_count = (int)(6 * sigma + 1);
            double[,] dt = new double[discrete_count, discrete_count];
            float sigma_sq = sigma * sigma;
            for (int x = 0; x < discrete_count; x++)
            {
                for (int y = 0; y < discrete_count; y++)
                {
                    int x_r = x - discrete_count / 2;
                    int y_r = y - discrete_count / 2;
                    dt[x, y] = Math.Sqrt(Math.Exp(-(x_r * x_r + y_r * y_r) / (2 * sigma_sq)) / (Math.Sqrt(2 * Math.PI) * sigma));
                }
            }
            return dt;
        }

        /// <summary>
        /// Создать BitmapImage из BitmapSource
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static BitmapImage FromBitmapSource(BitmapSource src)
        {
            MemoryStream stream = new MemoryStream();
            JpegBitmapEncoder enc = new JpegBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(src));
            enc.Save(stream);

            BitmapImage img = new BitmapImage();
            img.BeginInit();
            img.DecodePixelWidth = 176;
            img.DecodePixelHeight = 144;
            img.StreamSource = new MemoryStream(stream.ToArray());
            img.EndInit();

            return img;
        }

        /// <summary>
        /// Изменить размер изображения
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static BitmapImage Resize(this BitmapSource src, int w, int h)
        {
            MemoryStream stream = new MemoryStream();
            JpegBitmapEncoder enc = new JpegBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(src));
            enc.Save(stream);

            BitmapImage img = new BitmapImage();
            img.BeginInit();
            img.DecodePixelWidth = w;
            img.DecodePixelHeight = h;
            img.StreamSource = new MemoryStream(stream.ToArray());
            img.EndInit();

            return img;
        }

        /// <summary>
        /// Производная по X
        /// </summary>
        /// <param name="img"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static int DerivativeX(byte[] a, int x, int y, int w, int h)
        {
            int x1 = Math.Abs(x - 1);
            int x2 = Math.Abs(x + 1);
            if (x2 >= w) x2 = w - 1;
            return a[x2 + y * w] - a[x1 + y * w];// -2 * a[x + y * w];
        }

        /// <summary>
        /// Производная по Y
        /// </summary>
        /// <param name="img"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static int DerivativeY(byte[] a, int x, int y, int w, int h)
        {
            int y1 = Math.Abs(y - 1);
            int y2 = Math.Abs(y + 1);
            if (y2 >= h) y2 = h - 1;
            return a[x + y2 * w] - a[x + y1 * w];// -2 * a[x + y * w];
        }

        /// <summary>
        /// Производная по X
        /// </summary>
        /// <param name="img"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static int DerivativeX(this BitmapImage img, byte[] a, int x, int y)
        {
            int t = img.Format.BitsPerPixel / 8;
            int x1 = Math.Abs(x - 1);
            int x2 = Math.Abs(x + 1);
            if (x2 >= img.PixelWidth) x2 = img.PixelWidth - 1;
            return a[(x2 + y * img.PixelWidth) * t] + a[(x1 + y * img.PixelWidth) * t] - 2 * a[(x + y * img.PixelWidth) * t];
        }

        /// <summary>
        /// Производная по Y
        /// </summary>
        /// <param name="img"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static int DerivativeY(this BitmapImage img, byte[] a, int x, int y)
        {
            int t = img.Format.BitsPerPixel / 8;
            int y1 = Math.Abs(y - 1);
            int y2 = Math.Abs(y + 1);
            if (y2 >= img.PixelHeight) y2 = img.PixelHeight - 1;
            return a[(x + y2 * img.PixelWidth) * t] + a[(x + y1 * img.PixelWidth) * t] - 2 * a[(x + y * img.PixelWidth) * t];
        }

        /// <summary>
        /// Производная по XY
        /// </summary>
        /// <param name="img"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static int DerivativeXY(this BitmapImage img, byte[] a, int x, int y)
        {
            int t = img.Format.BitsPerPixel / 8;
            int x1 = Math.Abs(x - 1);
            int y1 = Math.Abs(y - 1);
            int x2 = Math.Abs(x + 1);
            if (x2 >= img.PixelWidth) x2 = img.PixelWidth - 1;
            int y2 = Math.Abs(y + 1);
            if (y2 >= img.PixelHeight) y2 = img.PixelHeight - 1;
            return a[(x2 + y2 * img.PixelWidth) * t] + a[(x1 + y1 * img.PixelWidth) * t] - 2 * a[(x + y * img.PixelWidth) * t];
        }

        /// <summary>
        /// Детектор Харриса
        /// </summary>
        /// <param name="img"></param>
        /// <param name="pix"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static double Harris(this BitmapImage img, byte[] pix, int x, int y)
        {
            int Ix = img.DerivativeX(pix, x, y);
            int Iy = img.DerivativeY(pix, x, y);
            int Ixy = img.DerivativeXY(pix, x, y);
            Ix *= Ix;
            Iy *= Iy;
            return Ix * Iy - Ixy * Ixy - 0.06 * (Ix + Iy) * (Ix + Iy);
        }

        /// <summary>
        /// Детектор Харриса
        /// </summary>
        /// <param name="img"></param>
        /// <returns></returns>
        public static List<CPoint> Harris(this BitmapImage img)
        {
            List<CPoint> l = new List<CPoint>(1024);
            byte[] pix = img.GrayScale();
            for (int x = 0; x < img.PixelWidth; x++)
            {
                for (int y = 0; y < img.PixelHeight; y++)
                {
                    int Ix = img.DerivativeX(pix, x, y);
                    int Iy = img.DerivativeY(pix, x, y);
                    int Ixy = img.DerivativeXY(pix, x, y);
                    Ix *= Ix;
                    Iy *= Iy;
                    double Mc = Ix * Iy - Ixy * Ixy - 0.06 * (Ix + Iy) * (Ix + Iy);
                    if (Mc > 150)
                    {
                        CPoint c = null;
                        if ((c = l.Find(a => /*Math.Sqrt((x - a.X) * (x - a.X) + (y - a.Y) * (y - a.Y)) < 9*/Math.Abs(x - a.X) < 14 && Math.Abs(y - a.Y) < 14)) == null)
                            l.Add(new CPoint(x, y, Mc));
                        else if (c != null && c.Mc < Mc)
                        {
                            c.X += x;
                            c.X /= 2;
                            c.Y += y;
                            c.Y /= 2;
                            c.Mc += Mc;
                            c.Mc /= 2;
                            // l.Add(new CPoint(x, y, Mc));
                        }
                    }
                }
            }
            return l;
        }

        /// <summary>
        /// Генерирует следующий массив HoG
        /// </summary>
        /// <param name="img"></param>
        /// <returns></returns>
        public static double[][] GenerateNext(this BitmapImage img)
        {
            if (!img.HasNext()) throw new Exception("You should call StartTransforms to begin generating transforms.");

            TransformsState state = data[img];
            double KRAT = state.max_scale - 1;
            int all = state.index_transform;
            int index_scale = all % state.count_scales;
            all /= state.count_scales;
            int index_shift_x = all % state.count_shifts;
            all /= state.count_shifts;
            int index_shift_y = all % state.count_shifts;
            all /= state.count_shifts;
            int index_rotate = all % state.count_rotates;
            all /= state.count_rotates;
            int index_blur = all % state.count_blurs;
            all /= state.count_blurs;
            int index_margin = all % state.count_margins;

            double scale = 1d + KRAT * index_scale / (double)state.count_scales;
            double offset_x = img.PixelWidth * (scale - 1) * (double)index_shift_x / (double)state.count_shifts;
            double offset_y = img.PixelHeight * (scale - 1) * (double)index_shift_y / (double)state.count_shifts;

            int margin_current = index_margin * (state.to_margin - state.from_margin) / state.count_margins + state.from_margin;

            BitmapImage blurred = Gauss(state.pix, img, index_blur);

            WriteableBitmap img3 = new WriteableBitmap(blurred);
            img3 = img3.RotateFree((state.to_angle - state.from_angle) * Math.PI / 180.0 * index_rotate / state.count_rotates + state.from_angle);
            img3 = img3.Resize((int)(scale * img.PixelWidth), (int)(scale * img.PixelHeight), WriteableBitmapExtensions.Interpolation.Bilinear);
            img3 = img3.Crop((int)offset_x, (int)offset_y, ImageWidth, ImageHeight);

            img3 = img3.Crop(margin_current, margin_current, ImageWidth - 2 * margin_current, ImageHeight - 2 * margin_current);
            img3 = img3.Resize(ImageWidth, ImageHeight, WriteableBitmapExtensions.Interpolation.Bilinear);

            BitmapImage img2 = FromBitmapSource(img3);
            img2.HoG();
            double[][] d = new double[state.channel_n == 0 ? 1 : state.channel_n][];
            int nde = 0;
            if (state.r || !state.r && !state.g && !state.b)
                d[nde++] = GetHistogram(img2, 0, 0, img2.PixelWidth, img2.PixelHeight, 0);
            if (state.g)
                d[nde++] = GetHistogram(img2, 0, 0, img2.PixelWidth, img2.PixelHeight, 1);
            if (state.b)
                d[nde++] = GetHistogram(img2, 0, 0, img2.PixelWidth, img2.PixelHeight, 2);

            if (++state.index_transform >= state.count) data.Remove(img);
            return d;
        }

        /// <summary>
        /// Повернуть изображение на угол par (в радианах)
        /// </summary>
        /// <param name="r"></param>
        /// <param name="par"></param>
        /// <returns></returns>
        private static BitmapImage Rotate(BitmapImage r, double par)
        {
            if (Math.Abs((par % (Math.PI * 2))) < 0.0001) return r;
            int x = r.PixelWidth / 2;
            int y = r.PixelHeight / 2;
            int channels = r.Format.BitsPerPixel / 8;
            byte[] pix = r.GrayScale();

            byte[] new_pix = new byte[4 * r.PixelWidth * r.PixelHeight];

            double scale_x, scale_y;
            getScale(r.PixelWidth, r.PixelHeight, par, out scale_x, out scale_y);

            for (int i = 0; i < pix.Length; i += channels)
            {
                int ind = i / channels;
                int x_ = ind % r.PixelWidth;
                int y_ = ind / r.PixelWidth;

                double nx, ny;
                RotatePoint(x, y, x_, y_, par, out nx, out ny);
                nx -= x;
                ny -= y;
                nx /= scale_x;
                ny /= scale_y;

                int n_x = (int)nx + x;
                int n_y = (int)ny + y;
                if (n_x < 0 || n_y < 0 || n_x >= r.PixelWidth || n_y >= r.PixelHeight) continue;

                ind = (n_x + n_y * r.PixelWidth) * channels;
                new_pix[ind] = pix[i];
                new_pix[ind + 1] = pix[i];
                new_pix[ind + 2] = pix[i];
                new_pix[ind + 3] = 255;
            }

            PixelFormat format = PixelFormats.Bgr32; //RGB + alpha
            WriteableBitmap wbm = new WriteableBitmap(r.PixelWidth, r.PixelHeight, 96, 96, format, null);
            wbm.WritePixels(new Int32Rect(0, 0, r.PixelWidth, r.PixelHeight), new_pix, r.PixelWidth * 4, 0);
            return FromBitmapSource(wbm);
        }

        /// <summary>
        /// Вернуть коэффициенты масштаба (для вписывания всего повёрнутого изображения)
        /// </summary>
        /// <param name="w">Ширина изображения</param>
        /// <param name="h">Высота изображения</param>
        /// <param name="scale">Масштаб</param>
        /// <param name="sx">Результат по X</param>
        /// <param name="sy">Результат по Y</param>
        private static void getScale(int w, int h, double scale, out double sx, out double sy)
        {
            // считаем координаты всех углов, находим разницу между максимальными и минимальными значениями, отношения записываем в sx и sy
            double x1, y1; // левый верхний угол
            double x2, y2; // правый верхний угол
            double x3, y3; // левый нижний угол
            double x4, y4; // правый нижний угол

            int x = w / 2, y = h / 2; // центр поворота

            RotatePoint(x, y, 0, 0, scale, out x1, out y1);
            RotatePoint(x, y, w - 1, 0, scale, out x2, out  y2);
            RotatePoint(x, y, 0, h - 1, scale, out  x3, out y3);
            RotatePoint(x, y, w - 1, h - 1, scale, out x4, out y4);

            double x_min = Math.Min(Math.Min(x1, x3), Math.Min(x2, x4));
            double x_max = Math.Max(Math.Max(x1, x3), Math.Max(x2, x4));
            double y_min = Math.Min(Math.Min(y1, y3), Math.Min(y2, y4));
            double y_max = Math.Max(Math.Max(y1, y3), Math.Max(y2, y4));

            sx = (x_max - x_min) / (double)w;
            sy = (y_max - y_min) / (double)h;
        }

        /// <summary>
        /// Повернуть точку
        /// </summary>
        /// <param name="x">X центра</param>
        /// <param name="y">Y центра</param>
        /// <param name="x_">X поворачиваемой точки</param>
        /// <param name="y_">Y поворачиваемой точки</param>
        /// <param name="par">Угол в радианах</param>
        /// <param name="nx">X результат</param>
        /// <param name="ny">Y результат</param>
        private static void RotatePoint(int x, int y, int x_, int y_, double par, out double nx, out double ny)
        {
            double r_ = Math.Sqrt((x_ - x) * (x_ - x) + (y_ - y) * (y_ - y));
            double angle = Math.Atan2(y_ - y, x_ - x); // исходный угол

            // координаты пикселя на новом изображении
            nx = x + r_ * Math.Cos(angle + par);
            ny = y + r_ * Math.Sin(angle + par);
        }

        /// <summary>
        /// Получить время в мс
        /// </summary>
        /// <returns></returns>
        public static double CurrentTimeMillis()
        {
            return (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
        }

        /// <summary>
        /// Описывает модель трансформируемого изображения
        /// </summary>
        private class TransformsState
        {
            public BitmapImage img; // изображение
            public byte[] pix; // массив пикселей grayscale изображения
            public float max_scale;
            public int index_transform; // индекс текущего преобразования
            public int count_scales, count_shifts, count_rotates, count_blurs, count; // количество масштабирований, смещений; общее кол-во
            public float from_angle, to_angle;
            public bool r, g, b;
            public int channel_n, count_margins, from_margin, to_margin;
        }
    }

    public struct BindedData : ISortable
    {
        public object Data;
        public int Key { get; set; }

        public BindedData(int k, object data)
            : this()
        {
            Key = k;
            Data = data;
        }
    }

    public interface ISortable
    {
        int Key { get; set; }
    }
}
