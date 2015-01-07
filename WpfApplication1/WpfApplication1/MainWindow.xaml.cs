using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
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

        public MainWindow()
        {
            this.InitializeComponent();

            save = new SaveFileDialog();
            save.AddExtension = true;
            save.DefaultExt = "xml";
            save.Filter = "XML документ|*.xml";
            open = new OpenFileDialog();
            open.Filter = "XML документ|*.xml";

            tabItems = new ArrayList();
            AddTabItem();
        }

        /// <summary>
        /// Обработка нажатия кнопки закрытия приложения
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Перетаскивание окна
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Rectangle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        /// <summary>
        /// Обработка нажатия кнопки "Создать"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Создать
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
                ((TabItemObject)tabItems[control01.SelectedIndex]).cntrl.dset.ReadXml(open.FileName);
        }

        /// <summary>
        /// Обработка нажатия кнопки "Сохранить"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Click_2(object sender, RoutedEventArgs e)
        {
            bool? s = save.ShowDialog();

            if (s == true)
                ((TabItemObject)tabItems[control01.SelectedIndex]).cntrl.dset.WriteXml(save.FileName);
        }

        /// <summary>
        /// Добавление вкладки
        /// </summary>
        private void AddTabItem()
        {
            TabItem tab = new TabItem();
            string name = GetNextName();
            TabItemObject obj = new TabItemObject(name);
            UserControl1 cn = new UserControl1();
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
                    if (it.name.Equals("Set" + last_number))
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
            public string save_path; // путь сохранения
            public string name; // имя вкладки
            public UserControl1 cntrl;

            public TabItemObject(string name)
            {
                this.name = name;
                is_saved = true;
            }
        }


        /// <summary>
        /// Изменение стиля кнопки при наведении
        /// </summary>
        private Style old;

        private void Button_MouseEnter(object sender, MouseEventArgs e)
        {
            old = exit_but.Style;
            exit_but.Style = exit_but.FocusVisualStyle;
        }

        private void exit_but_MouseLeave(object sender, MouseEventArgs e)
        {
            exit_but.Style = old;
        }
    }
}