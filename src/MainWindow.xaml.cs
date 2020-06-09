using PngToIco.Drawing;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
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

        private string[] fileNames;
        private string name;

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
                Title = "选择PNG文件",
                CheckFileExists = true,
            };
            if (open.ShowDialog() != true)
            {
                return;
            }
            name = open.SafeFileName;
            SrcTb.Text = string.Join(",", open.SafeFileNames);
            fileNames = open.FileNames;
            SaveBtn.IsEnabled = true;
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (fileNames.Length < 1)
            {
                MessageBox.Show("请选择PNG文件");
                return;
            }
            var open = new Microsoft.Win32.SaveFileDialog
            {
                Title = "选择保存路径",
                Filter = "ICO文件|*.ico",
                FileName = name.Replace(".png", ".ico")
            };
            if (open.ShowDialog() != true)
            {
                return;
            }
            var saveFile = open.FileName;
            var sizes = GetSizes();
            var images = CreateImages();
            using (var fs = new FileStream(saveFile, FileMode.Create))
            {
                if (sizes.Count > 0)
                {
                    Ico.Converter(images, sizes.ToArray(), GetQuality(), fs);
                } else
                {
                    Ico.Converter(images, fs);
                }
            }
            foreach (var item in images)
            {
                item.Dispose();
            }
            MessageBox.Show("转换完成");
        }

        private List<Bitmap> CreateImages()
        {
            var data = new List<Bitmap>();
            foreach (var item in fileNames)
            {
                data.Add(new Bitmap(item));
            }
            return data;
        }

        private SmoothingMode GetQuality()
        {
            switch (QualityCb.SelectedIndex)
            {
                case 1:
                    return SmoothingMode.Default;
                case 2:
                    return SmoothingMode.HighSpeed;
                default:
                    return SmoothingMode.HighQuality;
            }
        }

        private List<int> GetSizes()
        {
            var items = new List<int>();
            foreach (CheckBox item in SizeBox.Children)
            {
                if (item.IsChecked == true)
                {
                    items.Add(Convert.ToInt32(item.Content));
                }
            }
            items.Sort();
            return items;
        }
    }
}
