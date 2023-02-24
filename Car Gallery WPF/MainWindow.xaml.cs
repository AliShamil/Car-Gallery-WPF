using Car_Gallery_WPF.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Text.Json;
using System.Threading;
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

namespace Car_Gallery_WPF;


public partial class MainWindow : Window
{
    public ObservableCollection<Car>? Cars { get; set; }
    private CancellationTokenSource? _cts = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        Cars = new();
    }

    private void btnStart_Click(object sender, RoutedEventArgs e)
    {     
        BtnStart.IsEnabled = false;
        BtnCancel.IsEnabled = true;

        _cts = new();

        if (SingleOrMulti.IsChecked == false)
            AddSingleThread(_cts.Token);
        else
            AddMultiThread(_cts.Token);

        SingleOrMulti.IsEnabled = false;
    }


    private void AddSingleThread(CancellationToken token)
    {

        Cars?.Clear();
        new Thread(() =>
        {
            var watch = Stopwatch.StartNew();
            var dirInfo = new DirectoryInfo(@"..\..\..\FakeData");

            foreach (var file in dirInfo.GetFiles())
            {

                if (file.Extension == ".json")
                {
                    var txtJson = File.ReadAllText(file.FullName);

                    var cars = JsonSerializer.Deserialize<List<Car>>(txtJson);

                    if (cars is not null)

                        foreach (var car in cars)
                        {
                            if (token.IsCancellationRequested)
                            {
                                watch.Stop();
                                Dispatcher.Invoke(() => exeTime.Text = watch.Elapsed.ToString());
                                break;
                            }

                            Dispatcher.Invoke(() => Cars?.Add(car));
                            Dispatcher.Invoke(() => exeTime.Text = watch.Elapsed.ToString());
                            Thread.Sleep(100);
                        }
                }
            }
            watch.Stop();
            Dispatcher.Invoke(() => exeTime.Text = watch.Elapsed.ToString());
            Dispatcher.Invoke(() => BtnStart.IsEnabled = true);
            Dispatcher.Invoke(() => BtnCancel.IsEnabled = false);
            Dispatcher.Invoke(() => SingleOrMulti.IsEnabled = true);
        }).Start();
    }

    private void AddMultiThread(CancellationToken token)
    {
        Cars?.Clear();

        var dirInfo = new DirectoryInfo(@"..\..\..\FakeData");

        var sync = new object();
        var watch = Stopwatch.StartNew();
        uint workingThread = 0;
        foreach (var file in dirInfo.GetFiles())
        {
            if (file.Extension == ".json")
            {
                ThreadPool.QueueUserWorkItem(state =>
                {
                    ++workingThread;
                    var txtJson = File.ReadAllText(file.FullName);

                    var cars = JsonSerializer.Deserialize<List<Car>>(txtJson);

                    if (cars is not null)
                    {
                        foreach (var car in cars)
                        {

                            if (token.IsCancellationRequested)
                            {
                                watch.Stop();
                                Dispatcher.Invoke(() => exeTime.Text = watch.Elapsed.ToString());
                                break;
                            }

                            lock (sync)
                                Dispatcher.Invoke(() => Cars?.Add(car));

                            Dispatcher.Invoke(() => exeTime.Text = watch.Elapsed.ToString());
                            Thread.Sleep(100);
                        }
                    }

                    --workingThread;
                    if(workingThread == 0)
                    {
                        Dispatcher.Invoke(() => BtnStart.IsEnabled = true);
                        Dispatcher.Invoke(() => BtnCancel.IsEnabled = false);
                        Dispatcher.Invoke(() => SingleOrMulti.IsEnabled = true);
                    }

                });
                
            }
        }
        
        
    }

    private void btnCancel_Click(object sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
        BtnCancel.IsEnabled = false;
        BtnStart.IsEnabled = true;
        exeTime.Text = "00:00:00";
        SingleOrMulti.IsEnabled = true;
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if(e.Key == Key.Enter && BtnStart.IsEnabled) 
            btnStart_Click(sender, e);  
        else if(e.Key == Key.Back && BtnCancel.IsEnabled)
            btnCancel_Click(sender, e);
    }
}
