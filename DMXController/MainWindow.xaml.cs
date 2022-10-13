using System;
using System.IO;
using System.IO.Ports;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Threading;
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
using System.Windows;
using ColorPicker;

namespace DMXController
{

    ///datatypes and stuff yayyyy
    ///So basically we need to have a dictionary Ig that'll contain all the values
    ///Name of fixture - dictionary
    ///Has a dict that has following elements
    ///key-value pairs - key value to be changed (red,green,blue,white,amber,uv,dimmer,strobe,footprint) and value - channel/property name

    public partial class MainWindow : Window
    {
        private Dictionary<string, Dictionary<string, int>> fixtures = new Dictionary<string, Dictionary<string, int>>();
        private Dictionary<int, string> activeFixtures = new Dictionary<int, string>(); //first channel which it runs off of, second is name of ahrdware
        private List<int> changingFixtures = new List<int>();

        private Dictionary<int, int> dataToAdd = new Dictionary<int, int>();
        public DispatcherTimer dispatcherTimerr = new DispatcherTimer();
        private static SerialPort port = new SerialPort();
        private Boolean acceptval = true;
        private int startChannel = 1;



        private StackPanel allChanPan = new StackPanel();

        public MainWindow()
        {
            InitializeComponent();
            this.WindowState = WindowState.Maximized;
            if (File.Exists("fixtures.json"))
            {
                try
                {
                    fixtures = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, int>>>(File.ReadAllText("fixtures.json"));
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            }
            else
            {
                Dictionary<string, int> values = new Dictionary<string, int>();
                values["red"] = 1;
                values["green"] = 2;
                values["blue"] = 3;
                values["white"] = 4;
                values["amber"] = 5;
                values["uv"] = 6;
                values["dimmer"] = 7;
                values["footprint"] = 7;
                fixtures["12PX Hex Pearl"] = values;
                string towrite = JsonSerializer.Serialize(fixtures);
                File.WriteAllText("fixtures.json", towrite);
            }

            //currently hardcoded, make way to change
            activeFixtures[1] = "12PX Hex Pearl";
            activeFixtures[8] = "12PX Hex Pearl";
            activeFixtures[15] = "12PX Hex Pearl";
            activeFixtures[22] = "12PX Hex Pearl";
            activeFixtures[29] = "StairVille 240/8";
            activeFixtures[34] = "StairVille 240/8";
            activeFixtures[39] = "StairVille 240/8";
            activeFixtures[44] = "StairVille 240/8";
            activeFixtures[49] = "StairVille 240/8";
            activeFixtures[54] = "StairVille 240/8";
            activeFixtures[60] = "StairVille 240/8";

            foreach(int address in activeFixtures.Keys)            
            {
                StackPanel fixturepan = (StackPanel)this.FindName("FixturePan");

                StackPanel holdpan = new StackPanel();
                holdpan.Orientation = Orientation.Horizontal;

                CheckBox box = new CheckBox();
                box.Tag = address;
                box.Checked += new RoutedEventHandler(checcer);
                box.Unchecked += new RoutedEventHandler(checcer);
                box.Width = 40;
                box.Height = 40;

                Label label = new Label();
                label.Foreground = Brushes.White;
                label.Content = address.ToString() +": " +activeFixtures[address];

                holdpan.Children.Add(label);
                holdpan.Children.Add(box);
                fixturepan.Children.Add(holdpan);


            }

            port.PortName = "COM6"; // currently hardcoded, make way to change this
            port.BaudRate = 115200;
        
            dispatcherTimerr.Tick += new EventHandler(finalSender);
            dispatcherTimerr.Interval = TimeSpan.FromMilliseconds(50);
            dispatcherTimerr.Start();
            dispatcherTimerr.Tick += new EventHandler(datapart1Sender);
            dispatcherTimerr.Interval = TimeSpan.FromMilliseconds(10);
            dispatcherTimerr.Start();
            port.Open();
            allChanPan = (StackPanel)this.FindName("AllChanPan");

            for (int i = 0; i < 255; i++)
            {
                Canvas temppan = new Canvas();
                temppan.Width = 25;
                temppan.Height = 320;

                Label numbah = new Label();
                numbah.Content = (i+1).ToString();
                numbah.Foreground = Brushes.White;
                numbah.HorizontalAlignment = HorizontalAlignment.Center;

                Slider slider = new Slider();
                slider.Orientation = Orientation.Vertical;
                slider.Margin = new Thickness(0, 20, 0, 0);
                slider.Height = 250;
                slider.Minimum = 0;
                slider.SmallChange = 15;
                slider.Maximum = 255;
                slider.Name = "S"+(i + 1).ToString();
                slider.ValueChanged += new RoutedPropertyChangedEventHandler<double>(channelSliderChanged);

                temppan.Children.Add(numbah);
                temppan.Children.Add(slider);
                allChanPan.Children.Add(temppan);

            }
        }

        private void changeValue(string name, int value)
        {
            foreach (int id in changingFixtures)
            {
                var fixture = fixtures[activeFixtures[id]];
                if (fixture.Keys.Contains(name))
                {
                    dataToAdd[id - 1 + fixtures[activeFixtures[id]][name]] = value; //hesus, do you love me?
                }
            }
        }

        private void checcer(object sender, RoutedEventArgs e)
        {
            CheckBox box = (CheckBox)sender;
            if (box.IsChecked == true)
            {
                changingFixtures.Add((int)box.Tag);
            }
            else
            {
                changingFixtures.Remove((int)box.Tag);
            }
        }

        private void channelSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider slider = (Slider)sender;
            if (slider.Name.StartsWith ("S"))
            {
                //let's hope this works
                dataToAdd[int.Parse(slider.Name.Substring(1))] = (int)((Slider)sender).Value;
            }
        }

        private void finalSender(object? sender,EventArgs e)
        {
            port.Close();
            acceptval = true;
        }
        private void datapart1Sender(object? sender, EventArgs e)
        {
            //if broken then look into having the arduino reply back
            Thread thread = new Thread(datapart2Sender);
            thread.Start();
            
            /* Just a quick data ingegrity test idk
            for (int i = 0; i < 255; i++)
            {
                Task.Run(() => dataSender((i+1).ToString(), "2"));
            }
            */
        }
        //ehh that didn't fix it - TODO - some day maek this run without utilizing like half the cpu-
        private void datapart2Sender()
        {
            foreach (KeyValuePair<int, int> a in dataToAdd)
            {
                Task.Run(() => dataSender(a.Key.ToString(), a.Value.ToString()));
            }
        }
        private async Task dataSender(string key,string value)
        {
            if (acceptval == true)
            {
                if (!port.IsOpen)
                {
                    port.Open();
                }
                try
                {                
                    port.WriteLine("[" + key + ":" + value + "]");
                    Thread.Sleep(10);
                }
                catch (Exception ex) {}
                acceptval = false;
            }
        }
        


        //these are for testing purposes right now


        private void windowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            port.Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            dataToAdd.Clear();
        }

        private void main_ColorChanged(object sender, RoutedEventArgs e)
        {
            StandardColorPicker picker = (StandardColorPicker)sender;
            changeValue("red", (int)picker.Color.RGB_R);
            changeValue("green", (int)picker.Color.RGB_G);
            changeValue("blue", (int)picker.Color.RGB_B);
        }

        private void dimmer_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider slider = (Slider)sender;
            changeValue("dimmer", (int)slider.Value);
        }

        private void white_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider slider = (Slider)sender;
            changeValue("white", (int)slider.Value);
        }

        private void amber_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider slider = (Slider)sender;
            changeValue("amber", (int)slider.Value);
        }

        private void uv_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider slider = (Slider)sender;
            changeValue("uv", (int)slider.Value);
        }

        private void strobe_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider slider = (Slider)sender;
            changeValue("strobe", (int)slider.Value);
        }
    }
}

