using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET;
using AForge.Video.DirectShow;
using Accord.Video.FFMPEG;
using AForge.Video;
using System.Reflection.Emit;
using System.Text.RegularExpressions;//regular exporessin için
using System.Runtime.InteropServices;
using static GMap.NET.Entity.OpenStreetMapGraphHopperRouteEntity;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Yer_istasyonu_ödev
{
    public partial class Form1 : Form
    {
        string[] portlar = SerialPort.GetPortNames(); //seerial okuma


        float x = 0, y = 0, z = 0;

        FilterInfoCollection fico; //bilgisaara bağlı kameraların dizisi
        private VideoCaptureDevice videoSource;
        private VideoFileWriter videoWriter;
        private bool isRecording = false;

        private Thread _thread;

        private StreamWriter csvWriter;


        public string Username;
        public string Filename;
        public string Fullname;
        public string Server;
        public string Password;
        public string path;
        public string localdest;

        public Form1()
        {
            Control.CheckForIllegalCrossThreadCalls = false;//background hata almamak için threadlar çakışmaması için
            InitializeComponent();

            
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Tarihlabel.Text = DateTime.Now.ToLongDateString();
            Saatlabel.Text = DateTime.Now.ToLongTimeString();

            foreach (string port in portlar)
            {
                PortcomboBox.Items.Add(port);
                PortcomboBox.SelectedIndex = 0;

            }
            BaudratecomboBox.Items.Add("115200");
            BaudratecomboBox.Items.Add("9600");
            BaudratecomboBox.Items.Add("4800");
            BaudratecomboBox.SelectedIndex = 0;



            gMapControl1.DragButton = MouseButtons.Left; //maouse ile işlem yapmamızıı sağlıyor
            gMapControl1.MapProvider = GMapProviders.GoogleMap;


            // Form içinde bir GMapControl nesnesi oluşturun


            // Türkiye'nin koordinatlarını belirleyin
            double lat = 39.925533;
            double lng = 32.866287;

            // Harita ayarlarını yapın
            gMapControl1.MapProvider = GMapProviders.GoogleMap;
            gMapControl1.Position = new PointLatLng(lat, lng);
            gMapControl1.MinZoom = 5;
            gMapControl1.MaxZoom = 18;
            gMapControl1.Zoom = 7;


            // CSV dosyasını oluştur ve başlık satırını yaz
            csvWriter = new StreamWriter("veriler.csv", true);
            csvWriter.WriteLine("Zaman,Veri"); // İlk satır başlık olsun

            InitializeCamera();

        }

        private void Ftpbutton_Click(object sender, EventArgs e)
        {
            if (textBox1.Text != "" && textBox2.Text != "" && textBox3.Text != "")
            {
                using (OpenFileDialog ofd = new OpenFileDialog() { Multiselect = true, ValidateNames = true, Filter = "All Files|*.*" })
                {
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        FileInfo fi = new FileInfo(ofd.FileName);
                        Username = textBox2.Text;
                        Password = textBox3.Text;
                        Server = textBox1.Text;
                        Filename = fi.Name;
                        Fullname = fi.FullName;
                    }


                    //Start the Background and wait a little to start it.
                    Thread.Sleep(1000);
                    backgroundWorker1.RunWorkerAsync();  //the most important command to start the background worker
                    Thread.Sleep(1000);
                }
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            if (textBox1.Text != "" && textBox2.Text != "" && textBox3.Text != "")
            {
                //Upload Method.
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(new Uri(string.Format("{0}/{1}", Server, Filename)));
                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.Credentials = new NetworkCredential(Username, Password);
                Stream ftpstream = request.GetRequestStream();
                FileStream fs = File.OpenRead(Fullname);

                // Method to calculate and show the progress.
                byte[] buffer = new byte[1024];
                double total = (double)fs.Length;
                int byteRead = 0;
                double read = 0;
                do
                {
                    byteRead = fs.Read(buffer, 0, 1024);
                    ftpstream.Write(buffer, 0, byteRead);
                    read += (double)byteRead;
                    double percentage = read / total * 100;
                    //backgroundWorker1.ReportProgress((int)percentage);
                }
                while (byteRead != 0);
                fs.Close();
                ftpstream.Close();
            }
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            label6.Text = "Upload Complete!";
            MessageBox.Show("Upload Complete!");
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            label6.Text = $"Uploaded {e.ProgressPercentage}%";
            label6.Update();
            progressBar1.Value = e.ProgressPercentage;
            progressBar1.Update();
        }

        private void InitializeCamera()
        {
            FilterInfoCollection videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            if (videoDevices.Count > 0)
            {
                videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);
                videoSource.NewFrame += VideoSource_NewFrame;

                videoSource.Start();
            }
            else
            {
                MessageBox.Show("No video devices found.");
            }
        }

        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            try
            {
                if (isRecording)
                {
                    videoWriter.WriteVideoFrame(eventArgs.Frame);
                }

                pictureBox1.Image = (System.Drawing.Image)eventArgs.Frame.Clone();
            }
            catch (Exception ex)
            {
                bagladurumlabel.Text = ex.Message;



            }
        }

            private void button3_Click(object sender, EventArgs e)
        {

            InitializeCamera();
            if (!isRecording)
            {
                StartRecording();
            }
            else
            {
                StopRecording();
            }
        }

        private void StartRecording()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "MP4 Files (*.mp4)|*.mp4";
            saveFileDialog.DefaultExt = "mp4";

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                videoWriter = new VideoFileWriter();
                videoWriter.Open(saveFileDialog.FileName, pictureBox1.Image.Width, pictureBox1.Image.Height, 25, VideoCodec.MPEG4, 1000000);
                isRecording = true;
                Kamerabutton.Text = "Stop Recording";
            }
        }

        private void StopRecording()
        {
            if (isRecording)
            {
                isRecording = false;
                Kamerabutton.Text = "Start Recording";
                videoWriter.Close();
                videoWriter.Dispose();
            }
        }


        public void PortThread()
        {
            _thread = new Thread(() =>
            {
                while (true)
                {
                    PortOkuma();
                    PortYazdırma();

                    Thread.Sleep(1000);
                }
            });
            _thread.IsBackground = true;
            _thread.Start();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
           //timer1.Start();
            PortThread();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            _thread.Abort();//durdurmak için
            serialPort1.DiscardInBuffer();
            if (serialPort1.IsOpen == true)
            {
                serialPort1.Close();
                bagladurumlabel.Text = "bağlantı kapalı";

            }
        }

        private void backgroundWorker3_DoWork(object sender, DoWorkEventArgs e)
        {

        }

        private void silindir(float step, float topla, float radius, float dikey1, float dikey2)
        {
            float eski_step = 0.1f;
            GL.Begin(BeginMode.Quads);//Y EKSEN CIZIM DAİRENİN
            while (step <= 360)
            {
                if (step < 45)
                    GL.Color3(Color.FromArgb(255, 0, 0));
                else if (step < 90)
                    GL.Color3(Color.FromArgb(255, 255, 255));
                else if (step < 135)
                    GL.Color3(Color.FromArgb(255, 0, 0));
                else if (step < 180)
                    GL.Color3(Color.FromArgb(255, 255, 255));
                else if (step < 225)
                    GL.Color3(Color.FromArgb(255, 0, 0));
                else if (step < 270)
                    GL.Color3(Color.FromArgb(255, 255, 255));
                else if (step < 315)
                    GL.Color3(Color.FromArgb(255, 0, 0));
                else if (step < 360)
                    GL.Color3(Color.FromArgb(255, 255, 255));


                float ciz1_x = (float)(radius * Math.Cos(step * Math.PI / 180F));
                float ciz1_y = (float)(radius * Math.Sin(step * Math.PI / 180F));
                GL.Vertex3(ciz1_x, dikey1, ciz1_y);

                float ciz2_x = (float)(radius * Math.Cos((step + 2) * Math.PI / 180F));
                float ciz2_y = (float)(radius * Math.Sin((step + 2) * Math.PI / 180F));
                GL.Vertex3(ciz2_x, dikey1, ciz2_y);

                GL.Vertex3(ciz1_x, dikey2, ciz1_y);
                GL.Vertex3(ciz2_x, dikey2, ciz2_y);
                step += topla;
            }
            GL.End();
            GL.Begin(BeginMode.Lines);
            step = eski_step;
            topla = step;
            while (step <= 180)// UST KAPAK
            {
                if (step < 45)
                    GL.Color3(Color.FromArgb(255, 1, 1));
                else if (step < 90)
                    GL.Color3(Color.FromArgb(250, 250, 200));
                else if (step < 135)
                    GL.Color3(Color.FromArgb(255, 1, 1));
                else if (step < 180)
                    GL.Color3(Color.FromArgb(250, 250, 200));
                else if (step < 225)
                    GL.Color3(Color.FromArgb(255, 1, 1));
                else if (step < 270)
                    GL.Color3(Color.FromArgb(250, 250, 200));
                else if (step < 315)
                    GL.Color3(Color.FromArgb(255, 1, 1));
                else if (step < 360)
                    GL.Color3(Color.FromArgb(250, 250, 200));


                float ciz1_x = (float)(radius * Math.Cos(step * Math.PI / 180F));
                float ciz1_y = (float)(radius * Math.Sin(step * Math.PI / 180F));
                GL.Vertex3(ciz1_x, dikey1, ciz1_y);

                float ciz2_x = (float)(radius * Math.Cos((step + 180) * Math.PI / 180F));
                float ciz2_y = (float)(radius * Math.Sin((step + 180) * Math.PI / 180F));
                GL.Vertex3(ciz2_x, dikey1, ciz2_y);

                GL.Vertex3(ciz1_x, dikey1, ciz1_y);
                GL.Vertex3(ciz2_x, dikey1, ciz2_y);
                step += topla;
            }
            step = eski_step;
            topla = step;
            while (step <= 180)//ALT KAPAK
            {
                if (step < 45)
                    GL.Color3(Color.FromArgb(255, 1, 1));
                else if (step < 90)
                    GL.Color3(Color.FromArgb(250, 250, 200));
                else if (step < 135)
                    GL.Color3(Color.FromArgb(255, 1, 1));
                else if (step < 180)
                    GL.Color3(Color.FromArgb(250, 250, 200));
                else if (step < 225)
                    GL.Color3(Color.FromArgb(255, 1, 1));
                else if (step < 270)
                    GL.Color3(Color.FromArgb(250, 250, 200));
                else if (step < 315)
                    GL.Color3(Color.FromArgb(255, 1, 1));
                else if (step < 360)
                    GL.Color3(Color.FromArgb(250, 250, 200));

                float ciz1_x = (float)(radius * Math.Cos(step * Math.PI / 180F));
                float ciz1_y = (float)(radius * Math.Sin(step * Math.PI / 180F));
                GL.Vertex3(ciz1_x, dikey2, ciz1_y);

                float ciz2_x = (float)(radius * Math.Cos((step + 180) * Math.PI / 180F));
                float ciz2_y = (float)(radius * Math.Sin((step + 180) * Math.PI / 180F));
                GL.Vertex3(ciz2_x, dikey2, ciz2_y);

                GL.Vertex3(ciz1_x, dikey2, ciz1_y);
                GL.Vertex3(ciz2_x, dikey2, ciz2_y);
                step += topla;
            }
            GL.End();
        }
        private void koni(float step, float topla, float radius1, float radius2, float dikey1, float dikey2)
        {
            float eski_step = 0.1f;
            GL.Begin(BeginMode.Lines);//Y EKSEN CIZIM DAİRENİN
            while (step <= 360)
            {
                if (step < 45)
                    GL.Color3(1.0, 1.0, 1.0);
                else if (step < 90)
                    GL.Color3(1.0, 0.0, 0.0);
                else if (step < 135)
                    GL.Color3(1.0, 1.0, 1.0);
                else if (step < 180)
                    GL.Color3(1.0, 0.0, 0.0);
                else if (step < 225)
                    GL.Color3(1.0, 1.0, 1.0);
                else if (step < 270)
                    GL.Color3(1.0, 0.0, 0.0);
                else if (step < 315)
                    GL.Color3(1.0, 1.0, 1.0);
                else if (step < 360)
                    GL.Color3(1.0, 0.0, 0.0);


                float ciz1_x = (float)(radius1 * Math.Cos(step * Math.PI / 180F));
                float ciz1_y = (float)(radius1 * Math.Sin(step * Math.PI / 180F));
                GL.Vertex3(ciz1_x, dikey1, ciz1_y);

                float ciz2_x = (float)(radius2 * Math.Cos(step * Math.PI / 180F));
                float ciz2_y = (float)(radius2 * Math.Sin(step * Math.PI / 180F));
                GL.Vertex3(ciz2_x, dikey2, ciz2_y);
                step += topla;
            }
            GL.End();

            GL.Begin(BeginMode.Lines);
            step = eski_step;
            topla = step;
            while (step <= 180)// UST KAPAK
            {
                if (step < 45)
                    GL.Color3(Color.FromArgb(255, 1, 1));
                else if (step < 90)
                    GL.Color3(Color.FromArgb(250, 250, 200));
                else if (step < 135)
                    GL.Color3(Color.FromArgb(255, 1, 1));
                else if (step < 180)
                    GL.Color3(Color.FromArgb(250, 250, 200));
                else if (step < 225)
                    GL.Color3(Color.FromArgb(255, 1, 1));
                else if (step < 270)
                    GL.Color3(Color.FromArgb(250, 250, 200));
                else if (step < 315)
                    GL.Color3(Color.FromArgb(255, 1, 1));
                else if (step < 360)
                    GL.Color3(Color.FromArgb(250, 250, 200));


                float ciz1_x = (float)(radius2 * Math.Cos(step * Math.PI / 180F));
                float ciz1_y = (float)(radius2 * Math.Sin(step * Math.PI / 180F));
                GL.Vertex3(ciz1_x, dikey2, ciz1_y);

                float ciz2_x = (float)(radius2 * Math.Cos((step + 180) * Math.PI / 180F));
                float ciz2_y = (float)(radius2 * Math.Sin((step + 180) * Math.PI / 180F));
                GL.Vertex3(ciz2_x, dikey2, ciz2_y);

                GL.Vertex3(ciz1_x, dikey2, ciz1_y);
                GL.Vertex3(ciz2_x, dikey2, ciz2_y);
                step += topla;
            }
            step = eski_step;
            topla = step;
            GL.End();
        }
        private void Pervane(float yukseklik, float uzunluk, float kalinlik, float egiklik)
        {
            float radius = 10, angle = 45.0f;
            GL.Begin(BeginMode.Quads);

            GL.Color3(Color.Red);
            GL.Vertex3(uzunluk, yukseklik, kalinlik);
            GL.Vertex3(uzunluk, yukseklik + egiklik, -kalinlik);
            GL.Vertex3(0.0, yukseklik + egiklik, -kalinlik);
            GL.Vertex3(0.0, yukseklik, kalinlik);

            GL.Color3(Color.Red);
            GL.Vertex3(-uzunluk, yukseklik + egiklik, kalinlik);
            GL.Vertex3(-uzunluk, yukseklik, -kalinlik);
            GL.Vertex3(0.0, yukseklik, -kalinlik);
            GL.Vertex3(0.0, yukseklik + egiklik, kalinlik);

            GL.Color3(Color.White);
            GL.Vertex3(kalinlik, yukseklik, -uzunluk);
            GL.Vertex3(-kalinlik, yukseklik + egiklik, -uzunluk);
            GL.Vertex3(-kalinlik, yukseklik + egiklik, 0.0);//+
            GL.Vertex3(kalinlik, yukseklik, 0.0);//-

            GL.Color3(Color.White);
            GL.Vertex3(kalinlik, yukseklik + egiklik, +uzunluk);
            GL.Vertex3(-kalinlik, yukseklik, +uzunluk);
            GL.Vertex3(-kalinlik, yukseklik, 0.0);
            GL.Vertex3(kalinlik, yukseklik + egiklik, 0.0);
            GL.End();

        }

        
        private void glControl1_Load(object sender, EventArgs e)
        {
            GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
            GL.Enable(EnableCap.DepthTest);//sonradan yazdık
        }

        private void glControl1_Paint_1(object sender, PaintEventArgs e)
        {
            float step = 1.0f;
            float topla = step;
            float radius = 5.0f;
            float dikey1 = radius, dikey2 = -radius;
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Clear(ClearBufferMask.DepthBufferBit);

            Matrix4 perspective = Matrix4.CreatePerspectiveFieldOfView(1.04f, 4 / 3, 1, 10000);
            Matrix4 lookat = Matrix4.LookAt(25, 0, 0, 0, 0, 0, 0, 1, 0);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.LoadMatrix(ref perspective);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            GL.LoadMatrix(ref lookat);
            GL.Viewport(0, 0, glControl1.Width, glControl1.Height);
            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Less);

            GL.Rotate(x, 1.0, 0.0, 0.0);//ÖNEMLİ
            GL.Rotate(z, 0.0, 1.0, 0.0);
            GL.Rotate(y, 0.0, 0.0, 1.0);

            silindir(step, topla, radius, 3, -5);
            silindir(0.01f, topla, 0.5f, 9, 9.7f);
            silindir(0.01f, topla, 0.1f, 5, dikey1 + 5);
            koni(0.01f, 0.01f, radius, 3.0f, 3, 5);
            koni(0.01f, 0.01f, radius, 2.0f, -5.0f, -10.0f);
            Pervane(9.0f, 11.0f, 0.2f, 0.5f);

            GL.Begin(BeginMode.Lines);

            GL.Color3(Color.FromArgb(250, 0, 0));
            GL.Vertex3(-30.0, 0.0, 0.0);
            GL.Vertex3(30.0, 0.0, 0.0);


            GL.Color3(Color.FromArgb(0, 0, 0));
            GL.Vertex3(0.0, 30.0, 0.0);
            GL.Vertex3(0.0, -30.0, 0.0);

            GL.Color3(Color.FromArgb(0, 0, 250));
            GL.Vertex3(0.0, 0.0, 30.0);
            GL.Vertex3(0.0, 0.0, -30.0);

            GL.End();
            //GraphicsContext.CurrentContext.VSync = true;
            glControl1.SwapBuffers();
        }

        public void PortOkuma()
        {
            if (serialPort1.IsOpen == false)
            {
                if (PortcomboBox.Text == "")
                    return;
                serialPort1.PortName = PortcomboBox.Text;
                serialPort1.BaudRate = Convert.ToInt32(BaudratecomboBox.Text);



                try
                {
                    serialPort1.Open();
                    bagladurumlabel.Text = "bağlantı açık";
                }
                catch (Exception hata)
                {

                    bagladurumlabel.Text = hata.Message;

                    //throw;
                }

            }

            else
            {
                bagladurumlabel.Text = "bağlantı kuruldu";
            }





        }
        public void PortYazdırma()
        {

            try
            {
                string sonuc = serialPort1.ReadLine();
                string[] parca = sonuc.Split(',');
                string[] parca2 = Regex.Split(sonuc, @"\D+");

                string[] aras = Regex.Split(parca2[3], @"(\d)(\d)(\d)(\d)(\d)");
               
                           

                this.chart1.Series["Gy Basıncı(Pa)"].Points.AddY(parca2[10]);
                this.chart2.Series["Gy Yüksekliği(m)"].Points.AddY(parca2[12]);

                this.chart3.Series["T Basıncı(Pa)"].Points.AddY(parca2[11]);

                this.chart4.Series["T Yüksekliği(m)"].Points.AddY(parca2[13]);
                 this.chart5.Series["İrtifa Farkı(m)"].Points.AddY(parca2[14]);
                this.chart6.Series["Gerilim Farkı(V)"].Points.AddY(parca2[17]);
                 this.chart7.Series["Sıcaklık(C)"].Points.AddY(parca2[16]);
                 this.chart8.Series["İniş Hızı(m/s)"].Points.AddY(parca2[15]);

                string zaman = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string satir = zaman + "," + sonuc;

                // Veriyi CSV dosyasına ekleyin
                this.Invoke(new Action(() =>
                {
                    csvWriter.WriteLine(satir);
                }));





                if (parca2[2] == "0")
                {
                    label7.BackColor = Color.Green;
                }
                else if (parca2[2] == "1")
                {
                    label7.BackColor = Color.Green;
                    label9.BackColor = Color.Green;

                }
                else if (parca2[2] == "2")
                {

                    label7.BackColor = Color.Green;
                    label9.BackColor = Color.Green;
                    label11.BackColor = Color.Green;
                }

                else if (parca2[2] == "3")
                {

                    label7.BackColor = Color.Green;
                    label9.BackColor = Color.Green;
                    label11.BackColor = Color.Green;
                    label14.BackColor = Color.Green;


                }
                else if (parca2[2] == "4")
                {

                    label7.BackColor = Color.Green;
                    label9.BackColor = Color.Green;
                    label11.BackColor = Color.Green;
                    label14.BackColor = Color.Green;
                    label13.BackColor = Color.Green;


                }
                else if (parca2[2] == "5")
                {

                    label7.BackColor = Color.Green;
                    label9.BackColor = Color.Green;
                    label11.BackColor = Color.Green;
                    label14.BackColor = Color.Green;
                    label13.BackColor = Color.Green;
                    label12.BackColor = Color.Green;


                }
                else if (parca2[2] == "6")
                {

                    label7.BackColor = Color.Green;
                    label9.BackColor = Color.Green;
                    label11.BackColor = Color.Green;
                    label14.BackColor = Color.Green;
                    label13.BackColor = Color.Green;
                    label12.BackColor = Color.Green;
                    label10.BackColor = Color.Green;


                }
                else if (parca2[2] == "7")
                {

                    label7.BackColor = Color.Green;
                    label9.BackColor = Color.Green;
                    label11.BackColor = Color.Green;
                    label14.BackColor = Color.Green;
                    label13.BackColor = Color.Green;
                    label12.BackColor = Color.Green;
                    label10.BackColor = Color.Green;
                    label8.BackColor = Color.Green;

                }





                if (aras[1] == "0")
                {
                    textBox4.BackColor = Color.Green;

                }
                else if (aras[1] == "1")
                {
                    textBox4.BackColor = Color.Red;
                }


                if (aras[2] == "0")
                {
                    textBox5.BackColor = Color.Green;

                }
                else if (aras[2] == "1")
                {
                    textBox5.BackColor = Color.Red;
                }

                if (aras[3] == "0")
                {
                    textBox6.BackColor = Color.Green;

                }
                else if (aras[3] == "1")
                {
                    textBox6.BackColor = Color.Red;
                }

                if (aras[4] == "0")
                {
                    textBox7.BackColor = Color.Green;

                }

                else if (aras[4] == "1")
                {
                    textBox7.BackColor = Color.Red;
                }

                if (aras[5] == "0")
                {
                    textBox8.BackColor = Color.Green;

                }
                else if (aras[5] == "1")
                {
                    textBox8.BackColor = Color.Red;
                }

                listBox1.Items.Add(sonuc);



                serialPort1.DiscardInBuffer(); // buffer ı temizlemek için


            }

            catch (Exception ex)
            {
                bagladurumlabel.Text = ex.Message;
                //timer1.Stop();
                _thread.Abort();//durdurmak için


            }


        }
    }
}
