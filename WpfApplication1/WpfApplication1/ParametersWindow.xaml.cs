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

        private int cnt, cnt_scales, cnt_shifts, cnt_blurs, cnt_rotates, margin_from, margin_to, margins;
        private float max_scale, from_angle, to_angle;
        private bool r, g, b;

        public int Count { set { if (value > 0) cnt = value; else throw new ArgumentOutOfRangeException(); } get { return cnt; } }
        public int CountScales { get { return cnt_scales; } }
        public int CountShifts { get { return cnt_shifts; } }
        public int CountBlurs { get { return cnt_blurs; } }
        public int CountRotates { get { return cnt_rotates; } }
        public float MaxScale { get { return max_scale; } }
        public float MinAngle { get { return from_angle; } }
        public float MaxAngle { get { return to_angle; } }
        public bool Red { get { return r; } }
        public bool Green { get { return g; } }
        public bool Blue { get { return b; } }
        public int MinMargin { get { return margin_from; } }
        public int MaxMargin { get { return margin_to; } }
        public int CountMargins { get { return margins; } }

        public Window3()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            r = (bool)check_r.IsChecked;
            g = (bool)check_g.IsChecked;
            b = (bool)check_b.IsChecked;
            string tx = max_01.Text.Trim().Replace('.', ',');
            if (!float.TryParse(tx, out max_scale))
            {
                if (!float.TryParse(tx.Replace('.', ','), out max_scale))
                {
                    MessageBox.Show("Введите вещественное число в поле Макс. увеличение.", "Ошибка.");
                    return;
                }
            }
            float min, max;
            tx = from_box.Text.Trim().Replace('.', ',');
            if (!float.TryParse(tx, out min))
            {
                if (!float.TryParse(tx.Replace('.', ','), out min))
                {
                    MessageBox.Show("Введите вещественное число в поле \"Угол: от\".", "Ошибка.");
                    return;
                }
            }
            tx = to_box.Text.Trim().Replace('.', ',');
            if (!float.TryParse(tx, out max))
            {
                if (!float.TryParse(tx.Replace('.', ','), out max))
                {
                    MessageBox.Show("Введите вещественное число в поле \"Угол: до\".", "Ошибка.");
                    return;
                }
            }
            tx = from_margin.Text.Trim().Replace('.', ',');
            if (!int.TryParse(tx, out margin_from))
            {
                if (!int.TryParse(tx.Replace('.', ','), out margin_from))
                {
                    MessageBox.Show("Введите вещественное число в поле \"Отступ: от\".", "Ошибка.");
                    return;
                }
            }
            tx = to_margin.Text.Trim().Replace('.', ',');
            if (!int.TryParse(tx, out margin_to))
            {
                if (!int.TryParse(tx.Replace('.', ','), out margin_to))
                {
                    MessageBox.Show("Введите вещественное число в поле \"Отступ: до\".", "Ошибка.");
                    return;
                }
            }
            while (min < 0) min += 360;
            while (max < 0) max += 360;
            while (min > max) max += 360;
            from_angle = min;
            to_angle = max;

            DialogResult = true;
        }

        private void Update()
        {
            if (slider3 == null || slider2 == null || slider1 == null || slider4 == null || l1 == null || l3 == null || l4 == null || l5 == null || from_margin == null || to_margin == null || marg_cnt == null || l8 == null) return;
            label01.Content = "Всего изображений: " + Count * ((cnt_scales = (int)slider1.Value) * (cnt_rotates = (int)slider2.Value) * (cnt_shifts = (int)slider3.Value) * cnt_shifts * (cnt_blurs = (int)slider4.Value) * (margins = (int)marg_cnt.Value));
            l1.Content = "" + cnt_scales;
            l3.Content = "" + cnt_rotates;
            l4.Content = "" + cnt_shifts;
            l5.Content = "" + cnt_blurs;
            l8.Content = "" + margins;
            r = (bool)check_r.IsChecked;
            g = (bool)check_g.IsChecked;
            b = (bool)check_b.IsChecked;
            gs.Opacity = !r && !g && !b ? 1 : 0;
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
