using System;
using System.IO;
using System.IO.Ports;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Text.Json;
using System.Collections.Generic;
using System.Data;

namespace DMXController
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Dictionary<int, int> dataToAdd = new Dictionary<int, int>();
        public DispatcherTimer dispatcherTimerr = new DispatcherTimer();
        private static SerialPort port = new SerialPort();
        private Boolean acceptval = true;
        
        public MainWindow()
        {
            InitializeComponent();
            this.WindowState = WindowState.Maximized;
            if (File.Exists("fixtures.json"))
            {
                //tasks = JsonSerializer.Deserialize<List<Fixture>>(File.ReadAllText("fixtures.json"));
               

            }
            port.PortName = "COM12";
            port.BaudRate = 115200;
        
            dispatcherTimerr.Tick += new EventHandler(finalSender);
            dispatcherTimerr.Interval = TimeSpan.FromMilliseconds(50);
            dispatcherTimerr.Start();
            port.Open();
        }
        private void finalSender(object? sender,EventArgs e)
        {
            acceptval = true;
        }
        private void dataSender()
        {
            try
            {
                foreach (int key in dataToAdd.Keys)
                {
                    port.WriteLine("[" + key.ToString() + ":" + dataToAdd[key].ToString() + "]");
                    acceptval = false;
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }


        //these are for testing purposes right now
        private void mainvalchang(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            dataToAdd[7] = (int)((Slider)sender).Value;
            dataSender();
        }

        private void colvalchang(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            dataToAdd[1] = (int)((Slider)sender).Value;
            dataSender();
        }

        private void windowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            port.Close();
        }
    }
}
