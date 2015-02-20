using System.Collections.Generic;
using libsvm;
using System.Reflection;
using System.Windows.Media.Imaging;
using System.Drawing;

namespace WpfApplication1
{
    /// <summary>
    /// Класс для работы с machine learning
    /// </summary>
    public static class ML
    {
        /// <summary>
        /// Возвращает модель обученного SVM
        /// </summary>
        /// <returns></returns>
        public static SVM getSVMObject(List<List<double>> set)
        {
            var v = ProblemHelper.ReadProblem(set);
            var svm = new C_SVC(v, KernelHelper.RadialBasisFunctionKernel(0.5), 1, 100, true);//RadialBasisFunctionKernel(0.5)
            //svm_model model = (svm_model)(typeof(SVM).GetField("model", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(svm));
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
            return new C_SVC(file);
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
            List<ImageFunctions.CPoint> h = img.Harris();
            // h.Add(new ImageFunctions.CPoint(0, 0, 1));
            // h.Add(new ImageFunctions.CPoint(img.PixelWidth - 1, img.PixelHeight - 1, 1));
            int num = 0;
            foreach (ImageFunctions.CPoint p in h)
            {
                if (l.Count >= max_size) break;
                foreach (ImageFunctions.CPoint p2 in h)
                {
                    if (l.Count >= max_size) break;
                    if (p == p2) continue;
                    if (p2.X > p.X + 10 && p2.Y > p.Y + 10)
                    {
                        // System.Windows.MessageBox.Show("1. " + p.X + " " + p.Y + " " + p2.X + " " + p2.Y);
                        // if (!l.Exists(a => a.X + a.Width >= p.X && a.Y + a.Height >= p.Y && a.X <= p2.X && a.Y <= p2.Y))
                        // {
                        // System.Windows.MessageBox.Show("2. NULL");
                        CroppedBitmap b = new CroppedBitmap(img, new System.Windows.Int32Rect(p.X, p.Y, p2.X - p.X, p2.Y - p.Y));
                        BitmapImage ig = ImageFunctions.FromBitmapSource(b);

                        //ImageFunctions.SaveJpeg(ig, "a/" + num++ + ".jpg");
                        num++;

                        if (svm.Predict(ML.GetNodes(ig.HoG())) > 0.7)
                        {
                            System.Windows.MessageBox.Show("3. PREDICT " + (num - 1));
                            l.Add(new Rectangle(p.X, p.Y, p2.X - p.X, p2.Y - p.Y));
                            ImageFunctions.SaveJpeg(ig, "a/" + num + ".jpg");
                        }
                        // }
                    }
                }
            }
            System.Windows.MessageBox.Show("Cnt: " + num);
            return l;
        }
    }
}
