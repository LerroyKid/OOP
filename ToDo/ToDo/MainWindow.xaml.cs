using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ToDo.Models;
using ToDo.Services;

namespace ToDo
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly string PATH = $"{Environment.CurrentDirectory}\\data.json"; // путь к данным
        private BindingList<ToDoom> _DataList;
        private FileService _fileService;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) // загружаем данные при открытии окна
        {
            _fileService = new FileService(PATH);

            try
            {
                _DataList = _fileService.LoadData(); // загружаем задачи из JSON
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Close();
            }
               

            dgTodoList.ItemsSource = _DataList; // привязываем
            _DataList.ListChanged += _DataList_ListChanged; 
        }

        private void _DataList_ListChanged(object sender, ListChangedEventArgs e) // сохр при любом изменении списка
        {
            if (e.ListChangedType == ListChangedType.ItemAdded || e.ListChangedType == ListChangedType.ItemDeleted || e.ListChangedType == ListChangedType.ItemChanged)
            {
                try
                {
                    _fileService.SaveData(sender); // сохр в файл
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    Close();
                }
            }
        }
    }
}
