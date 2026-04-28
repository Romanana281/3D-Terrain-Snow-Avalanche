using System;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Project4
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            MainTab.SelectionChanged += MainTab_SelectionChanged;
        }

        private void MainTab_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainTab.SelectedItem is TabItem tabItem)
            {
                Viewer viewer;
                switch (tabItem.Header)
                {
                    case "Седло":
                        viewer = Saddle; break;
                    case "Параболоид":
                        viewer = Paraboloid; break;
                    case "Волны":
                        viewer = Waves; break;
                    case "Гауссов холм":
                        viewer = GaussianHill; break;
                    default:
                        viewer = null; break;
                }

                if (viewer != null)
                {
                    if (viewer.isSnow)
                    {
                        BSnow.Background =
                            new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.
                            FromArgb(0xFF, 0x0A, 0x5F, 0x38));
                        BSnow.Content = "Выкл. снег";
                    }
                    else
                    {
                        BSnow.Background =
                            new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.
                            FromArgb(0xFF, 0xBD, 0x16, 0x2C));
                        BSnow.Content = "Вкл. снег";
                    }
                }
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            GenerateSaddle();
            GenerateParaboloid();
            GenerateWaves();
            GenerateGaussianHill();
        }

        private void GenerateSaddle()
        {
            int size = 20;
            double[,] data = new double[size, size];
            double minX = -1, maxX = 1;
            double minY = -1, maxY = 1;

            for (int i = 0; i < size; i++)
            {
                double x = minX + (maxX - minX) * i / (size - 1);
                for (int j = 0; j < size; j++)
                {
                    double y = minY + (maxY - minY) * j / (size - 1);
                    data[i, j] = x * x - y * y;
                }
            }

            Saddle.Create(data, minX, maxX, minY, maxY);
        }

        private void GenerateParaboloid()
        {
            int size = 20;
            double[,] data = new double[size, size];
            double minX = -1.5, maxX = 1.5;
            double minY = -1.5, maxY = 1.5;

            for (int i = 0; i < size; i++)
            {
                double x = minX + (maxX - minX) * i / (size - 1);
                for (int j = 0; j < size; j++)
                {
                    double y = minY + (maxY - minY) * j / (size - 1);
                    data[i, j] = x * x + y * y;
                }
            }

            Paraboloid.Create(data, minX, maxX, minY, maxY);
        }

        private void GenerateWaves()
        {
            int size = 20;
            double[,] data = new double[size, size];
            double minX = -3, maxX = 3;
            double minY = -3, maxY = 3;

            for (int i = 0; i < size; i++)
            {
                double x = minX + (maxX - minX) * i / (size - 1);
                for (int j = 0; j < size; j++)
                {
                    double y = minY + (maxY - minY) * j / (size - 1);

                    data[i, j] = Math.Sin(x) * Math.Cos(y);
                }
            }

            Waves.Create(data, minX, maxX, minY, maxY);

        }

        private void GenerateGaussianHill()
        {
            int size = 20;
            double[,] data = new double[size, size];
            double minX = -3, maxX = 3;
            double minY = -3, maxY = 3;

            for (int i = 0; i < size; i++)
            {
                double x = minX + (maxX - minX) * i / (size - 1);
                for (int j = 0; j < size; j++)
                {
                    double y = minY + (maxY - minY) * j / (size - 1);

                    data[i, j] = 4 * Math.Exp(-(x * x + y * y) / 2);
                }
            }

            GaussianHill.Create(data, minX, maxX, minY, maxY);
        }

        private void TurnSnow_Click(object sender, RoutedEventArgs e)
        {
            if (MainTab.SelectedItem is TabItem tabItem)
            {
                Viewer viewer;
                switch (tabItem.Header)
                {
                    case "Седло":
                        viewer = Saddle; break;
                    case "Параболоид":
                        viewer = Paraboloid; break;
                    case "Волны":
                        viewer = Waves; break;
                    case "Гауссов холм":
                        viewer = GaussianHill; break;
                    default:
                        viewer = null; break;
                }

                if (viewer.isSnow)
                {
                    BSnow.Background =
                           new System.Windows.Media.SolidColorBrush(
                           System.Windows.Media.Color.
                           FromArgb(0xFF, 0xBD, 0x16, 0x2C));
                    BSnow.Content = "Вкл. снег";
                    viewer.isSnow = false;
                    viewer.SnowStop();
                }
                else
                {
                    BSnow.Background =
                            new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.
                            FromArgb(0xFF, 0x0A, 0x5F, 0x38));
                    BSnow.Content = "Выкл. снег";
                    viewer.isSnow = true;
                    viewer.snowTimer.Start();
                }
            }
        }
    }
}