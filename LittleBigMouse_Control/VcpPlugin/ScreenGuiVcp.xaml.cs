﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel.Channels;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Argyll;
using LbmScreenConfig;
using WinAPI_Dxva2;

namespace LittleBigMouse_Control.VcpPlugin
{
    /// <summary>
    /// Logique d'interaction pour ScreenGuiSizer.xaml
    /// </summary>
    public partial class ScreenGuiVcp : UserControl
    {
        public ScreenGuiVcp()
        {
            Lut.Load();
        }

        public Screen Screen => (DataContext as ScreenViewModel)?.Screen;

        public MonitorVcp Vcp => Screen?.Monitor?.Vcp();
        public ProbeLut Lut => Screen?.ProbeLut();

        private void ButtonOff_OnClick(object sender, RoutedEventArgs e)
        {
            Dxva2.SetVCPFeature(Screen.Monitor.HPhysical, 0xD6, 4);
        }
        private void WakeUp_OnClick(object sender, RoutedEventArgs e)
        {
                //Dxva2.SetVCPFeature(ScreenGui.Screen.HPhysical, 0x60, 0x0f);
                Dxva2.SetVCPFeature(Screen.Monitor.HPhysical, 0xD6, 1);
                //Dxva2.SetVCPFeature(ScreenGui.Screen.HPhysical, 0xE1, 1);
                Dxva2.SetVCPFeature(Screen.Monitor.HPhysical, 0xE1, 0);              
        }
        //private void ButtonOn_OnClick(object sender, RoutedEventArgs e)
        //{
        //    uint code = Convert.ToUInt32(txtCode.Text, 16);
        //    uint value = Convert.ToUInt32(txtValue.Text, 16);

        //    uint pvct;
        //    uint current;
        //    uint max;

        //    Dxva2.GetVCPFeatureAndVCPFeatureReply(ScreenGui.Screen.HPhysical, code, out pvct, out current, out max);

        //    Debug.Print(pvct.ToString() + ":" + current.ToString() + "<" + max.ToString());

        //    Dxva2.SetVCPFeature(ScreenGui.Screen.HPhysical, code, value);
        //    //for (uint i = 0; i < max; i++)
        //    //{
        //    //    if (i==5 && code==0xD6) continue; 
        //    //    bool result = Dxva2.SetVCPFeature(Screen.HPhysical, code, i);
        //    //    Debug.Print(i.ToString() + (result?":O":"X"));
        //    //}

        //    //IntPtr desk = User32.GetDesktopWindow();
        //    //IntPtr win = User32.FindWindowEx(IntPtr.Zero, IntPtr.Zero, null, null);

        //    //User32.SendMessage(-1, User32.WM_SYSCOMMAND, User32.SC_MONITORPOWER, 2);
        //    //User32.SendMessage(-1, User32.WM_SYSCOMMAND, User32.SC_MONITORPOWER, -1);
        //}

        private void PleaseInstall()
        {
            MessageBox.Show("Please install DispcalGUI & ArgyllCMS", "Calibration tools",
                         MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }
        private void ProbeLowLuminance_OnClick(object sender, RoutedEventArgs e)
        {
            var probe = new ArgyllProbe();
            if (!probe.Installed)
            {
                PleaseInstall();
                return;
            }


            new Thread(() =>
            {
                MonitorLevel level = Vcp.Brightness;
                //LineSeries _line = new LineSeries();

                //Curve.PlotModel.Series.Add(_line);

                uint max = Vcp.Gain.Red.Max;
                uint min = Vcp.Gain.Red.Min;

                double old = Screen.ProbeLut().Luminance;
                level.Value = 0;

                for (uint i = max; i >= min; i--)
                {
                    

                    TuneWhitePoint(i);

                    Tune t = Lut.Current;

                    if (probe.SpotRead())
                    {
                        t.Y = probe.ProbedColor.xyY.Y;
                        t.x = probe.ProbedColor.xyY.x;
                        t.y = probe.ProbedColor.xyY.y;                      
                    }

                    Lut.RemoveLowBrightness(i);
                    Lut.Add(t);

                    //_line.Points.Add(new DataPoint(i, t.Y));
                    //Curve.Refresh();

                    if(t.MinGain<=min) break;
                }

                Screen.ProbeLut().Luminance = old;
            }
            ).Start();
            return;
        }
        private void ProbeLuminance_OnClick(object sender, RoutedEventArgs e)
        {
            var probe = new ArgyllProbe();
            if (!probe.Installed)
            {
                PleaseInstall();
                return;
            }


            new Thread(() =>
            {
                MonitorLevel level = Vcp.Brightness;
                //LineSeries _line = new LineSeries();

                //_line.Color = OxyColors.Red;

                //Curve.PlotModel.Series.Add(_line);

                for (uint i = level.Min; i <= level.Max; i++)
                {
                    level.Value = i;

                    TuneWhitePoint();

                    Tune t = Lut.Current;
                    if (probe.SpotRead())
                    {
                        t.Y = probe.ProbedColor.xyY.Y;
                        t.x = probe.ProbedColor.xyY.x;
                        t.y = probe.ProbedColor.xyY.y;                        
                    }

                    Lut.RemoveBrightness(t.Brightness);
                    Lut.Add(t);

                    //_line.Points.Add(new DataPoint(i, t.Y));
                    //Curve.Refresh();
                }
            }
            ).Start();
            return;

            new Thread(() => {
                for (uint channel = 0; channel < 3; channel++)
                {
                    MonitorLevel level = Vcp.Gain.Channel(channel);
                    //LineSeries line = new LineSeries();

                    switch (channel)
                    {
                        case 0:
                            //line.Color = OxyColors.Red;
                            break;
                        case 1:
                            //line.Color = OxyColors.Green;
                            break;
                        case 2:
                            //line.Color = OxyColors.Blue;
                            break;
                    }

                  //  Curve.PlotModel.Series.Add(line);

                    uint max = level.Value;

                    for (uint i = 32; i <= max; i++)
                    {
                        level.Value = i; Thread.Sleep(1000);
                        probe.SpotRead();
                        ProbedColor color = probe.ProbedColor;

                        //line.Points.Add(new DataPoint(i, color.DeltaE00()));
                        //Curve.Refresh();
                    }

                    double min = double.MaxValue;
                    uint minIdx = level.Value;
                    //foreach (DataPoint dp in line.Points)
                    //{
                    //    if (dp.Y < min)
                    //    {
                    //        min = dp.Y;
                    //        minIdx = (uint)dp.X;
                    //    }
                    //}

                    level.Value = minIdx;
                }
            }
            ).Start();
        }

        private void Probe(ArgyllProbe probe, Color c, MonitorLevel level)
        {
            //LineSeries line = new LineSeries {Color = c};


            //Curve.PlotModel.Series.Add(line);

            for (uint i = level.Min; i <= level.Max; i++)
            {
                level.CheckedValue = i;

                if (probe.SpotRead())
                {
                    //line.Points.Add(new DataPoint(i, probe.ProbedColor.Lab.L));
                    //Curve.Refresh();                    
                }
            }

        }

        private void Probe_OnClick(object sender, RoutedEventArgs e)
        {
            var probe = new ArgyllProbe();
            if (!probe.Installed)
            {
                PleaseInstall();
                return;
            }
            //Vcp.Brightness.SetToMax();
            //Vcp.Contrast.SetToMax();
            uint red = Vcp.Gain.Red.Value;
            uint green = Vcp.Gain.Green.Value;
            uint blue = Vcp.Gain.Blue.Value;
            //Vcp.Brightness.SetToMax();
            //Vcp.Contrast.SetToMax();
            //Vcp.Gain.Red.SetToMax();
            //Vcp.Gain.Green.SetToMax();
            //Vcp.Gain.Blue.SetToMax();
            new Thread(() =>
            {
                Vcp.Gain.Red.SetToMax();
                Vcp.Gain.Green.SetToMin();
                Vcp.Gain.Blue.SetToMin();
                Probe(probe, Colors.Red, Vcp.Contrast);

                Vcp.Gain.Red.SetToMin();
                Vcp.Gain.Green.SetToMax();
                Vcp.Gain.Blue.SetToMin();
                Probe(probe, Colors.Green, Vcp.Contrast);

                Vcp.Gain.Red.SetToMin();
                Vcp.Gain.Green.SetToMin();
                Vcp.Gain.Blue.SetToMax();
                Probe(probe, Colors.Blue, Vcp.Contrast);

                Vcp.Gain.Red.Value = red;
                Vcp.Gain.Green.Value = green;
                Vcp.Gain.Blue.Value = blue;
            }
            ).Start();
            return;

            new Thread (() => {
                                  for (uint channel = 0; channel < 3; channel++)
                                  {
                                    MonitorLevel level = Vcp.Gain.Channel(channel);
                                    //LineSeries line = new LineSeries();

                                      switch (channel)
                                      {
                        case 0:
                            //line.Color = OxyColors.Red;
                                              break;
                        case 1:
                                              //line.Color = OxyColors.Green;
                                              break;
                                          case 2:
                                              //line.Color = OxyColors.Blue;
                                              break;
                                      }

                                //    Curve.PlotModel.Series.Add(line);

                                      uint max = level.Value;

                                    for (uint i = 32; i <= max; i++)
                                    {
                                        level.Value = i; Thread.Sleep(1000);

                                        if (probe.SpotRead())
                                        {
                                            //line.Points.Add(new DataPoint(i,probe.ProbedColor.DeltaE00()));
                                            //Curve.Refresh();                                            
                                        }
                                    }

                                      double min = double.MaxValue;
                                      uint minIdx = level.Value;
                                      //foreach (DataPoint dp in line.Points)
                                      //{
                                      //    if (dp.Y < min)
                                      //    {
                                      //        min = dp.Y;
                                      //        minIdx = (uint)dp.X;
                                      //    }
                                      //}

                                      level.Value = minIdx;
                                  }
            }
            ).Start();
        }

        private void Tune_OnClick(object sender, RoutedEventArgs e)
        {
            new Thread(TuneWhitePoint).Start();

        }

        private double[,,] _tune;
        //private LineSeries _line;

        public void TuneWhitePoint()
        {
            TuneWhitePoint(0);
        }
        public void TuneWhitePoint(uint max)
        {
        //    _line = new LineSeries();
        //    Curve.PlotModel.Series.Add(_line);

            int count = 6;
            uint channel = 0;

            _tune = new double[Vcp.Gain.Red.Max+1, Vcp.Gain.Green.Max+1, Vcp.Gain.Blue.Max+1];

            uint[] rgb = { Vcp.Gain.Red.CheckedValue, Vcp.Gain.Green.CheckedValue, Vcp.Gain.Blue.CheckedValue };

            while (count > 0)
            {
                if (TuneWhitePoint(channel, ref rgb, max)) count = 5;
                else count--;

                channel++;
                channel %= 6;
            }

            Vcp.Gain.Red.CheckedValue = rgb[0];
            Vcp.Gain.Green.CheckedValue = rgb[1];
            Vcp.Gain.Blue.CheckedValue = rgb[2];
        }

        private bool TuneWhitePoint(uint channel, ref uint[] rgb, uint maxlevel)
        {
            uint[] c;
            uint[] min = new uint[3];
            uint[] max = new uint[3];
            uint n = 0;

            switch (channel)
            {
                case 0:
                    c = new uint[] {0, 1, 2};
                    n = 1;
                    break;
                case 1:
                    c = new uint[] {1, 2, 0};
                    n = 1;
                    break;
                case 2:
                    c = new uint[] {2, 0, 1};
                    n = 1;
                    break;
                case 3: //0
                    c = new uint[] {0, 1, 2};
                    n = 2;
                    break;
                case 4: //1
                    c = new uint[] {1, 2, 0};
                    n = 2;
                    break;
                case 5: //2
                    c = new uint[] {2, 0, 1};
                    n = 2;
                    break;
                default:
                    c = new uint[] {0, 1, 2};
                    n = 3;
                    break;
            }


            for (int i = 0; i < 3; i++)
            {
                min[i] = Vcp.Gain.Channel(c[i]).Min;
                max[i] = maxlevel==0?Vcp.Gain.Channel(c[i]).Max:maxlevel;
            }


            // keep one channel to its max
            switch (n)
            {
                case 1:
                    if (rgb[c[0]] == max[0] && rgb[c[1]] < max[1] && rgb[c[2]] < max[2]) return false;
                    break;
                case 2:
                    if ((rgb[c[0]] == max[0] || rgb[c[1]] == max[1]) && rgb[c[2]] < max[2]) return false;
                    break;
                case 3:
                    return false;
            }
            


            uint[] oldGain = { rgb[c[0]], rgb[c[1]], rgb[c[2]] };
            //uint old2 = rgb[c2];

            double deltaE = Probe(rgb);
            //_line.Points.Add(new DataPoint(_line.Points.Count, deltaE));
            //Curve.Refresh();

            while (rgb[c[0]] > min[0] && (n < 2 || rgb[c[1]] > min[1]) && (n < 3 || rgb[c[2]]> min[2]))
            {
                double old = deltaE;

                for (int i=0; i<n; i++) rgb[c[i]]--;

                deltaE = Probe(rgb);

                if (deltaE > old) break;

                //_line.Points.Add(new DataPoint(_line.Points.Count,deltaE));
                //Curve.Refresh();
            }

            while (rgb[c[0]] < max[0] && (n < 2 || rgb[c[1]] < max[1]) && (n < 3 || rgb[c[2]] < max[2]))
            {
                double old = deltaE;

                for (int i = 0; i < n; i++) rgb[c[i]]++;

                deltaE = Probe(rgb);

                if (deltaE > old)
                {
                    for (int i = 0; i < n; i++) rgb[c[i]]--;
                    deltaE = Probe(rgb);
                    break;
                }

                //_line.Points.Add(new DataPoint(_line.Points.Count, deltaE));
                //Curve.Refresh();
            }

            return (
                oldGain[0] != rgb[c[0]]
                || oldGain[1] != rgb[c[1]] 
                || oldGain[2] != rgb[c[2]]
                );
    }
        private double Probe(uint[] rgb)
        {
            if (_tune[rgb[0], rgb[1], rgb[2]] == 0)
            {
                Vcp.Gain.Red.CheckedValue = rgb[0];
                Vcp.Gain.Green.CheckedValue = rgb[1];
                Vcp.Gain.Blue.CheckedValue = rgb[2];             

                Thread.Sleep(500);

                ArgyllProbe probe = new ArgyllProbe(true);
                if (!probe.Installed)
                {
                    PleaseInstall();
                    return 0;
                }
                if (probe.SpotRead())
                {
                    _tune[rgb[0], rgb[1], rgb[2]] = probe.ProbedColor.DeltaE00();                   
                }
 
                //return color.DeltaE00();
            }
            return _tune[rgb[0], rgb[1], rgb[2]];
        }
        private void UIElement_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void Save_OnClick(object sender, RoutedEventArgs e)
        {
            Lut.Save();
        }
    }
}
