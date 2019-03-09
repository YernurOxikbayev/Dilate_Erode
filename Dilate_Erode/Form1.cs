using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;



namespace lab4
{
    public partial class Form1 : Form
    {
        private Bitmap bmp;
        private bool[] binIm;
        private int maskSize = 3;

        public Form1()
        {
            InitializeComponent();
        }

        private void button_open_Click(object sender, EventArgs e)
        {
            getFile(pictureBox1);
        }
        public void getFile(PictureBox pictureBox1)
        {

            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Image files (*.BMP, *.JPG, *.GIF, *.TIF, *.PNG, *.ICO, *.EMF, *.WMF)|*.bmp;*.jpg;*.gif; *.tif; *.png; *.ico; *.emf; *.wmf";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                Image image = Image.FromFile(dialog.FileName);
                int width = pictureBox1.Width;
                int height = pictureBox1.Height;

                bmp = new Bitmap(image, width, height);
                ResetBinImage();
                pictureBox1.Image = bmp;

                button1.Enabled = true;
                button2.Enabled = true;
                button3.Enabled = true;
                button4.Enabled = true;
                button5.Enabled = true;
            }
        }
        private void btn_Reset_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = bmp;
            ResetBinImage();
            label1.Text = "";
        }
        private void ResetBinImage()
        {
            int width = bmp.Width;
            int height = bmp.Height;

            BitmapData data = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            int bytes = data.Stride * data.Height;

            byte[] pixelBuffer = new byte[bytes];
            binIm = new bool[bytes/4];

            IntPtr first_pixel = data.Scan0;

            Marshal.Copy(first_pixel, pixelBuffer, 0, bytes);

            bmp.UnlockBits(data);

            float rgb = 0;
            for (int i = 0; i < pixelBuffer.Length; i += 4)
            {
                rgb = pixelBuffer[i] * .21f;
                rgb += pixelBuffer[i + 1] * .71f;
                rgb += pixelBuffer[i + 2] * .071f;

                binIm[i/4] = rgb > 127;
            }
        }

        private Bitmap DilateAndErodeFilter(MorphologyType morphType, int matrixSize)
        {//   Размеры изображения 
            int width = bmp.Width;
            int height = bmp.Height;
            //Создание массивов байтов для хранения информации о пикселях изображения
            bool[] resultBw = (bool[])binIm.Clone();
            byte[] resultBuffer = new byte[width * height * 4];
            //центральный пиксель смещен от границы ядра    
            int filterOffset = (matrixSize - 1) / 2;
            int calcOffset = 0;

            int byteOffset = 0;

            bool res;

            if (morphType == MorphologyType.Dilation)
            {
                for (int offsetY = filterOffset; offsetY < height - filterOffset; offsetY++)
                {
                    for (int offsetX = filterOffset; offsetX < width - filterOffset; offsetX++)
                    {  //положение пикселя центра ядра
                        byteOffset = offsetY * width + offsetX;
                        res = true;
                        // Применение дилатации
                        for (int filterY = -filterOffset;filterY <= filterOffset && res; filterY++)
                        {
                            for (int filterX = -filterOffset;filterX <= filterOffset; filterX++)
                            {
                                calcOffset = (offsetY + filterY) * width + offsetX + filterX;

                                if (!binIm[calcOffset])
                                {
                                    res = false;
                                    break;
                                }
                            }
                        }

                        resultBw[byteOffset] = res;
                        //Запись обработанных данных во второй массив
                        resultBuffer[byteOffset * 4] = res ? (byte)255 : (byte)0;
                        resultBuffer[byteOffset * 4 + 1] = resultBuffer[byteOffset * 4];
                        resultBuffer[byteOffset * 4 + 2] = resultBuffer[byteOffset * 4];
                        resultBuffer[byteOffset * 4 + 3] = 255;
                    }
                }
            }
            else if (morphType == MorphologyType.Erosion)
            {
                for (int offsetY = filterOffset; offsetY < height - filterOffset; offsetY++)
                {
                    for (int offsetX = filterOffset; offsetX < width - filterOffset; offsetX++)
                    {
                        
                        byteOffset = offsetY * width + offsetX;                                                                               // положение пикселя центра ядра
                        res = false;
                        // вычисления ядра
                        for (int filterY = -filterOffset;filterY <= filterOffset && !res; filterY++)
                        {
                            for (int filterX = -filterOffset;filterX <= filterOffset; filterX++)
                            {
                                calcOffset = (offsetY + filterY) * width + offsetX + filterX;

                                if (binIm[calcOffset])
                                {
                                    res = true;
                                    break;
                                }
                            }
                        }

                        resultBw[byteOffset] = resultBw[byteOffset] ? resultBw[byteOffset] : res;
                        //установим новые данные в другом массиве байтов 
                        resultBuffer[byteOffset * 4] = resultBw[byteOffset] ? (byte)255 : (byte)0;
                        resultBuffer[byteOffset * 4 + 1] = resultBuffer[byteOffset * 4];
                        resultBuffer[byteOffset * 4 + 2] = resultBuffer[byteOffset * 4];
                        resultBuffer[byteOffset * 4 + 3] = 255;
                    }
                }                
            }            

            binIm = resultBw;
            // Создаём новое изображение, которое будет содержать обработанные данные
            Bitmap resultBitmap = new Bitmap(width, height);
            BitmapData resultData =resultBitmap.LockBits(new Rectangle(0, 0,resultBitmap.Width, resultBitmap.Height),ImageLockMode.WriteOnly,
                       PixelFormat.Format32bppArgb);
            // Копирование данных изображения в массивов байтов
            Marshal.Copy(resultBuffer, 0, resultData.Scan0,resultBuffer.Length);
            // Разблокируем бит из системной памяти так как у нас есть вся необходимая информация в массиве
            resultBitmap.UnlockBits(resultData);

            return resultBitmap;
        }
               

        private void btn_Dilation_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = DilateAndErodeFilter(MorphologyType.Dilation, maskSize);
            label1.Text = label1.Text + "Дилатация ";
        }

        private void btn_Erosion_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = DilateAndErodeFilter(MorphologyType.Erosion, maskSize);
            label1.Text = label1.Text + "Эррозия ";
        }

        private void btn_Opening_Click(object sender, EventArgs e)
        {
            DilateAndErodeFilter(MorphologyType.Erosion, maskSize);
            pictureBox1.Image = DilateAndErodeFilter(MorphologyType.Dilation, maskSize);
            label1.Text = label1.Text + "Размыкание ";
        }

        private void btn_Closing_Click(object sender, EventArgs e)
        {
            DilateAndErodeFilter(MorphologyType.Dilation, maskSize);
            pictureBox1.Image = DilateAndErodeFilter(MorphologyType.Erosion, maskSize);
            label1.Text = label1.Text + "Замыкание ";
        }

        public enum MorphologyType
        {
            Dilation,
            Erosion
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
