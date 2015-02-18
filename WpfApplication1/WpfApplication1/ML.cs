using System.Collections.Generic;
using libsvm;
using System.Reflection;
using System.Windows.Media.Imaging;

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
            var svm = new C_SVC(v, KernelHelper.RadialBasisFunctionKernel(0.5), 1);
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
            var svm = new C_SVC(v, KernelHelper.RadialBasisFunctionKernel(0.5), 1);
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
    }
}
