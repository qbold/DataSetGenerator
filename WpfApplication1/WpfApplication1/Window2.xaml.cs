using Microsoft.Win32;
using System.Windows;
using System.Threading;
using System.Windows.Media.Imaging;
using System.IO;
using libsvm;
using System.Windows.Media;
using System;

namespace WpfApplication1
{
    /// <summary>
    /// Логика взаимодействия для Window2.xaml
    /// </summary>
    public partial class Window2 : Window
    {

        public SVM svm;
        private OpenFileDialog open, import;
        private SaveFileDialog export;
        private Thread thread;

        public Window2()
        {
            InitializeComponent();

            thread = new Thread(Process);

            open = new OpenFileDialog();
            open.Multiselect = false;
            open.Filter = "Images and Videos|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.avi;*.mp4";

            import = new OpenFileDialog();
            import.Multiselect = false;
            import.Filter = "XML|*.xml";

            export = new SaveFileDialog();
            export.AddExtension = true;
            export.DefaultExt = "xml";
            export.Filter = "XML|*.xml";
        }

        /// <summary>
        /// Запуск
        /// </summary>
        public void Start()
        {
            thread.Start();
        }

        /// <summary>
        /// Процесс распознавания (происходит параллельно)
        /// </summary>
        private void Process()
        {
            // Выполняем циклически
            while (true)
            {
                // Получаем изображение из MediaElement
                RenderTargetBitmap b = new RenderTargetBitmap((int)m.Width, (int)m.Height, 96, 96, PixelFormats.Bgr32);
                b.Render(m);

                MemoryStream mem = new MemoryStream();
                JpegBitmapEncoder enc = new JpegBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(b));
                enc.Save(mem);

                BitmapImage img = new BitmapImage();
                img.BeginInit();
                img.StreamSource = mem;
                img.DecodePixelHeight = 144;
                img.DecodePixelWidth = 176;
                img.EndInit();

                double prob = svm.Predict(ML.GetNodes(img.HoG()));
                MessageBox.Show(prob + " ");

                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
        }

        /// <summary>
        /// Загрузка медиа элемента
        /// </summary>
        /// <param name="a"></param>
        /// <param name="r"></param>
        public void Load_dataset(object a, RoutedEventArgs r)
        {
            if (open.ShowDialog() == true)
            {
                m.Source = new System.Uri(open.FileName);
            }
        }

        /// <summary>
        /// Экспорт
        /// </summary>
        /// <param name="a"></param>
        /// <param name="r"></param>
        public void exp(object a, RoutedEventArgs r)
        {
            if (export.ShowDialog() == true)
            {
                svm.Export(export.FileName);
                MessageBox.Show("SVM экспортирована");
            }
        }

        /// <summary>
        /// Импорт
        /// </summary>
        /// <param name="a"></param>
        /// <param name="r"></param>
        public void imp(object a, RoutedEventArgs r)
        {
            if (import.ShowDialog() == true)
            {
                svm = ML.getSVMObject(import.FileName);
                MessageBox.Show("SVM импортирована");
            }
        }

        /// <summary>
        /// Распознать
        /// </summary>
        /// <param name="a"></param>
        /// <param name="r"></param>
        public void Recognize(object a, RoutedEventArgs r)
        {
            if (svm == null)
            {
                MessageBox.Show("Сначала загрузите SVM!");
                return;
            }

            RenderTargetBitmap b = new RenderTargetBitmap((int)m.Width, (int)m.Height, 96, 96, PixelFormats.Pbgra32);
            b.Render(m);
            BitmapImage img = ImageFunctions.FromBitmapSource(b);
            MessageBox.Show(svm.Recognize(img) + " ");
        }
    }
}
