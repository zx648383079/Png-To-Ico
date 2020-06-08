using PngToIco.Drawing;
using System;
using System.Collections.Generic;
using System.Drawing;
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

namespace PngToIco
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {

        private string fileName;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ChooseBtn_Click(object sender, RoutedEventArgs e)
        {
            var open = new Microsoft.Win32.OpenFileDialog
            {
                Multiselect = true,
                Filter = "PNG文件|*.png",
                Title = "选择PNG文件"
            };
            if (open.ShowDialog() != true)
            {
                return;
            }
            SrcTb.Text = open.SafeFileName;
            fileName = open.FileName;
            SaveBtn.IsEnabled = true;
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                MessageBox.Show("请选择PNG文件");
                return;
            }
            var open = new Microsoft.Win32.SaveFileDialog
            {
                Title = "选择保存路径",
                Filter = "ICO文件|*.ico",
                FileName = SrcTb.Text.Replace(".png", ".ico")
            };
            if (open.ShowDialog() != true)
            {
                return;
            }
            var saveFile = open.FileName;

            var bmp = new Bitmap(fileName);
            IconFactory.Save(bmp, saveFile, true);
        }
    }
}
