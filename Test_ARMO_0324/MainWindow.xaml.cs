using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Test_ARMO_0324
{
    public partial class MainWindow : Window
    {
        public int all_files;
        public int correct_files;
        public string start_dir;
        public string now_dir;
        public DateTime time_now;
        public Thread thread_search;
        public string file_name;
        public bool stop_search;
        public bool stop_thread;
        object locker;
        DispatcherTimer timer;
        TreeView tree;

        public MainWindow()
        {
            time_now = new();
            all_files = 0;
            correct_files = 0;
            stop_search = false;
            stop_thread = false;
            locker = new();
            InitializeComponent();
            try
            {
                StreamReader sr = new StreamReader("data.txt");
                start_dir_txt.Text = sr.ReadLine();
                file_name_txt.Text = sr.ReadLine();
            }
            catch { }
        }

        public void Add_File_First(out TreeViewItem item2)
        {
            var item = new TreeViewItem();
            item.Header = start_dir;
            tree.Items.Add(item);
            item2 = item;
        }
        public void Add_File(TreeViewItem perent, string text_file)
        {
            var item = new TreeViewItem();
            item.Header = text_file;
            perent.Items.Add(item);
        }
        public void Add_Dir(TreeViewItem perent, string text_dir, out TreeViewItem item2)
        {
            var item = new TreeViewItem();
            item.Header = text_dir;
            perent.Items.Add(item);
            item2 = item;
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            Search_Check();
        }

        void Search_Check()
        {
            if (start_dir_txt.IsEnabled)
            {
                search_btn.Content = "Сброс";
                start_dir = start_dir_txt.Text;
                file_name = file_name_txt.Text;
                if (start_dir.Length != 0 && file_name.Length != 0)
                {
                    start_dir_txt.IsEnabled = false;
                    file_name_txt.IsEnabled = false;
                    path_txt.Visibility = Visibility.Visible;
                    data_txt.Visibility = Visibility.Visible;
                    stop_or_start.Visibility = Visibility.Visible;
                    info_txt.Visibility = Visibility.Hidden;
                    tree = new TreeView();
                    Grid.SetRow(tree, 0);
                    Grid.SetColumn(tree, 0);
                    Grid.SetColumnSpan(tree, 3);
                    big_Grid.Children.Add(tree);
                    timer = new();
                    timer.Interval = TimeSpan.FromSeconds(0.1);
                    timer.Tick += Timer_Tick;
                    timer.Start();

                    thread_search = new Thread(Search_Files);
                    thread_search.Start();
                }
                else
                {
                    MessageBox.Show("Некорректные данные!");
                }
            }
            else
            {
                lock (locker)
                {
                   // stop_search = true;
                    stop_thread = true;
                }
                thread_search.Interrupt();
                search_btn.Content = "Поиск";
                start_dir_txt.IsEnabled = true;
                file_name_txt.IsEnabled = true;
                timer.Stop();
                time_now = new();
                all_files = 0;
                correct_files = 0;
                stop_search = false;
                stop_or_start.Content = "Стоп";
                locker = new();
                path_txt.Visibility = Visibility.Hidden;
                data_txt.Visibility = Visibility.Hidden;
                stop_or_start.Visibility = Visibility.Hidden;
                big_Grid.Children.Remove(tree);
                info_txt.Visibility = Visibility.Visible;

            }
        }

        void Search_Files()
        {
            TreeViewItem item = null;
            Dispatcher.Invoke(new Action(() => Add_File_First(out item)), DispatcherPriority.Normal, null);
            List<string> lines = new() { start_dir + '\n' + file_name };
            File.WriteAllLines("data.txt", lines);
            Show_Files(start_dir, item);
        }

        void Show_Files(string path_now, TreeViewItem perents)
        {
            bool thread_stop;
            bool shutdown;
            lock (locker)
            {
                thread_stop = stop_search;
                shutdown = stop_thread;
            }
            while (thread_stop)
            {
                Thread.Sleep(200);
                lock (locker)
                {
                    thread_stop = stop_search;
                }
            }
            now_dir = path_now;
            try
            {
                if (!shutdown)
                {
                    all_files += Directory.GetFiles(path_now).Length;
                    var files = Directory.EnumerateFiles(path_now, file_name);
                    foreach (string filename in files)
                    {
                        //Thread.Sleep(1000);//dell
                        string[] buff = filename.Split('\\');
                        Dispatcher.Invoke(new Action(() => Add_File(perents, buff[buff.Length - 1])), DispatcherPriority.Normal, null);
                        correct_files++;
                    }
                    var dirs = Directory.EnumerateDirectories(now_dir, "*.*");
                    foreach (string dirname in dirs)
                    {
                        if (!shutdown)
                        {
                            TreeViewItem item = null;
                            string[] buff = dirname.Split('\\');
                            Dispatcher.Invoke(new Action(() => Add_Dir(perents, buff[buff.Length - 1], out item)), DispatcherPriority.Normal, null);
                            Show_Files(dirname, item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // MessageBox.Show(ex.Message);
            }
        }

        public void Timer_Tick(object sender, EventArgs e)
        {
            time_txt.Content = time_now.ToString("HH:mm:ss");
            all_files_txt.Content = all_files.ToString();
            correct_files_txt.Content = correct_files.ToString();
            path_txt.Content = now_dir;
            time_now = time_now.AddSeconds(0.1);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            bool local_stop_search;
            lock (locker)
            {
                local_stop_search = stop_search;
            }
            if (!local_stop_search)
            {
                timer.Stop();
                stop_or_start.Content = "Дальше";
                lock (locker)
                {
                    stop_search = true;
                }
            }
            else
            {
                timer.Start();
                stop_or_start.Content = "Стоп";
                lock (locker)
                {
                    stop_search = false;
                }
            }
        }
    }

}
