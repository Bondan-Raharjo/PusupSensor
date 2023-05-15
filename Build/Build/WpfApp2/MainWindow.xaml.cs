using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO.Ports;
using System.Threading;
using Microsoft.Kinect;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using Excel = Microsoft.Office.Interop.Excel;

namespace WpfApp2
{

    public partial class MainWindow : System.Windows.Window
    {
        SerialPort serial = new SerialPort();
        KinectSensor sensor;
        const int SKELETON_COUNT = 6;
        Skeleton[] allSkeletons = new Skeleton[SKELETON_COUNT];
      
        int Syarat1, Syarat2, Syarat3, Nilai_total=0;
        DispatcherTimer _timer;
        TimeSpan _time;
        bool Counting = false;

        string LokasiFile = @"C:\Users\HIKARI\Documents\Kinect_New\a.xls";

        private static DateTime StartTimeWholeDay;
        private DispatcherTimer _dailyTimer;
        public double AngleBetweenTwoVectors(Vector3D vectorA, Vector3D vectorB)
        {
            double dotProduct;
            vectorA.Normalize();
            vectorB.Normalize();
            dotProduct = Vector3D.DotProduct(vectorA, vectorB);
            return (double)Math.Acos(dotProduct) / Math.PI * 180;
        }

        public byte[] GetVector(Skeleton skeleton)
        {
            Vector3D ShoulderCenter = new Vector3D(skeleton.Joints[JointType.ShoulderCenter].Position.X, skeleton.Joints[JointType.ShoulderCenter].Position.Y, skeleton.Joints[JointType.ShoulderCenter].Position.Z);
            Vector3D RightShoulder = new Vector3D(skeleton.Joints[JointType.ShoulderRight].Position.X, skeleton.Joints[JointType.ShoulderRight].Position.Y, skeleton.Joints[JointType.ShoulderRight].Position.Z);
            Vector3D LeftShoulder = new Vector3D(skeleton.Joints[JointType.ShoulderLeft].Position.X, skeleton.Joints[JointType.ShoulderLeft].Position.Y, skeleton.Joints[JointType.ShoulderLeft].Position.Z);
            Vector3D RightElbow = new Vector3D(skeleton.Joints[JointType.ElbowRight].Position.X, skeleton.Joints[JointType.ElbowRight].Position.Y, skeleton.Joints[JointType.ElbowRight].Position.Z);
            Vector3D LeftElbow = new Vector3D(skeleton.Joints[JointType.ElbowLeft].Position.X, skeleton.Joints[JointType.ElbowLeft].Position.Y, skeleton.Joints[JointType.ElbowLeft].Position.Z);
            Vector3D RightWrist = new Vector3D(skeleton.Joints[JointType.WristRight].Position.X, skeleton.Joints[JointType.WristRight].Position.Y, skeleton.Joints[JointType.WristRight].Position.Z);
            Vector3D LeftWrist = new Vector3D(skeleton.Joints[JointType.WristLeft].Position.X, skeleton.Joints[JointType.WristLeft].Position.Y, skeleton.Joints[JointType.WristLeft].Position.Z);
            Vector3D UpVector = new Vector3D(0.0, 1.0, 0.0);

            double AngleRightElbow = AngleBetweenTwoVectors(RightElbow - RightShoulder, RightElbow - RightWrist);
            double AngleRightShoulder = AngleBetweenTwoVectors(UpVector, RightShoulder - RightElbow);
            double AngleLeftElbow = AngleBetweenTwoVectors(LeftElbow - LeftShoulder, LeftElbow - LeftWrist);
            double AngleLeftShoulder = AngleBetweenTwoVectors(UpVector, LeftShoulder - LeftElbow);


            byte[] Angles = { Convert.ToByte(AngleRightElbow), Convert.ToByte(AngleRightShoulder), Convert.ToByte(AngleLeftElbow), Convert.ToByte(AngleLeftShoulder) };
            return Angles;
        }
        public MainWindow()
        {
            InitializeComponent();
            Bt_Riset.IsEnabled = false;
            logo.Visibility = Visibility.Hidden;
            logo2.Visibility = Visibility.Hidden;
            logo3.Visibility = Visibility.Hidden;

            batasAtas.Visibility = Visibility.Hidden;
            batasBawah.Visibility = Visibility.Hidden;
            Input_Nama.Text = "Nama Peserta";
            Input_No.Text = "No Peserta";
            serial.Close();
            Bt_Start.IsEnabled = false;

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            {
                cbPort.Items.Add(port);
            }
           }
        public void StartWorkingTimeTodayTimer()
        {
            
            StartTimeWholeDay = DateTime.Now;
            DateTime x30SecsLater = StartTimeWholeDay.AddSeconds(59);

            _dailyTimer = new DispatcherTimer(DispatcherPriority.Render);
            _dailyTimer.Interval = TimeSpan.FromSeconds(1);
            _dailyTimer.Tick += (sender, args) =>
            {
                Waktu_value.Content = (DateTime.Now - x30SecsLater).ToString(@"ss"); // DateTime.Now.ToLongTimeString()
                if (Convert.ToInt16(Waktu_value.Content) == 0)
                {
                    _dailyTimer.Stop();
                }
            };
            _dailyTimer.Start();

        }
        private void Sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame == null)
                    return;

                byte[] pixels = new byte[colorFrame.PixelDataLength];
                colorFrame.CopyPixelDataTo(pixels);
                int stride = colorFrame.Width * 4;
                imaKinect.Source = BitmapSource.Create(colorFrame.Width, colorFrame.Height, 96, 96, PixelFormats.Bgr32, null, pixels, stride);
            
            }

            Skeleton satu = null;
            GetSkeleton(e, ref satu);

            if (satu == null)
            {
                logo.Visibility = Visibility.Hidden;
                logo2.Visibility = Visibility.Hidden;
                logo3.Visibility = Visibility.Hidden;
                batasAtas.Visibility = Visibility.Visible;
                batasBawah.Visibility =Visibility.Visible;
                Batas_Atas_Value.Content = Convert.ToInt16(Slide_batasAtas1.Value);
                Batas_Bawah_Value.Content = Convert.ToInt16(Slide_batasBawah.Value);

                return;
            }

            if (satu != null)
            {
                logo.Visibility = Visibility.Visible;
                logo2.Visibility = Visibility.Visible;
                logo3.Visibility = Visibility.Visible;
                batasAtas.Visibility = Visibility.Visible;
                batasBawah.Visibility = Visibility.Visible;
                GetCameraPoint(satu, e);
               // GetVector(satu); // get angel
                
            }

        }
        
        private void GetSkeleton(AllFramesReadyEventArgs e, ref Skeleton satu)
        {
            using (SkeletonFrame skeletonFrameData = e.OpenSkeletonFrame())
            {
                if (skeletonFrameData == null)
                    return;
                skeletonFrameData.CopySkeletonDataTo(allSkeletons);
                satu = (from s in allSkeletons where s.TrackingState == SkeletonTrackingState.Tracked select s).FirstOrDefault();
            }
        }

        private void GetCameraPoint(Skeleton satu, AllFramesReadyEventArgs e)
        {
            using (DepthImageFrame depth = e.OpenDepthImageFrame())
            {
                if (depth == null || sensor == null)
                {
                    return;
                }               

                DepthImagePoint headDepthPoint = depth.MapFromSkeletonPoint(satu.Joints[JointType.Head].Position);
                ColorImagePoint headColorPoint = depth.MapToColorImagePoint(headDepthPoint.X, headDepthPoint.Y, ColorImageFormat.RgbResolution640x480Fps30);
                Canvas.SetLeft(logo, headColorPoint.X - (logo.Width / 2));
                Canvas.SetTop(logo, headColorPoint.Y - (logo.Height / 2));

                DepthImagePoint shoulderLeftDepthPoint = depth.MapFromSkeletonPoint(satu.Joints[JointType.ShoulderLeft].Position);
                ColorImagePoint shoulderLeftPoint = depth.MapToColorImagePoint(shoulderLeftDepthPoint.X, shoulderLeftDepthPoint.Y, ColorImageFormat.RgbResolution640x480Fps30);
                Canvas.SetLeft(logo2, shoulderLeftPoint.X - (logo2.Width / 2));
                Canvas.SetTop(logo2, shoulderLeftPoint.Y - (logo2.Height / 2));

                DepthImagePoint shoulderRightDepthPoint = depth.MapFromSkeletonPoint(satu.Joints[JointType.ShoulderRight].Position);
                ColorImagePoint shoulderRightPoint = depth.MapToColorImagePoint(shoulderRightDepthPoint.X, shoulderRightDepthPoint.Y, ColorImageFormat.RgbResolution640x480Fps30);
                Canvas.SetLeft(logo3, shoulderRightPoint.X - (logo3.Width / 2));
                Canvas.SetTop(logo3, shoulderRightPoint.Y - (logo3.Height / 2));

                DepthImagePoint shoulderCenterDepthPoint = depth.MapFromSkeletonPoint(satu.Joints[JointType.ShoulderCenter].Position);
                ColorImagePoint shoulderCenterPoint = depth.MapToColorImagePoint(shoulderCenterDepthPoint.X, shoulderCenterDepthPoint.Y, ColorImageFormat.RgbResolution640x480Fps30);


                Posisi_kepala.Content = headColorPoint.Y; // Posisi Naik Turun
                Posisi_Bahu.Content = shoulderLeftPoint.Y;
                PosisiBahu2.Content = shoulderRightPoint.Y;

                if (Counting == true)
                {
                    if (Convert.ToInt16(Waktu_value.Content) > 0)
                    {
                        if (headColorPoint.Y <= Slide_batasAtas1.Value && Syarat2 == 1 && Syarat3 == 1)
                        {
                            Syarat1 = 1;
                        }
                        if ((shoulderLeftPoint.Y >= Slide_batasBawah.Value) && (shoulderRightPoint.Y >= Slide_batasBawah.Value) && Syarat1 == 0)
                        {
                            Syarat2 = 1;
                        }
                        if ((shoulderLeftPoint.Y <= Slide_batasBawah.Value ) && ((shoulderRightPoint.Y <= Slide_batasBawah.Value)) && Syarat1 == 0 && Syarat2 == 1)
                        {
                            Syarat3 = 1;
                        }

                        if (Syarat1 == 1 && Syarat2 == 1 && Syarat3 == 1)
                        {
                            Nilai_total++;
                            Syarat1 = 0;
                            Syarat2 = 0;
                            Syarat3 = 0;
                        }
                        Nilai_value.Content = Nilai_total;
                    }
                }
            }
        }

        private void Save_Exel_data()
        {
            int Eror_code = 0;

            try
            {
                Excel.Application ExcelApp = new Excel.Application();

                Excel.Workbook ExcelWorkBook = ExcelApp.Workbooks.Open(@"C:\Users\Public\Documents\Hasil.xlsx");

                //Excel.Workbook ExcelWorkBook = ExcelApp.Workbooks.Open(@"C:\Users\HIKARI\Downloads\WpfApp2\WpfApp2\test.xlsx");
                Excel.Range last = ExcelApp.Cells.SpecialCells(Excel.XlCellType.xlCellTypeLastCell, Type.Missing);
                Excel.Range range = ExcelApp.get_Range("A1", last);

                int lastUsedRow = last.Row;
                int lastUsedColumn = last.Column;

                ExcelApp.Cells[1, 1] = "NO";
                ExcelApp.Cells[1, 2] = "ID Peserta";
                ExcelApp.Cells[1, 3] = "Nama";
                ExcelApp.Cells[1, 4] = "Nilai";
                ExcelApp.Cells[1, 5] = "Sisa Waktu";
                ExcelApp.Cells[1, 6] = "Waktu Ujian";


                ExcelApp.Cells[last.Row + 1, 1] = last.Row - 1;
                ExcelApp.Cells[last.Row + 1, 2] = Input_No.Text;
                ExcelApp.Cells[last.Row + 1, 3] = Input_Nama.Text;
                ExcelApp.Cells[last.Row + 1, 4] = Nilai_total;
                ExcelApp.Cells[last.Row + 1, 5] = Waktu_value.Content;
                ExcelApp.Cells[last.Row + 1, 6] = DateTime.Now.ToString(@"dd-mm-yyyy HH:mm:ss");

                ExcelWorkBook.Save();
                ExcelWorkBook.Close();
                ExcelApp.Quit();
            }
            catch (Exception ex)
            {
                Eror_code = 1;
                MessageBox.Show(ex.Message, "Data Gagal Tersimpan");
            }
            finally
            {
                if (Eror_code == 0)
                {
                    MessageBox.Show("Data Anda Sudah Tersimpan \nSilahkan tekan Ok", "Data tersimpan");

                }
                else { }

            }


        }

        #region Send
        public void SerialCmdSend(string data)
        {
            if (serial.IsOpen)
            {
                try
                {
                    byte[] hexstring = Encoding.ASCII.GetBytes(data);
                    foreach (byte hexval in hexstring)
                    {
                        byte[] _hexval = new byte[] { hexval }; 
                        serial.Write(_hexval, 0, 1);
                        Thread.Sleep(1);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);  
                }
            }
            else
            {
            }
        }
        #endregion
        
        #region Button

        private void buttConnect_Click(object sender, RoutedEventArgs e)
        {   try {          
            if (serial.IsOpen == true)
            {
                buttConnect.Content = "Connect";
                serial.Close();
            }
                
            else
            {
                serial.PortName = cbPort.Text;
                serial.BaudRate = Convert.ToInt32(cbBaudrate.Text);
                serial.Parity = Parity.None;
                serial.DataBits = 8;
                serial.StopBits = StopBits.One;
                serial.ReadTimeout = 200;
                serial.WriteTimeout = 50;
                serial.Open();
                buttConnect.Content = "Disconnect";
            }
            }
            catch (Exception ex)
            { 
                MessageBox.Show(ex.Message, "Kode Error"); 
            }
        }

        private void buttSet_Click(object sender, RoutedEventArgs e)
        {
            if (Convert.ToInt16(Waktu_value.Content) == 60)
            {
                Bt_Start.Content = "Save";
                _time = TimeSpan.FromSeconds(59);
                _timer = new DispatcherTimer(new TimeSpan(0, 0, 1), DispatcherPriority.Normal, delegate
                {
                    Waktu_value.Content = _time.ToString(@"ss");
                    if (_time == TimeSpan.Zero)
                    {
                        _timer.Stop();
                        Counting = false;
                        Save_Exel_data();
                        Bt_Start.Content = "Start";
                        Waktu_value.Content = "60";
                        Nilai_total = 0;
                    }
                    _time = _time.Add(TimeSpan.FromSeconds(-1));

                }, Application.Current.Dispatcher);
                Counting = true;
                _timer.Start();
                Bt_Riset.IsEnabled = true;
            }
            if(Bt_Start.Content == "Save" && Convert.ToInt16(Waktu_value.Content) < 60)
            {
                Counting = false;
                _timer.Stop();
                Save_Exel_data();
                Waktu_value.Content = "60";
                Nilai_value.Content = "0";
                Nilai_total = 0;
                Bt_Riset.IsEnabled = false;
                Bt_Start.Content = "Start";
                Input_Nama.Text = "Nama Peserta";
                Input_No.Text = "No Peserta";


            }
        }

        private void Bt_Riset_Click(object sender, RoutedEventArgs e)
        {
            Counting = false;
            _timer.Stop(); 
            Waktu_value.Content = "60";
            Nilai_value.Content = "0";
            Nilai_total = 0;
            Bt_Riset.IsEnabled = false;
            Bt_Start.Content = "Start";


        }

        private void buttConnect_Copy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (KinectSensor.KinectSensors.Count > 0)
                {
                    sensor = KinectSensor.KinectSensors[0];

                }
                else
                {
                    MessageBox.Show("Kinect Sensor Not Found", "Error");
                    return;
                }
                if (sensor.Status == KinectStatus.Connected)
                {
                    sensor.ColorStream.Enable();
                    sensor.DepthStream.Enable();
                    sensor.SkeletonStream.Enable();
                    sensor.SkeletonStream.EnableTrackingInNearRange = false;
                    sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;
                    sensor.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(Sensor_AllFramesReady);
                    sensor.Start();
                    Bt_Start.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
        }
        #endregion

        #region Batas_Deteksi
        private void Slide_batasBawah_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            batasBawah.Margin = new Thickness(283, Slide_batasBawah.Value, 0, 0);

        }

        private void Slide_batasAtas1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            batasAtas.Margin = new Thickness(283, Slide_batasAtas1.Value, 0, 0);
        }

        #endregion
    }
}
