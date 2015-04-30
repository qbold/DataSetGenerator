using System.Collections.Generic;
using libsvm;
using System.Reflection;
using System.Windows.Media.Imaging;
using System.Collections.Concurrent;
using System;

namespace WpfApplication1
{
    public delegate List<CPoint[]> PrepareAdaptiveOptimizer(BitmapImage img);
    /// <summary>
    /// Класс для работы с machine learning
    /// </summary>
    public static class ML
    {
        /// <summary>
        /// Оптимизаторы
        /// </summary>
        private static Dictionary<SVM, AdaptivePriorityOptimizer[]> opt;

        private static int PARALLEL_DEGREE;

        private static PrepareAdaptiveOptimizer HARRIS_PREPARE = (img) =>
        {
            List<CPoint> h = img.Harris();
            List<CPoint[]> data = new List<CPoint[]>(h.Count * h.Count);

            foreach (CPoint p in h)
            {
                foreach (CPoint p2 in h)
                {
                    if (p == p2) continue;
                    int wa = p2.X - p.X;
                    int ha = p2.Y - p.Y;
                    if (wa < Functions.ImageWidth / 4 || ha < Functions.ImageHeight / 4) continue;
                    if (p2.X > p.X && p2.Y > p.Y && 3 * (p2.X - p.X) > p2.Y - p.Y && 3 * (p2.Y - p.Y) > p2.X - p.X)
                    {
                        data.Add(new CPoint[] { p, p2 });
                    }
                }
            }

            return data;
        },
        SLIDE_PREPARE = (img) =>
        {
            List<CPoint[]> data = new List<CPoint[]>(1024);
            byte[] gray = img.GrayScale();

            int cnt_x_ = 4, cnt_y_ = 4;
            int scales = 3;

            double step_scale_x = (double)(cnt_x_ - 1) / (double)scales;
            double step_scale_y = (double)(cnt_y_ - 1) / (double)scales;

            for (int s = 0; s < scales; s++)
            {
                double cnt_x = (cnt_x_ - step_scale_x * s);
                double cnt_y = (cnt_y_ - step_scale_y * s);

                int step_x = (int)(img.PixelWidth / cnt_x);
                int step_y = (int)(img.PixelHeight / cnt_y);

                for (int x = 0; x < img.PixelWidth - step_x; x += 10)
                {
                    for (int y = 0; y < img.PixelHeight - step_y; y += 10)
                    {
                        CPoint a = new CPoint(x, y, img.Harris(gray, x, y));
                        CPoint b = new CPoint(x + step_x, y + step_y, img.Harris(gray, x + step_x - 1, y + step_y - 1));
                        data.Add(new CPoint[] { a, b });
                    }
                }
            }
            return data;
        };

        static ML()
        {
            opt = new Dictionary<SVM, AdaptivePriorityOptimizer[]>();
            PARALLEL_DEGREE = 1;
        }

        /// <summary>
        /// Возвращает модель обученного SVM
        /// </summary>
        /// <returns></returns>
        public static SVM getSVMObject(List<List<double>> set)
        {
            var v = ProblemHelper.ReadProblem(set);
            var svm = new C_SVC(v, KernelHelper.RadialBasisFunctionKernel(0.5), 1, 100, true);//RadialBasisFunctionKernel(0.5)
            //svm_model model = (svm_model)(typeof(SVM).GetField("model", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(svm));
            AdaptivePriorityOptimizer[] ad = new AdaptivePriorityOptimizer[PARALLEL_DEGREE];
            for (int i = 0; i < ad.Length; i++)
            {
                ad[i] = new AdaptivePriorityOptimizer();
            }
            opt.Add(svm, ad);
            return svm;
        }

        /// <summary>
        /// Сделать из массива ноды для svm
        /// </summary>
        /// <param name="ar"></param>
        /// <returns></returns>
        public static svm_node[] GetNodes(double[] ar)
        {
            svm_node[] nodes = new svm_node[ar.Length];
            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i] = new svm_node();
                nodes[i].index = i + 1;
                nodes[i].value = ar[i];
            }
            return nodes;
        }

        /// <summary>
        /// Возвращает модель обученного SVM
        /// </summary>
        /// <returns></returns>
        public static svm_model getSVM(List<List<double>> set)
        {
            var v = ProblemHelper.ReadProblem(set);
            var svm = new C_SVC(v, KernelHelper.RadialBasisFunctionKernel(0.5), 1, 100, true);//RadialBasisFunctionKernel(0.5)
            svm_model model = (svm_model)(typeof(SVM).GetField("model", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(svm));
            return model;
        }

        /// <summary>
        /// Загружает SVM из файла
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static SVM getSVMObject(string file)
        {
            var svm = new C_SVC(file);
            AdaptivePriorityOptimizer[] ad = new AdaptivePriorityOptimizer[PARALLEL_DEGREE];
            for (int i = 0; i < ad.Length; i++)
            {
                ad[i] = new AdaptivePriorityOptimizer();
            }
            opt.Add(svm, ad);
            return svm;
        }

        /// <summary>
        /// Распознать изображение BitmapImage
        /// </summary>
        /// <param name="svm"></param>
        /// <param name="img"></param>
        /// <returns></returns>
        public static ConcurrentQueue<RecognizedRectangle> RecognizeAll(this SVM svm, BitmapImage img, out int n)
        {
            return RecognizeAll(new SVM[] { svm }, img, out n);
        }

        /// <summary>
        /// Харрисом
        /// </summary>
        /// <param name="svm"></param>
        /// <param name="img"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public static ConcurrentQueue<RecognizedRectangle> RecognizeAll(this SVM[] svm, BitmapImage img, out int n)
        {
            return svm.RecognizeAll(img, out n, HARRIS_PREPARE);
        }

        /// <summary>
        /// Скользящим окном
        /// </summary>
        /// <param name="svm"></param>
        /// <param name="img"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public static ConcurrentQueue<RecognizedRectangle> RecognizeAllSlide(this SVM[] svm, BitmapImage img, out int n)
        {
            return svm.RecognizeAll(img, out n, SLIDE_PREPARE);
        }

        /// <summary>
        /// Распознать изображение BitmapImage мультиклассификатором
        /// </summary>
        /// <param name="svm"></param>
        /// <param name="img"></param>
        /// <returns></returns>
        public static ConcurrentQueue<RecognizedRectangle> RecognizeAll(this SVM[] svm, BitmapImage img, out int n, PrepareAdaptiveOptimizer pr)
        {
            int max_size = 1;
            ConcurrentQueue<RecognizedRectangle> l = new ConcurrentQueue<RecognizedRectangle>();
            int num = 0;
            int indx = 0;
            List<CPoint[]> points = pr(img);
            foreach (CPoint[] p in points)
            {
                indx = (indx + 1) % opt[svm[0]].Length;
                for (int i = 0; i < svm.Length; i++)
                {
                    opt[svm[i]][indx].AddPoint(p[0], p[1]);
                }
            }
            AdaptivePriorityOptimizer a2 = opt[svm[0]][0];
            img.HoG(0, 0, 0, 0);
            double d2 = 0;
            while (!a2.Empty())
            {
                if (l.Count >= max_size) break;
                CPoint p, p2;

                a2.GetPoints(out p, out p2);
                num++;

                d2 = -1;
                for (int md = 0; md < svm.Length; md++)
                {
                    double[] hf = //bit.HoG(0, 0, bit.PixelWidth, bit.PixelHeight, md);
                        img.HoG(p.X, p.Y, p2.X - p.X, p2.Y - p.Y, md);
                    C_SVC svc = (C_SVC)svm[md];
                    double key = 0;
                    if ((key = svm[md].Predict(ML.GetNodes(hf))) != d2 && d2 != -1)
                    {
                        //bln = false;
                        d2 = 0;
                        break;
                    }
                    d2 = key;
                    /* double prob = 0;
                     int key = 0;
                     Dictionary<int, double> dict = svc.PredictProbabilities(ML.GetNodes(hf));
                     foreach (int ky in dict.Keys)
                     {
                         if (prob < dict[ky] && dict[ky] > 0.55)
                         {
                             prob = dict[ky];
                             key = ky;
                         }
                     }
                     if (d2 != 0 && key != d2)
                     {
                         //  bln = false;
                         d2 = 0;
                         break;
                     }
                     d2 = key;*/
                }

                if (d2 > 0)//bln)
                {
                    l.Enqueue(new RecognizedRectangle(p, p2, d2));
                    for (int i = 0; i < svm.Length; i++)
                        for (int j = 0; j < opt[svm[i]].Length; j++)
                            opt[svm[i]][j].Add(p.Color, p2.Color, p2.X - p.X, p2.Y - p.Y);
                }

            }

            /* double d = Functions.CurrentTimeMillis();
             // Кэшируем изображение
             img.HoG(0, 0, 0, 0);
             Func<AdaptivePriorityOptimizer, bool> Find = (a2) =>
             {
                 while (!a2.Empty())
                 {
                     if (l.Count >= max_size) return false;

                     CPoint p, p2;
                     a2.GetPoints(out p, out p2);

                     System.Windows.Int32Rect int2 = new System.Windows.Int32Rect(p.X, p.Y, p2.X - p.X, p2.Y - p.Y);

                     //  CroppedBitmap b = new CroppedBitmap(img, int2);
                     //  BitmapImage ig = Functions.FromBitmapSource(b);

                     double[] hf = img.HoG(p.X, p.Y, p2.X - p.X, p2.Y - p.Y);

                     if (l.Count < max_size && svm.Predict(ML.GetNodes(hf)) > 0)
                     {
                         l.Enqueue(new RecognizedRectangle(p.X, p.Y, p2.X - p.X, p2.Y - p.Y, d));
                         a2.AddCorrect(p.Color, p2.Color, p2.X - p.X, p2.Y - p.Y);
                     }
                 }
                 return false;
             };
             ParallelEnumerable.Range(0, optimizer.Length).AsOrdered().WithDegreeOfParallelism(System.Environment.ProcessorCount).Any((a) => { return Find(optimizer[a]); });
             */
            n = num;

            // System.Windows.MessageBox.Show("Cnt: " + num + " Time: " + (ImageFunctions.CurrentTimeMillis() - s) + " Images: " + sum_images + " Predict: " + sum_predict + " Points: " + sum_points);
            return l;
        }
    }

    /// <summary>
    /// Распознанная прямоугольная область
    /// </summary>
    public class RecognizedRectangle
    {
        public double prob;
        public int X, Y, Width, Height;

        public CPoint left, right;

        public RecognizedRectangle(int x, int y, int w, int h)
            : this(x, y, w, h, 1)
        {
        }

        public RecognizedRectangle(int x, int y, int w, int h, double prob)
        {
            this.X = x;
            this.Y = y;
            this.Width = w;
            this.Height = h;
            this.prob = prob;
        }

        public RecognizedRectangle(CPoint l, CPoint r, double prob)
        {
            left = l;
            right = r;

            this.X = l.X;
            this.Y = l.Y;
            this.Width = r.X - X;
            this.Height = r.Y - Y;
            this.prob = prob;
        }
    }
}
