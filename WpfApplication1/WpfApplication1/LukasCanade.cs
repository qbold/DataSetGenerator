using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace WpfApplication1
{
    /// <summary>
    /// Алгоритм Лукаса-Канаде
    /// </summary>
    class LukasCanade
    {
        private byte[][] old;
        private List<CPoint> points;
        private int window, pyr_depth;

        /// <summary>
        /// Размер окрестности
        /// </summary>
        public int WindowSize { set { if (value < 0) value = 0; if (value % 2 == 0) value++; window = value; } get { return window; } }
        public int PyramidDepth { get { return pyr_depth - 1; } set { pyr_depth = value + 1; } }

        public LukasCanade()
        {
            pyr_depth = 4;
            window = 5;
            points = new List<CPoint>();
        }

        /// <summary>
        /// Очистить список
        /// </summary>
        public void Clear()
        {
            points.Clear();
        }

        /// <summary>
        /// Добавить точку для слежения
        /// </summary>
        /// <param name="p"></param>
        public void AddPoint(CPoint p)
        {
            points.Add(p);
        }

        /// <summary>
        /// Удалить точку из списка слежения
        /// </summary>
        /// <param name="p"></param>
        public void RemovePoint(CPoint p)
        {
            points.Remove(p);
        }

        /// <summary>
        /// Инициализация
        /// </summary>
        /// <param name="img"></param>
        public void Start(BitmapImage img)
        {
            old = new byte[pyr_depth][];
            int sz = 1;
            for (int i = 0; i < pyr_depth; i++, sz *= 2)
            {
                old[i] = img.Resize(img.PixelWidth / sz, img.PixelHeight / sz).Gray();
            }
        }

        /// <summary>
        /// Обновить данные потока
        /// </summary>
        /// <param name="data"></param>
        /// <param name="b"></param>
        public void UpdateOpticalFlow(BitmapImage ba)
        {
            int[] pow2 = new int[pyr_depth];
            pow2[0] = 1;
            for (int i = 1; i < pow2.Length; i++)
            {
                pow2[i] = pow2[i - 1] * 2;
            }

            foreach (CPoint pn in points)
            {
                pn.Vx = 0;
                pn.Vy = 0;
            }

            int q = pyr_depth - 1;
            while (q >= 0)
            {
                BitmapImage b = ba.Resize(ba.PixelWidth / pow2[q], ba.PixelHeight / pow2[q]);
                byte[] data = b.Gray();

                int w = b.PixelWidth, h = b.PixelHeight;
                double a1 = 0, a2 = 0, a3 = 0, b1 = 0, b2 = 0;

                List<Pixel> IX = new List<Pixel>(window * window);
                List<Pixel> IY = new List<Pixel>(window * window);
                List<Pixel> IT = new List<Pixel>(window * window);

                int window_ = window / 2;

                foreach (CPoint pn in points)
                {
                    int X = pn.X;
                    int Y = pn.Y;

                    // Вычисление частных производных
                    for (int i = 0; i < window; i++)
                    {
                        int x = X - window_ + i;
                        if (x < 0 || x >= w) continue;
                        for (int j = 0; j < window; j++)
                        {
                            int y = Y - window_ + j;
                            if (y < 0 || y >= h) continue;
                            IX.Add(new Pixel(x, y, Functions.DerivativeX(data, x, y, b.PixelWidth, b.PixelHeight)));
                            IY.Add(new Pixel(x, y, Functions.DerivativeY(data, x, y, b.PixelWidth, b.PixelHeight)));
                            IT.Add(new Pixel(x, y, data[x + y * w] - old[q][x + y * w]));
                        }
                    }

                    int n = IX.Count;

                    // вычисление элементов матриц
                    for (int i = 0; i < n; i++)
                    {
                        double ix = IX[i].Color, iy = IY[i].Color, it = IT[i].Color;
                        a1 += ix * ix;
                        a3 += iy * iy;
                        a2 += ix * iy;
                        b1 -= it * ix;
                        b2 -= it * iy;
                    }

                    // приближённое решение методом наименьших квадратов
                    double div = (double)pow2[q] / (a1 * a3 - a2 * a2);
                    pn.Vx += div * (b1 * a3 - b2 * a2);
                    pn.Vy += div * (b2 * a1 - b1 * a2);

                    IX.Clear();
                    IY.Clear();
                    IT.Clear();
                }
                old[q] = data;
                q--;
            }
        }
    }
}
