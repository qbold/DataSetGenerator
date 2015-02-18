using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WpfApplication1
{
    /// <summary>
    /// Описывает функции, применяемые к изображениям
    /// </summary>
    public static class ImageFunctions
    {

        private static Dictionary<BitmapImage, TransformsState> data; // Хранит данные о количестве сгенерированных трансформаций

        static ImageFunctions()
        {
            data = new Dictionary<BitmapImage, TransformsState>();
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
            for (int i = 1 - 1; i < pix.Length; i += channels)
            {
                try
                {
                    pix[i] = (byte)(0.299 * pix[i] + 0.587 * pix[i + 1] + 0.114 * pix[i + 2]);
                    pix[i + 1] = pix[i];
                    pix[i + 2] = pix[i];
                }
                catch (Exception e)
                {
                    MessageBox.Show("Error" + channels);
                }
            }
            return pix;
        }

        /// <summary>
        /// Генерация HoG
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static double[] HoG(this BitmapImage b)
        {
            return HoG(GrayScale(b), b);
        }

        private static int cnt;

        /// <summary>
        /// Построение гистограммы ориентированных градиентов
        /// </summary>
        /// <param name="pix"></param>
        /// <returns></returns>
        private static double[] HoG(byte[] pix, BitmapImage b)
        {
            int w = b.PixelWidth;
            int h = b.PixelHeight;

            int channels = b.Format.BitsPerPixel / 8;

            int count_pixels_in_cell = 8;
            int count_cells_in_block = 4;
            int step_block = 2;
            int count_bins = 8;

            // Вычисляем направления (углы) градиента изображения и записываем в массив гистограмму по ячейке
            int count_cells_x = w / count_pixels_in_cell; // количество ячеек по x
            int count_cells_y = h / count_pixels_in_cell; // количество ячеек по y

            byte[] pix2 = new byte[pix.Length];

            int[,] cells = new int[count_bins, count_cells_x * count_cells_y]; // массив, в котором будут храниться гистограммы для каждой ячейки
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
                    int Dx = pix[(i2 + sess_index_y) * channels + 1 - 1] - pix[(i1 + sess_index_y) * channels + 1 - 1]; // градиент по x
                    int Dy = pix[(i + next_index_y) * channels + 1 - 1] - pix[(i + prev_index_y) * channels + 1 - 1]; // градиент по y

                    Dx *= Dx;
                    Dy *= Dy;

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
                    // if (bin == count_bins) 
                    bin--;

                    int px = (i + sess_index_y) * channels;
                    pix2[px] = (byte)(bin * 256d / count_bins);
                    pix2[px + 1] = pix2[px];
                    pix2[px + 2] = pix2[px];
                    pix2[px + 3] = 255;

                    cells[bin, i / count_pixels_in_cell + (j / count_pixels_in_cell) * count_cells_x]++;
                }
            }

            //SaveJpeg(pix2, w, h, channels * b.PixelWidth, "1/filename" + cnt++ + ".jpg");
            //MessageBox.Show("e");

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

        /// <summary>
        /// Сохранение изображения в формате Jpeg
        /// </summary>
        /// <param name="img"></param>
        private static void SaveJpeg(BitmapSource s, string filename)
        {
            using (FileStream stream = new FileStream(filename, FileMode.Create))
            {
                JpegBitmapEncoder coder = new JpegBitmapEncoder();
                coder.Frames.Add(BitmapFrame.Create(s));
                coder.Save(stream);
            }
        }

        /// <summary>
        /// Гауссово сглаживание
        /// </summary>
        /// <param name="img"></param>
        /// <returns></returns>
        private static BitmapImage Gauss(byte[] img, BitmapImage im, int k)
        {
            if (k == 0) return im;
            byte[] outp = new byte[img.Length];

            double[] GAUSS = { 0.028, 0.23, 0.47, 0.23, 0.028 };
            for (int x = 0; x < im.PixelWidth; x++)
            {
                for (int y = 0; y < im.PixelHeight; y++)
                {
                    double red = 0;
                    for (int k1 = 0; k1 < GAUSS.Length; k1++)
                    {
                        int x_r = k1 - GAUSS.Length / 2 + x;
                        if (x_r < 0) x_r = 0;
                        if (x_r >= im.PixelWidth) x_r = im.PixelWidth - 1;
                        red += GAUSS[k1] * img[(x_r + y * im.PixelWidth) * 4];
                    }
                    int index = (x + y * im.PixelWidth) * 4;
                    if (red > 255) red = 255;
                    if (red < 0) red = 0;
                    outp[index] = (byte)red;
                    outp[index + 1] = outp[index];
                    outp[index + 2] = outp[index];
                    outp[index + 3] = 255;
                }
            }
            for (int x = 0; x < im.PixelWidth; x++)
            {
                for (int y = 0; y < im.PixelHeight; y++)
                {
                    double red = 0;
                    for (int k1 = 0; k1 < GAUSS.Length; k1++)
                    {
                        int x_r = k1 - GAUSS.Length / 2 + y;
                        if (x_r < 0) x_r = 0;
                        if (x_r >= im.PixelHeight) x_r = im.PixelHeight - 1;
                        red += GAUSS[k1] * img[(x + x_r * im.PixelWidth) * 4];
                    }
                    int index = (x + y * im.PixelWidth) * 4;
                    if (red > 255) red = 255;
                    if (red < 0) red = 0;
                    outp[index] = (byte)red;
                    outp[index + 1] = outp[index];
                    outp[index + 2] = outp[index];
                    outp[index + 3] = 255;
                }
            }

            if (k > 0) return Gauss(outp, im, k - 1);
            else
            {
                PixelFormat format = PixelFormats.Bgr32; //RGB + alpha
                WriteableBitmap wbm = new WriteableBitmap(im.PixelWidth, im.PixelHeight, 96, 96, format, null);
                wbm.WritePixels(new Int32Rect(0, 0, im.PixelWidth, im.PixelHeight), outp, im.PixelWidth * 4, 0);
                return FromBitmapSource(wbm);
            }
        }

        /// <summary>
        /// Старт трансформаций
        /// </summary>
        /// <param name="img"></param>
        /// <param name="count_scales">Количество масштабов для каждой трансформации</param>
        /// <param name="count_shifts">Количество смещений для каждой трансформации</param>
        public static void StartTransforms(this BitmapImage img2, int count_scales2, int count_shifts2, int count_blurs2, int count_rotates2)
        {
            data.Add(img2, new TransformsState() { count_scales = count_scales2, count_shifts = count_shifts2, img = img2, pix = img2.GrayScale(), count_blurs = count_blurs2, count_rotates = count_rotates2, count = count_shifts2 * count_scales2 * count_blurs2 * count_shifts2 * count_rotates2 });
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
        /// Генерирует следующий массив HoG
        /// </summary>
        /// <param name="img"></param>
        /// <returns></returns>
        public static double[] GenerateNext(this BitmapImage img)
        {
            if (!img.HasNext()) throw new Exception("You should call StartTransforms to begin generating transforms.");

            TransformsState state = data[img];
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

            double scale = 1d + 0.5d * index_scale / (double)state.count_scales;
            double offset_x = img.PixelWidth * (scale - 1) * (double)index_shift_x / (double)state.count_shifts;
            double offset_y = img.PixelHeight * (scale - 1) * (double)index_shift_y / (double)state.count_shifts;

            double center_x = (offset_x + img.PixelWidth / 2) / scale;
            double center_y = (offset_y + img.PixelHeight / 2) / scale;

            // MessageBox.Show(index_blur + " ");

            BitmapImage blurred = Gauss(state.pix, img, index_blur);
            TransformedBitmap img3 = new TransformedBitmap(blurred, new RotateTransform(360d * index_rotate / state.count_rotates, img.PixelWidth / 2, img.PixelHeight / 2));
            CroppedBitmap img31 = new CroppedBitmap(img3, new Int32Rect(img3.PixelWidth / 2 - img.PixelWidth / 2, img3.PixelHeight / 2 - img.PixelHeight / 2, img.PixelWidth, img.PixelHeight));

            BitmapSource source = new CroppedBitmap(new TransformedBitmap(img31, new ScaleTransform(scale, scale, center_x, center_y)), new Int32Rect((int)offset_x, (int)offset_y, img.PixelWidth, img.PixelHeight));
            // SaveJpeg(source, "1/" + state.index_transform + ".jpg");
            BitmapImage img2 = FromBitmapSource(source);

            double[] d = HoG(img2.GrayScale(), img2);
            if (++state.index_transform >= state.count) data.Remove(img);
            return d;
        }

        /// <summary>
        /// Описывает модель трансформируемого изображения
        /// </summary>
        private class TransformsState
        {
            public BitmapImage img; // изображение
            public byte[] pix; // массив пикселей grayscale изображения
            public int index_transform; // индекс текущего преобразования
            public int count_scales, count_shifts, count_rotates, count_blurs, count; // количество масштабирований, смещений; общее кол-во
        }
    }
}
