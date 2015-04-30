using System;

namespace WpfApplication1
{
    /// <summary>
    /// Класс, описывающий систему адаптивного подбора приоритетов, необходимого для оптимизации подбора областей
    /// Если надо вернуть table, нужно заменить все table3 на table
    /// </summary>
    class AdaptivePriorityOptimizer
    {
        /// <summary>
        /// Таблица плотности частоты с учётом координат
        /// </summary>
        //  private float[/*,*/ , ,] table;
        /// <summary>
        /// И без учёта
        /// </summary>
        private float[,] table2;
        /// <summary>
        /// Одни координаты
        /// </summary>
        private float[,] table3;

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
        private PriorityQueue<CPoint[]> queue;

        /// <summary>
        /// Сигма - параметр для гауссова распределения
        /// </summary>
        public float Sigma
        {
            set
            {
                sigma = value;
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
        public float this[/*int x,*/int y, int z, int w]
        {
            get
            {
                return GetValue3(/*x,*/ y, z, w);
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
            : this(/*26,26,*/52, 18, 15)
        {
        }

        public AdaptivePriorityOptimizer(/*int numa,*/int numb, int numc, int numd)
        {
            w = numb;//numa
            h = numb;
            w2 = numc;
            h2 = numd;
            table3 = new float[/*numa, numb,*/ numc, numd];
            table2 = new float[numb, numb];
            Sigma = 5;
            t = 1;
            queue = new PriorityQueue<CPoint[]>(1024);
        }

        /// <summary>
        /// Очередь
        /// </summary>
        public PriorityQueue<CPoint[]> Queue { get { return queue; } }

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
        public void AddPoint(CPoint a, CPoint b)
        {
            if (state == PREPROCESSED)
            {
                queue.Clear();
                state = NOT_PREPROCESSED;
            }
            float ft = table3[/*a.Color/10,b.Color/10*//*(a.Color + b.Color) / 10, */(b.X - a.X) / 10, (b.Y - a.Y) / 10];
            float fg = table2[a.Color / 10, b.Color / 10];
            queue.Add(new CPoint[] { a, b }, ft * ft + fg * fg - (float)(Math.Abs(a.X - Functions.ImageWidth / 2) + Math.Abs(b.X - Functions.ImageWidth / 2) + Math.Abs(a.Y - Functions.ImageHeight / 2) + Math.Abs(b.Y - Functions.ImageHeight / 2)) / (2f * (Functions.ImageWidth + Functions.ImageHeight)));
        }

        public bool IsSmall(int x_, int y_, int dx, int dy)
        {
            int c1 = x_ / 10;
            int c2 = (x_ + y_) / 10;
            int c3 = dx / 10;
            int c4 = dy / 10;
            return table3[c3, c4] * 4 < t && table2[c1, c2] * 4 < t;
        }

        /// <summary>
        /// Получить следующие точки
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public void GetPoints(out CPoint a, out CPoint b)
        {
            if (state == NOT_PREPROCESSED)
            {
                queue.buildHeap();
                state = PREPROCESSED;
            }
            CPoint[] pc = queue.Get();
            a = pc[0];
            b = pc[1];
        }

        /// <summary>
        /// Добавить влияние нового объекта с координатами (x_; y_)
        /// </summary>
        /// <param name="x_"></param>
        /// <param name="y_"></param>
        public void Add(int x_, int y_, int dx, int dy, float coef = 1)
        {
            int c1 = x_ / 10;
            int c2 = (x_ + y_) / 10;
            int c3 = dx / 10;
            int c4 = dy / 10;
            table3[/*c1,*/ /*c2,*/ c3, c4] += t * coef;
            if (table3[c3, c4] < 0) table3[c3, c4] = 0;
            if (table3[/*c1,*/ /*c2,*/ c3, c4] > maximum) maximum = table3[/*c1,*/ /*c2,*/ c3, c4];
            int k = 0;
            float t3 = t * coef;
            while (k++ < 5)
            {
                float t2 = t3 / k;
                for (int q2 = c2 - k; q2 <= c2 + k; q2++)
                {
                    if (q2 < 0) continue;
                    if (q2 >= h) break;
                    if (q2 > c2 - k && q2 < c2 + k) continue;


                    for (int q = c1 - k; q <= c1 + k; q++)
                    {
                        if (q < 0) continue;
                        if (q >= w) break;
                        if (q > c1 - k && q < c1 + k) continue;
                        table2[q, q2] += t2;
                        if (table2[q, q2] < 0) table2[q, q2] = 0;
                    }
                }

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
                        if (/*q2 == c2 &&*/ q3 == c3 && q4 == c4)
                        {
                            continue;
                        }
                        table3[/*q,*/ /*q2,*/ q3, q4] += t2;
                        if (table3[q3, q4] < 0) table3[q3, q4] = 0;
                        if (table3[/*q,*//* q2,*/ q3, q4] > maximum) maximum = table3[/*q,*//*q2,*/ q3, q4];
                    }
                }
            }
            t = maximum / 2;
        }

        /// <summary>
        /// Получить значение плотности в заданной точке
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public float GetValue3(int x, /*y,*/ int z, int w)
        {
            return table3[/*x,*//*y,*/  z, w];
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
