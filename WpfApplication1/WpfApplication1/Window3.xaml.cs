using System;
using System.Windows;
using System.Windows.Input;

namespace WpfApplication1
{
    /// <summary>
    /// Логика взаимодействия для Window3.xaml
    /// </summary>
    public partial class Window3 : Window
    {

        private int cnt, cnt_scales, cnt_shifts, cnt_blurs, cnt_rotates;

        public int Count { set { if (value > 0) cnt = value; else throw new ArgumentOutOfRangeException(); } get { return cnt; } }
        public int CountScales { get { return cnt_scales; } }
        public int CountShifts { get { return cnt_shifts; } }
        public int CountBlurs { get { return cnt_blurs; } }
        public int CountRotates { get { return cnt_rotates; } }

        public Window3()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Update()
        {
            if (slider3 == null || slider2 == null || slider1 == null || slider4 == null) return;
            label01.Content = "Всего изображений: " + Count * ((cnt_scales = (int)slider1.Value) * (cnt_rotates = (int)slider2.Value) * (cnt_shifts = (int)slider3.Value) * cnt_shifts * (cnt_blurs = (int)slider4.Value));
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Update();
        }

        private void Slider_ValueChanged(object sender, StylusEventArgs e)
        {
            Update();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Update();
        }
    }
}
