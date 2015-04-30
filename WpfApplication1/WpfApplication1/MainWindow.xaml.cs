using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;


namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        /// <summary>
        /// Хранит объекты TabItemObject - модель данных каждой вкладки
        /// </summary>
        private ArrayList tabItems;

        /// <summary>
        /// Диалоговые окна сохранения и загрузки
        /// </summary>
        private SaveFileDialog save;
        private OpenFileDialog open;

        public static MainWindow window;

        public MainWindow()
        {
            window = this;
            this.InitializeComponent();

            save = new SaveFileDialog();
            save.AddExtension = true;
            save.DefaultExt = "xml";
            save.Filter = "XML документ|*.xml|Файл libsvm|*";

            open = new OpenFileDialog();
            open.Filter = "XML документ|*.xml";

            tabItems = new ArrayList();
            AddTabItem();
        }

        /// <summary>
        /// Если есть несохранённые изменения в какой-либо вкладке, то true
        /// </summary>
        /// <returns></returns>
        private bool isUnsaved()
        {
            foreach (TabItemObject obj in tabItems)
            {
                if (!obj.is_saved) return true;
            }
            return false;
        }

        /// <summary>
        /// Обработка нажатия кнопки "Создать"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            AddTabItem();
        }

        /// <summary>
        /// Обработка нажатия кнопки "Открыть"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            bool? s = open.ShowDialog();

            if (s == true)
            {
                string name = open.SafeFileName.LastIndexOf('.') < 0 ? open.SafeFileName : open.SafeFileName.Substring(0, open.SafeFileName.LastIndexOf('.'));

                AddTabItem(false);
                control01.SelectedIndex = control01.Items.Count - 1;
                TabItemObject obj = ((TabItemObject)tabItems[control01.SelectedIndex]);
                obj.cntrl.dset.ReadXml(open.FileName);
                obj.save_name = name;
                obj.save_path = open.FileName;
                ((UserControl2)((TabItem)control01.SelectedItem).Header).label.Content = name;
            }
        }

        /// <summary>
        /// Обработка нажатия кнопки "Сохранить"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Click_2(object sender, RoutedEventArgs e)
        {
            TabItemObject obj = ((TabItemObject)tabItems[control01.SelectedIndex]);
            string save_path = obj.save_path;
            if (save_path == null) MenuItem_Click_3(sender, e);
            else
            {
                // Если уже сохраняли, то не просим ввести имя заново
                obj.cntrl.dset.WriteXml(save_path);
                obj.is_saved = true;
                ((UserControl2)((TabItem)control01.SelectedItem).Header).label.Content = obj.save_name;
            }
        }

        /// <summary>
        /// Обработка нажатия кнопки "Сохранить как"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Click_3(object sender, RoutedEventArgs e)
        {
            bool? s = save.ShowDialog();

            if (s == true)
            {
                TabItemObject obj = ((TabItemObject)tabItems[control01.SelectedIndex]);
                obj.cntrl.dset.WriteXml(save.FileName);

                string name = save.SafeFileName.LastIndexOf('.') < 0 ? save.SafeFileName : save.SafeFileName.Substring(0, save.SafeFileName.LastIndexOf('.'));
                ((UserControl2)((TabItem)control01.SelectedItem).Header).label.Content = name;
                obj.is_saved = true;
                obj.save_path = save.FileName;
                obj.save_name = name;
            }
        }

        /// <summary>
        /// Добавить звёздочку к имени если изменили что-то
        /// </summary>
        /// <param name="cnt"></param>
        public void Change(UserControl1 cnt)
        {
            if (!((UserControl2)((TabItem)control01.SelectedItem).Header).label.Content.ToString().EndsWith("*"))
            {
                ((UserControl2)((TabItem)control01.SelectedItem).Header).label.Content += "*";
                ((TabItemObject)tabItems[control01.SelectedIndex]).is_saved = false;
            }
        }

        /// <summary>
        /// Добавление вкладки
        /// </summary>
        private void AddTabItem(bool add = true)
        {
            TabItem tab = new TabItem();
            string name = GetNextName();
            TabItemObject obj = new TabItemObject(name);
            UserControl1 cn = new UserControl1(add);
            UserControl2 u2 = new UserControl2();
            u2.clear_button.Click += delegate(object sender, RoutedEventArgs e)
            {
                // Клик по крестику на вкладке
                if (MessageBox.Show("Really close the tab?", "Closing", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    tabItems.Remove(obj);
                    control01.Items.Remove(tab);
                }
            };
            u2.label.Content = name;
            tab.Header = u2;
            obj.cntrl = cn;
            //cn.add_button.Click += obj.click_add;
            tab.Content = cn;
            control01.Items.Add(tab);
            tabItems.Add(obj);
            if (tabItems.Count == 1)
            {
                tab.Focus();
            }
        }

        /// <summary>
        /// Генерирует имя для новой вкладки
        /// </summary>
        /// <returns></returns>
        private string GetNextName()
        {
            int last_number = 1;

            bool w = true;
            while (w)
            {
                w = false;
                foreach (TabItemObject it in tabItems)
                {
                    if (it.save_name.Equals("Set" + last_number))
                    {
                        last_number++;
                        w = true;
                        break;
                    }
                }
            }

            return "Set" + last_number;
        }

        /// <summary>
        /// Класс описывает модель данных создаваемой выборки данных
        /// </summary>
        private class TabItemObject
        {

            public bool is_saved; // были ли изменения после последнего сохранения
            public string save_path, save_name; // путь сохранения и название вкладки
            public UserControl1 cntrl;

            public TabItemObject(string name)
            {
                save_name = name;
                is_saved = true;
            }
        }

        private void Button_Click_1(object sender, System.ComponentModel.CancelEventArgs e)
        {
            bool uns = isUnsaved();
            if (!uns || MessageBox.Show("Really close the window? There are unsaved changes.", "Closing", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                return;
            }
            e.Cancel = true;
        }

        /// <summary>
        /// Находится ли курсор в основной части окна
        /// </summary>
        private bool isInMainArea;
        /// <summary>
        /// Находится ли курсор в окне
        /// </summary>
        private bool isInArea;

        private void Rectangle_MouseEnter(object sender, MouseEventArgs e)
        {
            isInMainArea = true;
        }

        private void Rectangle_MouseLeave(object sender, MouseEventArgs e)
        {
            isInMainArea = false;
        }

        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            isInArea = true;
        }

        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            isInArea = false;
        }

        /// <summary>
        /// Обновить состояние окна (если наведены на заголовок, сделать его непрозрачным и т.д.)
        /// </summary>
        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            bool isa = isInArea && !isInMainArea;
            WindowStyle = isa ? WindowStyle.SingleBorderWindow : WindowStyle.None;
        }
    }
}