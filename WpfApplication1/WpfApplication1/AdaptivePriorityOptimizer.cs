using System;

namespace WpfApplication1
{
    /// <summary>
    /// Класс, описывающий систему адаптивного подбора приоритетов, необходимого для оптимизации подбора областей
    /// </summary>
    class AdaptivePriorityOptimizer
    {
        /// <summary>
        /// Таблица плотности частоты с учётом координат
        /// </summary>
        private float[, , ,] table;
        /// <summary>
        /// И без учёта
        /// </summary>
        private float[,] table2;

        private int w, h, w2, h2;
        private float sigma;

        /// <summary>
        /// Коэффициент новых элементов
        /// </summary>
        private float t;

        /// <summary>
        /// Максимальный элемент
        /// </summary>
        private float maximum;

        /// <summary>
        /// Состояние оптимизатора
        /// </summary>
        private byte state;
        private const byte NOT_PREPROCESSED = 0, PREPROCESSED = 1;

        /// <summary>
        /// Приоритетная очередь
        /// </summary>
        private PriorityQueue<ImageFunctions.CPoint[]> queue;

        /// <summary>
        /// Дискретная матрица значения функции Гауссова распределения
        /// </summary>
        //private float[,] GAUSS;

        /// <summary>
        /// Радиус дискретной области, в рамках которой будут происходить изменения
        /// </summary>
        //private int discrete_count;

        /// <summary>
        /// Сигма - параметр для гауссова распределения
        /// </summary>
        public float Sigma
        {
            set
            {
                sigma = value;
                // discrete_count = (int)(sigma * 3 + 0.5);
                //  GAUSS = ComputeGauss();
            }
            get
            {
                return sigma;
            }
        }

        /// <summary>
        /// Количество объектов
        /// </summary>
        public int Count
        {
            get { return queue.Count; }
        }

        /// <summary>
        /// Вернуть плотность в заданных координатах
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public float this[int x, int y, int z, int w]
        {
            get
            {
                return GetValue(x, y, z, w);
            }
        }

        /// <summary>
        /// Вернуть плотность в заданных координатах
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public float this[int x, int y]
        {
            get
            {
                return GetValue(x, y);
            }
        }

        public AdaptivePriorityOptimizer()
            : this(26, 26, 18, 15)
        {
        }

        public AdaptivePriorityOptimizer(int numa, int numb, int numc, int numd)
        {
            w = numa;
            h = numb;
            w2 = numc;
            h2 = numd;
            table = new float[numa, numb, numc, numd];
            table2 = new float[numa, numb];
            Sigma = 5;
            t = 1;
            queue = new PriorityQueue<ImageFunctions.CPoint[]>(1024);
        }

        /// <summary>
        /// Пустой ли?
        /// </summary>
        /// <returns></returns>
        public bool Empty()
        {
            return queue.Empty();
        }

        /// <summary>
        /// Добавить точки
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public void AddPoint(ImageFunctions.CPoint a, ImageFunctions.CPoint b)
        {
            if (state == PREPROCESSED)
            {
                queue.Clear();
                state = NOT_PREPROCESSED;
            }
            float ft = table[a.Color / 10, b.Color / 10, (b.X - a.X) / 10, (b.Y - a.Y) / 10];
            float fg = table2[a.Color / 10, b.Color / 10];
            queue.Add(new ImageFunctions.CPoint[] { a, b }, ft * ft + fg * fg / 2 - (float)(Math.Abs(a.X - ImageFunctions.ImageWidth / 2) + Math.Abs(b.X - ImageFunctions.ImageWidth / 2) + Math.Abs(a.Y - ImageFunctions.ImageHeight / 2) + Math.Abs(b.Y - ImageFunctions.ImageHeight / 2)) / (2f * (ImageFunctions.ImageWidth + ImageFunctions.ImageHeight)));
        }

        /// <summary>
        /// Получить следующие точки
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public void GetPoints(out ImageFunctions.CPoint a, out ImageFunctions.CPoint b)
        {
            if (state == NOT_PREPROCESSED)
            {
                //long s = ImageFunctions.CurrentTimeMillis();
                queue.buildHeap();
                // System.Windows.MessageBox.Show("buildHeap " + (ImageFunctions.CurrentTimeMillis() - s));
                state = PREPROCESSED;
            }
            ImageFunctions.CPoint[] pc = queue.Get();
            a = pc[0];
            b = pc[1];
        }

        /// <summary>
        /// Добавить влияние нового объекта с координатами (x_; y_)
        /// </summary>
        /// <param name="x_"></param>
        /// <param name="y_"></param>
        public void AddCorrect(int x_, int y_, int dx, int dy)
        {
            /* int dv = discrete_count / 2;
             for (int x = 0; x < discrete_count; x++)
             {
                 int x_r = x - dv + x_;
                 if (x_r < 0) continue;
                 if (x_r >= w) break;
                 for (int y = 0; y < discrete_count; y++)
                 {
                     int y_r = y - dv + y_;
                     if (y_r < 0) continue;
                     if (y_r >= h) break;
                     table[x_r, y_r] += GAUSS[x, y];
                 }
             }*/
            int c1 = x_ / 10;
            int c2 = y_ / 10;
            int c3 = dx / 10;
            int c4 = dy / 10;
            table[c1, c2, c3, c4] += t;
            if (table[c1, c2, c3, c4] > maximum) maximum = table[c1, c2, c3, c4];
            int k = 0;
            float t2 = t;
            while (k++ < 5)
            {
                t2 /= 2;
                for (int q = c1 - k; q <= c1 + k; q++)
                {
                    if (q < 0) continue;
                    if (q >= w) break;
                    if (q > c1 - k && q < c1 + k) continue;
                    for (int q2 = c2 - k; q2 <= c2 + k; q2++)
                    {
                        if (q2 < 0) continue;
                        if (q2 >= h) break;
                        if (q2 > c2 - k && q2 < c2 + k) continue;


                        table2[q, q2] += t2;

                        for (int q3 = c3 - k; q3 <= c3 + k; q3++)
                        {
                            if (q3 < 0) continue;
                            if (q3 >= w2) break;
                            if (q3 > c3 - k && q3 < c3 + k) continue;
                            for (int q4 = c4 - k; q4 <= c4 + k; q4++)
                            {
                                if (q4 < 0) continue;
                                if (q4 >= h2) break;
                                if (q4 > c4 - k && q4 < c4 + k) continue;
                                if (q == c1 && q2 == c2 && q3 == c3 && q4 == c4)
                                {
                                    continue;
                                }
                                table[q, q2, q3, q4] += t2;
                                if (table[q, q2, q3, q4] > maximum) maximum = table[q, q2, q3, q4];
                            }
                        }
                    }
                }
            }
            t = maximum / 2;
        }

        /// <summary>
        /// Пересчитать таблицу со значениями функции Гаусса
        /// </summary>
        /// <returns></returns>
        private float[,] ComputeGauss()
        {
            /*  float[,] dt = new float[discrete_count, discrete_count];
              float sigma_sq = sigma * sigma;
              for (int x = 0; x < discrete_count; x++)
              {
                  for (int y = 0; y < discrete_count; y++)
                  {
                      dt[x, y] = (float)(1f / (Math.Sqrt(2 * Math.PI) * sigma) * Math.Exp(-(x * x + y * y) / (2 * sigma_sq)));
                  }
              }
              return dt;*/
            return null;
        }

        /// <summary>
        /// Получить значение плотности в заданной точке
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public float GetValue(int x, int y, int z, int w)
        {
            return table[x, y, z, w];
        }

        /// <summary>
        /// Получить значение плотности в заданной точке
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public float GetValue(int x, int y)
        {
            return table2[x, y];
        }
    }
}
