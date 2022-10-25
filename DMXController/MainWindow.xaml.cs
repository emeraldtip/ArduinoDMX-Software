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

    ///Animations aka effects
    ///dict: key - animation name; value - dict: key - millisecond; value - dict: key - channel; value - value
    public partial class MainWindow : Window
    {
        private Dictionary<string, Dictionary<string, int>> fixtures = new Dictionary<string, Dictionary<string, int>>();
        private Dictionary<int, string> activeFixtures = new Dictionary<int, string>(); //first channel which it runs off of, second is name of ahrdware
        private List<int> changingFixtures = new List<int>();

        //it's a dictionary inside of a dictionary inside of a dictionary inside of a dictionary
        //I don't see a single problem here
        private Dictionary<string,Dictionary<int,Dictionary<int,int>>> animations = new Dictionary<string, Dictionary<int, Dictionary<int, int>>>(); 

        private Dictionary<int, int> dataToAdd = new Dictionary<int, int>();
        private List<int> channelOrder = new List<int>(); //to add newest change in the channels first and make sure it get's through

        public DispatcherTimer dispatcherTimerr = new DispatcherTimer();
        private static SerialPort port = new SerialPort();
        private Boolean acceptval = true;
        private int startChannel = 1; //this will be used later
        private bool animRunning = false;



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

            if (File.Exists("animations.json"))
            {
                try
                {
                    //first frame - millisecond 0 has the length of the animation (aka last millisecond) as the value of channel 0, then the next millisecond is the thing is the frame where the animation actually starts
                    animations = JsonSerializer.Deserialize<Dictionary<string, Dictionary<int, Dictionary<int, int>>>>(File.ReadAllText("animations.json"));
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            }
            else
            {
                Dictionary<int,int> test = new Dictionary<int,int>();
                test[0] = 100;
                test[2] = 100;

                Dictionary<int, Dictionary<int, int>> tes2 = new Dictionary<int, Dictionary<int, int>>();
                tes2[0] = test;

                animations["Police"] = tes2;

                string towrite = JsonSerializer.Serialize(animations);
                File.WriteAllText("animations.json", towrite);

                //{"Police":{1:{3:2000},1000:{1:255,3:0,8:255,10:0,15:0,17:255,22:0,24:255},2000:{1:0,3:255,8:0,10:255,15:255,17:0,22:255,24:0}}}

            }

            //currently hardcoded, make way  to change
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

            foreach(String animation in animations.Keys)
            {
                StackPanel animPanel = (StackPanel)this.FindName("AnimPan");
                StackPanel panel = new StackPanel();
                panel.Orientation = Orientation.Horizontal;

                Label label = new Label();
                label.Content = animation;
                label.Foreground = Brushes.White;

                Button enable = new Button();
                enable.Margin = new Thickness(20,0,0,0);
                enable.Tag = animation;
                enable.Content = "Start animation";
                enable.Click += animer;

                Button disable = new Button();
                disable.Tag = animation;
                disable.Content = "Stop animation";
                disable.Margin = new Thickness(20, 0, 0, 0);
                disable.IsEnabled = false;
                disable.Click += animer;

                panel.Children.Add(label);
                panel.Children.Add(enable);
                panel.Children.Add(disable);
                animPanel.Children.Add(panel);
            }
            port.PortName = "COM12"; // currently hardcoded, make way to change this
            port.BaudRate = 115200;
        
            dispatcherTimerr.Tick += new EventHandler(finalSender);
            dispatcherTimerr.Interval = TimeSpan.FromMilliseconds(50);
            dispatcherTimerr.Start();
            dispatcherTimerr.Tick += new EventHandler(datapart1Sender);
            dispatcherTimerr.Interval = TimeSpan.FromMilliseconds(10);
            dispatcherTimerr.Start();
            try
            {
                port.Open();
            }
            catch (Exception ex) { }
            
            allChanPan = (StackPanel)this.FindName("AllChanPan");

            for (int i = 0; i < 512; i++)
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
                slider.Margin = new Thickness(0, 50, 0, 0);
                slider.Height = 250;
                slider.Minimum = 0;
                slider.SmallChange = 15;
                slider.SmallChange = 1;
                slider.Maximum = 255;
                slider.Name = "S"+(i + 1).ToString();
                slider.ValueChanged += new RoutedPropertyChangedEventHandler<double>(channelSliderChanged);

                temppan.Children.Add(numbah);
                temppan.Children.Add(slider);
                allChanPan.Children.Add(temppan);

            }
        }
        private void animer(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            button.IsEnabled = false;
            if (button.Content == "Start animation")
            {
                ((Button)((StackPanel)button.Parent).Children[2]).IsEnabled = true;

            }
            else
            {
                ((Button)((StackPanel)button.Parent).Children[1]).IsEnabled = true;
            }
        }

        private void animstart(string animation)
        {
            int length = animations[animation][0][0];
            for (int i = 0; i < length; i++)
            {
                var frame = animations[animation][i+1];
                foreach (var chanel in frame.Keys)
                {
                    dataToAdd[chanel] = frame[chanel];
                    if (channelOrder.Contains(chanel))
                    {
                        channelOrder.Remove(chanel);
                    }
                    channelOrder.Insert(0, chanel);
                }
            }
        }

        private void changeValue(string name, int value)
        {
            foreach (int id in changingFixtures)
            {
                var fixture = fixtures[activeFixtures[id]];
                if (fixture.Keys.Contains(name))
                {
                    int fixtureID = id - 1 + fixtures[activeFixtures[id]][name];
                    dataToAdd[fixtureID] = value; //hesus, do you love me?
                    if (channelOrder.Contains(fixtureID))
                    {
                        channelOrder.Remove(fixtureID);
                    }
                    channelOrder.Insert(0, fixtureID);
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
                if (channelOrder.Contains(int.Parse(slider.Name.Substring(1))))
                {
                    channelOrder.Remove(int.Parse(slider.Name.Substring(1)));
                }
                channelOrder.Insert(0,int.Parse(slider.Name.Substring(1)));
            }
        }

        private void finalSender(object? sender,EventArgs e)
        {
            port.Close();
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
            try
            {
                foreach (int a in channelOrder)
                {
                    if (dataSender(a.ToString(), dataToAdd[a].ToString()) == true)
                    {
                        continue;
                    }
                }
            }
            catch { }
        }
        private Boolean dataSender(string key,string value)
        {
            if (acceptval == true)
            {
                if (!port.IsOpen)
                {
                    try
                    {
                        port.Open();
                    } catch { }
                    
                }
                try
                {                
                    port.WriteLine("[" + key + ":" + value + "]");
                    acceptval = true;
                }
                catch (Exception ex) {}
                
                return false;
            }
            return false;
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

        private void Blackout(object sender, RoutedEventArgs e)
        {
            dataToAdd.Clear();
            for (int i = 0; i < 512; i++)
            { 
                dataSender(i.ToString(), "0");
                Thread.Sleep(1);
            }
        }
    }
}

