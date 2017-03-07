using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Imaging;

namespace RemovingBackgrounds
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            detector = new BackgroundDetector();
            trackBar1.Maximum = 255;
            trackBar1.Minimum = 0;
            trackBar2.Minimum = 50;
            trackBar2.Maximum = 200;
            trackBar2.Value = 100;
        }
        
        public static Bitmap resizeImage(Bitmap imgToResize, Size size)
        {
            return new Bitmap(imgToResize, size);
        }

        public Bitmap StreampTo32bppArgbBitmap(Stream myStream)
        {
            Bitmap orig;
            Bitmap newimg;
            using (myStream)
            {
                orig = new Bitmap(myStream);
                newimg = new Bitmap(orig.Width, orig.Height, PixelFormat.Format32bppArgb);
                using (Graphics gr = Graphics.FromImage(newimg))
                {
                    gr.DrawImage(orig, new Rectangle(0, 0, orig.Width, orig.Height));
                }
            }
            return newimg;
        }

        Bitmap image;
        int w;
        int h;
        int t;
        BackgroundDetector detector;

        private void button1_Click(object sender, EventArgs e)
        {
            Stream myStream = null;
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.InitialDirectory = "c:\\";
            openFileDialog1.Filter = "JPG (*.jpg)|*.jpg|PNG (*.png)|*.png";
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.RestoreDirectory = true;
            String FileName = "";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    FileName = openFileDialog1.FileName;
                    if ((myStream = openFileDialog1.OpenFile()) != null)
                    {
                        image = StreampTo32bppArgbBitmap(myStream);
                        myStream.Close();
                        w_box.Text = (w=image.Width) + "";
                        h_box.Text = (h=image.Height) + "";
                        pictureBox1.Width = image.Width;
                        pictureBox1.Height = image.Height;
                        //image = resizeImage(image, new Size(pictureBox1.Width, pictureBox1.Height));
                        pictureBox1.Image = image;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("無法開啟" + FileName + ":\n" + ex.Message);
                }
            }
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            Bitmap temp = new Bitmap(image);
            detector.detect(e.X, e.Y, trackBar1.Value, image, temp);
            image = temp;
            pictureBox1.Image = image;

        }

        private void button8_Click(object sender, EventArgs e)
        {
            Stream myStream;
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();

            saveFileDialog1.Filter = "PNG files (*.png)|*.png";
            saveFileDialog1.FilterIndex = 1;
            saveFileDialog1.RestoreDirectory = true;
            String FileName = "";
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if ((myStream = saveFileDialog1.OpenFile()) != null)
                {
                    try
                    {
                        if (image != null)
                        {
                            FileName = saveFileDialog1.FileName;
                            int ww = (int)((float)image.Width * ((float)trackBar2.Value / 100.0f));
                            int hh = (int)((float)image.Height * ((float)trackBar2.Value / 100.0f));
                            image = resizeImage(image, new Size(ww, hh));
                            image.Save(myStream, ImageFormat.Png);
                        }
                    }catch(Exception ex)
                    {
                        MessageBox.Show("無法寫入" + FileName + ":\n" + ex.Message);
                    }
                }
            }
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            int ww = (int)((float)image.Width * ((float)trackBar2.Value / 100.0f));
            int hh = (int)((float)image.Height * ((float)trackBar2.Value / 100.0f));
            image = resizeImage(image, new Size(ww, hh));
            pictureBox1.Width = ww;
            pictureBox1.Height = hh;
            pictureBox1.Image = image;
            w_box.Text = ww + "";
            h_box.Text = hh + "";
        }
    }

    class BackgroundDetector
    {
        private class entry
        {
            public int x;
            public int y;
            public entry(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
        }
        Color target_color;
        Bitmap bmp;
        Boolean[,] is_background;
        List<entry> BFS_array;
        Boolean[,] go;
        private int BFS(int t)
        {
            entry e = BFS_array.ElementAt(0);
            BFS_array.RemoveAt(0);

            int d = 0;
            int A = bmp.GetPixel(e.x, e.y).A;
            int R = bmp.GetPixel(e.x, e.y).R;
            int G = bmp.GetPixel(e.x, e.y).G;
            int B = bmp.GetPixel(e.x, e.y).B;
            int tA = target_color.A;
            int tR = target_color.R;
            int tG = target_color.G;
            int tB = target_color.B;
            int dA = Math.Abs(A - tA);
            int dR = Math.Abs(R - tR);
            int dG = Math.Abs(G - tG);
            int dB = Math.Abs(B - tB);
            d = dA + dR + dG + dB;


            if (Math.Abs(d)<t)
            {
                is_background[e.x,e.y] = true;
                if ((e.x + 1) < bmp.Width)
                {
                    if (!is_background[e.x + 1,e.y])
                    {
                        if (!go[e.x + 1,e.y])
                        {
                            //加到List裡面代表遲早會執行,直接視為一經處理過,防止List前面的元素在重複加入
                            go[e.x + 1,e.y] = true;
                            BFS_array.Add(new entry(e.x + 1, e.y));
                        }
                    }
                }
                if ((e.y + 1) < bmp.Height)
                {
                    if (!is_background[e.x,e.y + 1])
                    {
                        if (!go[e.x,e.y + 1])
                        {
                            go[e.x,e.y + 1] = true;
                            BFS_array.Add(new entry(e.x, e.y + 1));
                        }
                    }
                }
                if ((e.x - 1) >= 0)
                {
                    if (!is_background[e.x - 1,e.y])
                    {
                        if (!go[e.x - 1,e.y])
                        {
                            go[e.x - 1,e.y] = true;
                            BFS_array.Add(new entry(e.x - 1, e.y));
                        }
                    }
                }
                if ((e.y - 1) >= 0)
                {
                    if (!is_background[e.x,e.y - 1])
                    {
                        if (!go[e.x,e.y - 1])
                        {
                            go[e.x,e.y - 1] = true;
                            BFS_array.Add(new entry(e.x, e.y - 1));
                        }
                    }
                }
            }
            return BFS_array.Count;
        }

        public void detect(int x,int y,int t,Bitmap RGBA_bitmap, Bitmap RGBA_result)
        {
            target_color = RGBA_bitmap.GetPixel(x, y);
            this.bmp = RGBA_bitmap;
            is_background = new Boolean[bmp.Width,bmp.Height];
            this.go = new Boolean[bmp.Width, bmp.Height];
            for (int i = 0; i < bmp.Width; i++)
            {
                for (int j = 0; j < bmp.Height; j++)
                {
                    is_background[i,j] = false;
                    go[i,j] = false;
                }
            }
            BFS_array = new List<entry>();
            go[x,y] = true;
            BFS_array.Add(new entry(x, y));
            int c;
            while ((c = BFS(t)) > 0)
            {
                //Log.d("ccc","size="+c);
            }
            for (int i = 0; i < bmp.Width; i++)
            {
                for (int j = 0; j < bmp.Height; j++)
                {
                    if (is_background[i,j])
                    {
                        RGBA_result.SetPixel(i, j, Color.Transparent);
                    }
                    else
                    {
                        RGBA_result.SetPixel(i, j, bmp.GetPixel(i, j));
                    }
                }
            }
        }
    }
}
