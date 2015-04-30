using Microsoft.Win32;
using System.Windows;

namespace WpfApplication1
{
    /// <summary>
    /// Логика взаимодействия для Window4.xaml
    /// </summary>
    public partial class Window4 : Window
    {
        public int Count;
        public byte TypeFilter;
        public int HammingParameter;
        public byte SearchSource;

        public const byte GOOGLE_SEARCH = 0, BING_SEARCH = 1;

        public string[] Images;

        private OpenFileDialog ims;

        public Window4()
        {
            InitializeComponent();

            ims = new OpenFileDialog();
            ims.Filter = "Images|*.gif;*.jpg;*.jpeg;*.png;*.bmp";
            ims.Multiselect = true;

            Images = new string[0];
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            int d;
            if (!int.TryParse(nmb.Text.Trim(), out d) || d <= 0)
            {
                MessageBox.Show("Ошибка. Введите целое положительное число в поле \"Количество изображений\"");
                return;
            }
            Count = d;

            TypeFilter = (byte)combo.SelectedIndex;
            SearchSource = (byte)srce.SelectedIndex;

            HammingParameter = 66 - (byte)perc.Value;

            DialogResult = true;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (ims.ShowDialog() == true)
            {
                Images = ims.FileNames;
                cnt_lab.Content = "Изображений для сравнения: " + Images.Length;
            }
        }
    }
}
