using Microsoft.Win32;
using System.Windows;
using System.Threading;
using System.Windows.Media.Imaging;
using libsvm;
using System.Windows.Media;
using System;
using System.Windows.Controls;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using Emgu.CV;

namespace WpfApplication1
{
    /// <summary>
    /// Логика взаимодействия для Window2.xaml
    /// </summary>
    public partial class Window2 : Window
    {
        private Capture capture;

        public SVM[] svm;
        private OpenFileDialog open, import;
        private SaveFileDialog export;
        private Thread thread, thread2, worker;
        private LukasCanade flow;

        private double old;
        private bool start, recognize;

        public static Window2 wind;

        public Window2()
        {
            InitializeComponent();
            wind = this;

            thread = new Thread(Process);
            // thread.IsBackground = true;
            //  thread2 = new Thread(Timer_);
            //    thread2.Start();

            open = new OpenFileDialog();
            open.Multiselect = false;
            open.Filter = "Images and Videos|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.avi;*.mp4";

            import = new OpenFileDialog();
            import.Multiselect = false;
            import.Filter = "XML|*.xml|ZIP files|*.zip";

            export = new SaveFileDialog();
            export.AddExtension = true;
            export.DefaultExt = "xml";
            export.Filter = "XML|*.xml|BIN|*.bin";

            list = new ConcurrentQueue<RecognizedRectangle>();
            flow = new LukasCanade();
        }

        private ImageSourceConverter conv;

        /// <summary>
        /// Сделать снимок и добавить в канвас его
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="args"></param>
        private void Captured(object obj, EventArgs args)
        {
        }

        /// <summary>
        /// Запуск
        /// </summary>
        public void Start()
        {
            start = true;
            if (thread.ThreadState == ThreadState.Unstarted)
                thread.Start();
            rec.Header = "Stop recognition";
        }

        /// <summary>
        /// Стоп
        /// </summary>
        public void Stop()
        {
            if (start)
            {
                start = false;
                rec.Header = "Recognize";
                canvas.Children.Clear();
            }
        }

        private BitmapImage img;
        private ConcurrentQueue<RecognizedRectangle> list;

        /// <summary>
        /// Процесс распознавания (происходит параллельно)
        /// </summary>
        private void Process()
        {
            // Выполняем циклически
            while (true)
            {
                if (!start) continue;
                int media_w = 0, media_h = 0;
                m.Dispatcher.Invoke(
                    System.Windows.Threading.DispatcherPriority.SystemIdle,
                    TimeSpan.FromMilliseconds(50),
                   new Action(() =>
                   {
                       RenderTargetBitmap b = new RenderTargetBitmap(media_w = (int)m.Width, media_h = (int)m.Height, 96, 96, PixelFormats.Pbgra32);
                       b.Render(m);
                       b.Freeze();

                       img = Functions.FromBitmapSource(b);

                       //s img = img.GrayScale();// ImageFunctions.Gauss(img.GrayScale(), img, 1);
                       img.Freeze();
                   }));

                // old = ImageFunctions.CurrentTimeMillis();
                // recognize = true;
                // worker = new Thread(new ThreadStart(() => { 
                double t = Functions.CurrentTimeMillis();
                int n = -1;

                bool is_ = list.IsEmpty;
                // Слежение методом Лукаса-Канаде
                if (!is_)
                {
                    flow.UpdateOpticalFlow(img);
                }
                foreach (RecognizedRectangle rect in list)
                {
                    CPoint l = rect.left;
                    CPoint r = rect.right;

                    l.X += (int)l.Vx;
                    l.Y += (int)l.Vy;
                    r.X += (int)r.Vx;
                    r.Y += (int)r.Vy;

                    if (l.X < 0 || l.Y < 0 || l.X >= media_w || l.Y >= media_h)
                    {
                        is_ = true;
                        break;
                    }

                    rect.X = l.X;
                    rect.Y = l.Y;
                    rect.Width = r.X - l.X;
                    rect.Height = r.Y - l.Y;
                }

                // распознавание
                if (is_)
                {
                    list = svm.RecognizeAll(img, out n);

                    // Обновляем список точек для слежения
                    flow.Clear();
                    flow.Start(img);
                    foreach (RecognizedRectangle rect in list)
                    {
                        flow.AddPoint(rect.left);
                        flow.AddPoint(rect.right);
                    }
                }

                //}));
                // worker.Start();
                t = Functions.CurrentTimeMillis() - t;

                m.Dispatcher.Invoke(
                    System.Windows.Threading.DispatcherPriority.SystemIdle,
                    TimeSpan.FromMilliseconds(50),
                   new Action(() =>
                   {
                       canvas.Children.Clear();
                       foreach (RecognizedRectangle rect in list)
                       {
                           System.Windows.Shapes.Rectangle r = new System.Windows.Shapes.Rectangle();
                           r.Stroke = new SolidColorBrush(rect.prob == 3 ? Colors.Green : rect.prob == 2 ? Colors.Blue : Colors.Red);
                           r.Width = rect.Width * m.Width / Functions.ImageWidth;
                           r.Height = rect.Height * m.Height / Functions.ImageHeight;
                           Canvas.SetLeft(r, rect.X * m.Width / Functions.ImageWidth);
                           Canvas.SetTop(r, rect.Y * m.Height / Functions.ImageHeight);
                           canvas.Children.Add(r);
                       }
                       Title = "N: " + n + " T: " + t;
                   }));

                Thread.Sleep(TimeSpan.FromMilliseconds(t >= 70 ? 1 : 70 - t));
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
                Stop();
                if (capture != null)
                {
                    capture.Stop();
                }
                m.Source = new System.Uri(open.FileName);
                m.IsMuted = true;
            }
        }

        /// <summary>
        /// Брать изображение с веб камеры
        /// </summary>
        /// <param name="a"></param>
        /// <param name="r"></param>
        public void From_webcam(object a, RoutedEventArgs r)
        {
            if (!capt)
            {
                if (m.UnloadedBehavior == MediaState.Play)
                    m.Stop();
                if (capture == null)
                {
                    try
                    {
                        capture = new Capture();
                        capture.FlipHorizontal = true;
                        capture.ImageGrabbed += Captured;
                    }
                    catch (Exception efd)
                    {
                        MessageBox.Show("Ошибка инициализации веб-камеры.");
                    }
                }
                if (capture != null)
                {
                    capture.Start();
                    capt = true;
                }
            }
        }

        private bool capt;

        /// <summary>
        /// Экспорт
        /// </summary>
        /// <param name="a"></param>
        /// <param name="r"></param>
        public void exp(object a, RoutedEventArgs r)
        {
            if (export.ShowDialog() == true)
            {
                if (svm.Length == 0)
                {
                    if (export.FileName.EndsWith(".bin"))
                    {
                        svm[0].ExportBinary(export.FileName, Functions.NO_COMPRESSION);
                    }
                    else
                    {
                        svm[0].Export(export.FileName);
                    }
                }
                else
                {
                    int nm = 0;
                    do
                    {
                        nm = Functions.random.Next();
                    } while (Directory.Exists("" + nm) || File.Exists("" + nm));

                    Directory.CreateDirectory("" + nm);

                    string nms = export.FileName.Replace(@"\", "/");

                    int ind = nms.LastIndexOf('/');
                    string dir2 = nms.Substring(0, ind);
                    string file = nms.Substring(ind + 1);
                    string dir = nm + "";

                    for (int i = 0; i < svm.Length; i++)
                    {
                        if (file.EndsWith(".bin"))
                        {
                            svm[i].ExportBinary(dir + "/" + i + file, Functions.NO_COMPRESSION);
                        }
                        else
                        {
                            svm[i].Export(dir + "/" + i + file);
                        }
                    }

                    ZipFile.CreateFromDirectory(dir, nms + ".zip");

                    Directory.Delete(dir, true);
                }
                MessageBox.Show("SVM экспортирована");
                return;
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
                try
                {
                    if (import.FileName.EndsWith(".zip"))
                    {
                        ZipArchive s = new ZipArchive(new FileStream(import.FileName, FileMode.Open), ZipArchiveMode.Read);

                        ReadOnlyCollection<ZipArchiveEntry> col = s.Entries;

                        List<SVM> l = new List<SVM>(col.Count);

                        int nm = 0;
                        do
                        {
                            nm = Functions.random.Next();
                        } while (Directory.Exists("" + nm) || File.Exists("" + nm));

                        foreach (ZipArchiveEntry entry in col)
                        {
                            entry.ExtractToFile(nm + ".xml");
                            if (entry.FullName.EndsWith(".xml"))
                            {
                                l.Add(ML.getSVMObject(nm + ".xml"));
                            }
                            File.Delete(nm + ".xml");
                        }
                        svm = l.ToArray();
                    }
                    else if (import.FileName.EndsWith(".xml"))
                    {
                        svm = new SVM[] { ML.getSVMObject(import.FileName) };
                    }
                    else throw new Exception("The incorrect format of file!");
                }
                catch (Exception x)
                {
                    MessageBox.Show("Некорректный формат файла.");
                    return;
                }
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
            if (svm.Length == 0 || svm.Length > 0 && svm[0] == null)
            {
                MessageBox.Show("Сначала загрузите SVM!");
                return;
            }
            if (!start)
                Start();
            else Stop();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            thread.Abort();
        }
    }
}
