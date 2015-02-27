using System.Collections.Generic;
using libsvm;
using System.Reflection;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace WpfApplication1
{
    /// <summary>
    /// Класс для работы с machine learning
    /// </summary>
    public static class ML
    {
        /// <summary>
        /// Оптимизаторы
        /// </summary>
        private static Dictionary<SVM, AdaptivePriorityOptimizer> opt;

        static ML()
        {
            opt = new Dictionary<SVM, AdaptivePriorityOptimizer>();
        }

        /// <summary>
        /// Возвращает модель обученного SVM
        /// </summary>
        /// <returns></returns>
        public static SVM getSVMObject(List<List<double>> set)
        {
            var v = ProblemHelper.ReadProblem(set);
            var svm = new C_SVC(v, KernelHelper.RadialBasisFunctionKernel(0.5), 1);//RadialBasisFunctionKernel(0.5)
            //svm_model model = (svm_model)(typeof(SVM).GetField("model", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(svm));
            opt.Add(svm, new AdaptivePriorityOptimizer());
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
            var svm = new C_SVC(v, KernelHelper.RadialBasisFunctionKernel(0.5), 1);//RadialBasisFunctionKernel(0.5)
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
            opt.Add(svm, new AdaptivePriorityOptimizer());
            return svm;
        }

        /// <summary>
        /// Распознать изображение BitmapImage
        /// </summary>
        /// <param name="svm"></param>
        /// <param name="img"></param>
        /// <returns></returns>
        public static double Recognize(this SVM svm, BitmapImage img)
        {
            return svm.Predict(ML.GetNodes(img.HoG()));
        }

        /// <summary>
        /// Распознать изображение BitmapImage
        /// </summary>
        /// <param name="svm"></param>
        /// <param name="img"></param>
        /// <returns></returns>
        public static List<Rectangle> RecognizeAllSlide(this SVM svm, BitmapImage img)
        {
            List<Rectangle> l = new List<Rectangle>();
            int xstep = 1, ystep = 1;
            int w = img.PixelWidth / 8;
            int h = img.PixelHeight / 8;
            int count = 0;
            while (w <= img.PixelWidth)
            {
                int x = 0, y = 0;
                while (x <= img.PixelWidth - w)
                {
                    y = 0;
                    while (y <= img.PixelHeight - h)
                    {
                        if (x > img.Width - w) break;
                        CroppedBitmap b = new CroppedBitmap(img, new System.Windows.Int32Rect(x, y, w, h));
                        BitmapImage ig = ImageFunctions.FromBitmapSource(b);
                        count++;
                        if (svm.Predict(ML.GetNodes(ig.HoG())) > 0.7)
                        {
                            if (l.Find(a => a.X + a.Width >= x && a.Y + a.Height >= y && a.X <= x + w && a.Y <= y + h) == null)
                            {
                                l.Add(new Rectangle(x, y, w, h));
                                x += w;
                                y += h;
                                return l;
                            }
                        }
                        y += ystep;
                    }
                    x += xstep;
                }

                w *= 2;
                h *= 2;
            }
            System.Windows.MessageBox.Show("Count: " + count);
            return l;
        }

        //  private static bool is_end;

        /// <summary>
        /// Распознать изображение BitmapImage
        /// </summary>
        /// <param name="svm"></param>
        /// <param name="img"></param>
        /// <returns></returns>
        public static List<Rectangle> RecognizeAll(this SVM svm, BitmapImage img)
        {
            int max_size = 1;
            List<Rectangle> l = new List<Rectangle>();
            double e = ImageFunctions.CurrentTimeMillis();
            img = ImageFunctions.Gauss(img.GrayScale(), img, 1);
            System.Windows.MessageBox.Show("Tm: " + (ImageFunctions.CurrentTimeMillis() - e));

            List<ImageFunctions.CPoint> h = img.Harris();

            AdaptivePriorityOptimizer optimizer = opt[svm];

            int num = 0;
            foreach (ImageFunctions.CPoint p in h)
            {
                foreach (ImageFunctions.CPoint p2 in h)
                {
                    if (p == p2) continue;
                    if (p2.X > p.X && p2.Y > p.Y)
                    {
                        optimizer.AddPoint(p, p2);
                    }
                }
            }

            // is_end = false;

            double s = ImageFunctions.CurrentTimeMillis();
            double sum_images = 0, sum_points = 0, sum_predict = 0;

            //img.Freeze();

            while (!optimizer.Empty() && !is_end)
            {
                if (l.Count >= max_size) break;
                ImageFunctions.CPoint p, p2;

                double s1 = ImageFunctions.CurrentTimeMillis();
                optimizer.GetPoints(out p, out p2);
                sum_points += ImageFunctions.CurrentTimeMillis() - s1;

                System.Windows.Int32Rect int2 = new System.Windows.Int32Rect(p.X, p.Y, p2.X - p.X, p2.Y - p.Y);

                s1 = ImageFunctions.CurrentTimeMillis();
                CroppedBitmap b = new CroppedBitmap(img, int2);
                BitmapImage ig = ImageFunctions.FromBitmapSource(b);
                double[] hf = ig.HoG();
                sum_images += ImageFunctions.CurrentTimeMillis() - s1;

                num++;
                double d;
                s1 = ImageFunctions.CurrentTimeMillis();
                if ((d = svm.Predict(ML.GetNodes(hf))) > 0)
                {
                    sum_predict += ImageFunctions.CurrentTimeMillis() - s1;
                    l.Add(new Rectangle(p.X, p.Y, p2.X - p.X, p2.Y - p.Y));
                    optimizer.AddCorrect(p.Color, p2.Color, p2.X - p.X, p2.Y - p.Y);

                    // is_end = true;

                    ImageFunctions.SaveJpeg(ig, "a/" + num + ".jpg");
                }
            }

            System.Windows.MessageBox.Show("Cnt: " + num + " Time: " + (ImageFunctions.CurrentTimeMillis() - s) + " Images: " + sum_images + " Predict: " + sum_predict + " Points: " + sum_points);
            return l;
        }
    }
}
