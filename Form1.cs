using EvilDICOM.Core;
using EvilDICOM.Core.Helpers;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using AForge.Imaging.Filters;
using FellowOakDicom;
using FellowOakDicom.Imaging.Codec;
using FellowOakDicom.IO.Buffer;
using System.Drawing;
using FellowOakDicom.Imaging;
using System.Security.Cryptography;
using System.Windows.Forms;
using System.ComponentModel;
using System.Text;
using FellowOakDicom.Imaging.Reconstruction;
using System;


namespace DicomViewer2._0
{

    public partial class Form1 : Form
    {

        //ImageMatrix imgFile = new ImageMatrix(); 
        List<Image> imageList = new List<Image>();//"массив" файлов
        public static List<Image> imageList2 = new List<Image>();//"массив" файлов в другом режиме
        List<Image> coloredImageList = new List<Image>();
        List<Image> imageListWithPoints = new List<Image>();
        List<Bitmap> maskList = new List<Bitmap>();
        List<Point> points = new List<Point>();
        Point cursorPosition = new Point();
        Bitmap pointBitmap;
        List<Bitmap> brushBitmap = new List<Bitmap>();
        Bitmap aortaBitmap;

        int SliceNum; //текущий номер слайса
        int borderleft, borderright; //граница по слоям слева, справа, две точки для ограничения области
        int constborderleft, constborderright;
        public static int scalle; //масштаб 
        bool Filtered = false;
        string folder; //путь к папке
        int reportNum = 0;//кол-во сделанных отчетов
        // public static string[] allfiles;
        public static List<string> alloriginalfiles = new List<string>();
        public static List<string> allfiles = new List<string>();
        bool hounsfield = false;
        bool Coloring = false;
        bool Painting = false;
        bool Erasing = false;
        bool Scrolling_m = false;
        Graphics graphics;

        int? initX = null;
        int? initY = null;

        Bitmap filterbet;
        string checkchosen;

        int intOriginalExStyle = -1;
        bool bEnableAntiFlicker = true;
        bool bOneLayerBrush = true;

        double aorta_value = 0;
        bool aorta = false;

        int current_view_mode = 1;

        int fx, fy, cx, cy, sx, sy, y_m_past;
        Form3 dif_window;

        public Form1()
        {
            ToggleAntiFlicker(false);
            InitializeComponent();
            pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
            scalle = 100;
            scale.Value = 100;
            this.pictureBox1.MouseWheel += new MouseEventHandler(pictureBox_MouseWheel); //событие - прокручивание мыши

            endToolStripButton.Enabled = false;
            changeborderleftToolStripButton.Enabled = false;
            changeborderrightToolStripButton.Enabled = false;
            savingToolStripButton.Enabled = false;
            restartToolStripButton.Enabled = false;
            huToolStripButton.Enabled = false;
            coloringToolStripButton.Enabled = false;
            pointToolStripButton1.Enabled = false;
            PropertyStripLabel.Enabled = false;
            brushToolStripButton.Enabled = false;
            eraserToolStripDropDown.Enabled = false;
            aortaToolStripButton.Enabled = false;
            // this.DoubleBuffered = true;

        }

        protected override CreateParams CreateParams
        {
            get
            {
                if (intOriginalExStyle == -1)
                {
                    intOriginalExStyle = base.CreateParams.ExStyle;
                }
                CreateParams cp = base.CreateParams;
                if (bEnableAntiFlicker)
                {
                    cp.ExStyle |= 0x02000000; //WS_EX_COMPOSITED
                }
                else
                {
                    cp.ExStyle = intOriginalExStyle;
                }
                return cp;
            }
        }

        private void ToggleAntiFlicker(bool Enable)
        {
            bEnableAntiFlicker = Enable;
            this.MaximizeBox = true;
        }


        //Открыть DICOM-файл и вывести на экран  
        private void openToolStripButton_Click(object sender, EventArgs e)
        {
            endToolStripButton.Enabled = false;
            changeborderleftToolStripButton.Enabled = false;
            changeborderrightToolStripButton.Enabled = false;
            savingToolStripButton.Enabled = false;
            restartToolStripButton.Enabled = false;
            huToolStripButton.Enabled = false;
            coloringToolStripButton.Enabled = false;
            pointToolStripButton1.Enabled = false;
            PropertyStripLabel.Enabled = false;
            hounsfield = false;
            Coloring = false;
            Filtered = false;
            coloringToolStripButton.BackColor = SystemColors.ControlLight;
            huToolStripButton.BackColor = SystemColors.ControlLight;
            alloriginalfiles = new List<string>();
            allfiles = new List<string>();
            imageList = new List<Image>();
            imageList2 = new List<Image>();
            coloredImageList = new List<Image>();
            imageListWithPoints = new List<Image>();
            maskList = new List<Bitmap>();
            points = new List<Point>();
            aorta_value = 0;
            current_view_mode = 1;

            DialogResult res = folderBrowserDialog1.ShowDialog();

            aortaBitmap = new Bitmap(512, 512);
            using (Graphics gfx2 = Graphics.FromImage(aortaBitmap))
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(0, 0, 0)))
            {
                gfx2.FillRectangle(brush, 0, 0, 512, 512);
            }

            if (res == DialogResult.OK)
            {

                try
                {

                    hScrollBar1.Enabled = false;
                    reportNum = 0;
                    folder = folderBrowserDialog1.SelectedPath;
                    string fname = new DirectoryInfo(folder).Name;
                    this.Text = "DicomViewer: " + fname;
                    //allfiles = Directory.GetFiles(folder, "*.dcm");
                    alloriginalfiles = Directory.GetFiles(folder).ToList();
                    imageList = new List<Image>();
                    imageList2 = new List<Image>();
                    hScrollBar1.Enabled = true;

                    Form2 series = new Form2();
                    series.ShowDialog();

                    checkchosen = series.chosenone;

                    Cursor = Cursors.WaitCursor;
                    int flag = 1;

                    foreach (var file in alloriginalfiles)
                    {
                        var dcm = DICOMObject.Read(file);

                        string option;
                        if (dcm.FindFirst(TagHelper.ScanOptions) != null)
                            option = dcm.FindFirst(TagHelper.ScanOptions).DData.ToString();
                        else
                            option = "";

                        string description = dcm.FindFirst(TagHelper.SeriesDescription).DData.ToString();

                        string line;
                        if (description != "")
                        {
                            line = option + " " + description;
                        }
                        else
                            line = option;


                        if (line == checkchosen)
                        {

                            allfiles.Add(file);
                            var dicomfile = DicomFile.Open(file);
                            DicomDataset dataset = new DicomDataset();
                            dataset = dicomfile.Dataset.Clone();
                            DicomTranscoder x = new DicomTranscoder(DicomTransferSyntax.JPEGProcess14SV1, DicomTransferSyntax.ImplicitVRLittleEndian);
                            IByteBuffer newpixelData1 = x.DecodeFrame(dataset, 0);

                            List<byte> newpixelData = newpixelData1.Data.ToList();

                            string photo = dcm.FindFirst(TagHelper.PhotometricInterpretation).DData.ToString();
                            ushort bitsAllocated = (ushort)dcm.FindFirst(TagHelper.BitsAllocated).DData;
                            ushort highBit = (ushort)dcm.FindFirst(TagHelper.HighBit).DData;
                            ushort bitsStored = (ushort)dcm.FindFirst(TagHelper.BitsStored).DData;

                            double intercept = 0;
                            if (dcm.FindFirst(TagHelper.RescaleIntercept) != null)
                                intercept = (double)dcm.FindFirst(TagHelper.RescaleIntercept).DData;
                            else
                            {
                                if (flag != 0)
                                {
                                    MessageBox.Show("Вы выбрали серию файлов, которую нельзя визуалировать.", "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                                    openToolStripButton.Enabled = true;
                                    flag = 0;
                                    //Cursor = Cursors.Arrow;
                                }

                            }
                            if (flag != 0)
                            {
                                double slope = (double)dcm.FindFirst(TagHelper.RescaleSlope).DData;
                                ushort rows = (ushort)dcm.FindFirst(TagHelper.Rows).DData;
                                ushort colums = (ushort)dcm.FindFirst(TagHelper.Columns).DData;
                                ushort pixelRepresentation = (ushort)dcm.FindFirst(TagHelper.PixelRepresentation).DData;

                                //List<byte> pixelData = (List<byte>)dcm.FindFirst(TagHelper.PixelData).DData_;
                                List<byte> pixelData = newpixelData;

                                double window = (double)dcm.FindFirst(TagHelper.WindowWidth).DData;
                                double level = (double)dcm.FindFirst(TagHelper.WindowCenter).DData;

                                int index = 0;
                                byte[] outPixelData = new byte[rows * colums * 4];//rgba
                                ushort mask = (ushort)(ushort.MaxValue >> (bitsAllocated - bitsStored));
                                double maxval = Math.Pow(2, bitsStored);

                                for (int i = 0; i < pixelData.Count; i += 2)
                                {
                                    ushort gray = (ushort)((ushort)(pixelData[i]) + (ushort)(pixelData[i + 1] << 8));
                                    double valgray = gray & mask;

                                    if (pixelRepresentation == 1)
                                    {
                                        if (valgray > (maxval / 2))
                                            valgray = valgray - maxval;

                                    }

                                    valgray = slope * valgray + intercept;

                                    double half = ((window - 1) / 2.0) - 0.5;

                                    if (valgray <= level - half)
                                        valgray = 0;
                                    else if (valgray >= level + half)
                                        valgray = 255;
                                    else
                                        valgray = ((valgray - (level - 0.5)) / (window - 1) + 0.5) * 255;

                                    outPixelData[index] = (byte)valgray;
                                    outPixelData[index + 1] = (byte)valgray;
                                    outPixelData[index + 2] = (byte)valgray;
                                    outPixelData[index + 3] = 255;

                                    index += 4;
                                }

                                Image newimage = ImageFromRawBgraArray(outPixelData, colums, rows);

                                imageList.Add(newimage);

                                window = 1500;
                                level = -400;

                                index = 0;
                                outPixelData = new byte[rows * colums * 4];//rgba
                                mask = (ushort)(ushort.MaxValue >> (bitsAllocated - bitsStored));
                                maxval = Math.Pow(2, bitsStored);

                                for (int i = 0; i < pixelData.Count; i += 2)
                                {
                                    ushort gray = (ushort)((ushort)(pixelData[i]) + (ushort)(pixelData[i + 1] << 8));
                                    double valgray = gray & mask;

                                    if (pixelRepresentation == 1)
                                    {
                                        if (valgray > (maxval / 2))
                                            valgray = valgray - maxval;

                                    }

                                    valgray = slope * valgray + intercept;

                                    double half = ((window - 1) / 2.0) - 0.5;

                                    if (valgray <= level - half)
                                        valgray = 0;
                                    else if (valgray >= level + half)
                                        valgray = 255;
                                    else
                                        valgray = ((valgray - (level - 0.5)) / (window - 1) + 0.5) * 255;

                                    outPixelData[index] = (byte)valgray;
                                    outPixelData[index + 1] = (byte)valgray;
                                    outPixelData[index + 2] = (byte)valgray;
                                    outPixelData[index + 3] = 255;

                                    index += 4;

                                }
                                Image newimage2 = ImageFromRawBgraArray(outPixelData, colums, rows);

                                imageList2.Add(newimage2);

                            }
                        }
                    }
                    //new 04.12
                    string[] alldi = new string[allfiles.Count];
                    Image[] allim = new Image[imageList.Count];
                    Image[] allim2 = new Image[imageList2.Count];
                    for (int i = 0; i < allfiles.Count; i++)
                    {
                        var dcm = DICOMObject.Read(allfiles[i]);
                        int index = (int)dcm.FindFirst(TagHelper.InstanceNumber).DData - 1;
                        alldi[index] = allfiles[i];
                        allim[index] = imageList[i];
                        allim2[index] = imageList2[i];
                    }
                    allfiles = new List<string>(); imageList = new List<Image>(); imageList2 = new List<Image>();
                    allfiles = alldi.ToList();
                    imageList = allim.ToList();
                    imageList2 = allim2.ToList();

                    if (flag == 1)
                    {
                        pictureBox1.Image = imageList[0];
                        borderleft = 0;
                        borderrleftToolStrip.Text = (borderleft + 1).ToString();
                        borderright = imageList.Count() - 1;

                        hScrollBar1.Minimum = borderleft;
                        hScrollBar1.Maximum = borderright;
                        borderrrightToolStrip.Text = (borderright + 1).ToString();

                        constborderleft = borderleft;
                        constborderright = borderright;

                        pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
                        pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                        pictureBox1.BackColor = SystemColors.Desktop;

                        hScrollBar1.Value = 0;
                        //hscrollvalueStatusLabel.Text = "0/" + (imageList.Count - 1).ToString();
                        hscrollvalueStatusLabel.Text = "1/" + (imageList.Count).ToString();
                        SliceNum = 0;
                        scale.Value = 100;
                        scaleStatusLabel.Text += " 100%";

                        changeborderleftToolStripButton.Enabled = true;
                        changeborderrightToolStripButton.Enabled = true;
                        endToolStripButton.Enabled = false;
                        restartToolStripButton.Enabled = true;
                        savingToolStripButton.Enabled = true;
                        hounsfield = false;
                        huToolStripButton.Enabled = true;
                        PropertyStripLabel.Enabled = true;
                        aortaToolStripButton.Enabled = true;
                        Cursor = Cursors.Arrow;
                        bigpic();
                        alloriginalfiles.Clear();
                        toolStripStatusLabel2.Text = "Серия: " + checkchosen;
                        dif_window = new Form3(imageList2);
                    }
                    else
                    {
                        Application.Restart();
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("В данной папке отсутствуют или повреждены DICOM-файлы", "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    Application.Restart();
                    //Cursor = Cursors.Arrow;
                    //openToolStripButton.Enabled = true;
                }
            }
            //чтобы можно было повторно попытаться выбрать файл, если сначала нажал Х
            if (res == DialogResult.Cancel || res == DialogResult.Abort)
            {
                openToolStripButton.Enabled = true;
            }
        }


        public void bigpic()
        {
            scale.Value = 200;
            pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox1.Width = pictureBox1.Width * scale.Value / 100;
            pictureBox1.Height = pictureBox1.Height * scale.Value / 100;
            pictureBox1.Location = new Point(((panel1.Width - pictureBox1.Width) / 2), ((panel1.Height - pictureBox1.Height) / 2));

            scalle = scale.Value;
            scaleStatusLabel.Text = "Масштаб: " + scalle.ToString() + "%";
        }


        public Image ImageFromRawBgraArray(byte[] arr, int width, int height)
        {
            var output = new Bitmap(width, height);
            var rect = new Rectangle(0, 0, width, height);
            var bmpData = output.LockBits(rect, ImageLockMode.ReadWrite, output.PixelFormat);
            var ptr = bmpData.Scan0;

            Marshal.Copy(arr, 0, ptr, arr.Length);
            output.UnlockBits(bmpData);

            return output;
        }

        //считает массив хаунсфилдов по пикселям
        private int[,] GetHounsfieldOfSlice(int slice)
        {
            if (allfiles != null)
            {
                var dcm = DICOMObject.Read(allfiles[slice]);

                var dicomfile = DicomFile.Open(allfiles[slice]);
                DicomDataset dataset = new DicomDataset();
                dataset = dicomfile.Dataset.Clone();
                DicomTranscoder xx = new DicomTranscoder(DicomTransferSyntax.JPEGProcess14SV1, DicomTransferSyntax.ImplicitVRLittleEndian);
                IByteBuffer newpixelData1 = xx.DecodeFrame(dataset, 0);

                List<byte> newpixelData = newpixelData1.Data.ToList();
                List<byte> pixelData = newpixelData;

                ushort bitsAllocated = (ushort)dcm.FindFirst(TagHelper.BitsAllocated).DData;
                ushort bitsStored = (ushort)dcm.FindFirst(TagHelper.BitsStored).DData;
                double intercept = (double)dcm.FindFirst(TagHelper.RescaleIntercept).DData;
                double slope = (double)dcm.FindFirst(TagHelper.RescaleSlope).DData;
                ushort pixelRepresentation = (ushort)dcm.FindFirst(TagHelper.PixelRepresentation).DData;
                //List<byte> pixelData = (List<byte>)dcm.FindFirst(TagHelper.PixelData).DData_;

                ushort mask = (ushort)(ushort.MaxValue >> (bitsAllocated - bitsStored));
                double maxval = Math.Pow(2, bitsStored);
                int[,] pix = new int[512, 512];
                for (int x = 0; x < 512; x++)
                {
                    for (int y = 0; y < 512; y++)
                    {
                        ushort gray;
                        if ((2 * (y * 512 + x) + 1) <= 524288)
                            gray = (ushort)((ushort)(pixelData[2 * (y * 512 + x)]) + (ushort)(pixelData[2 * (y * 512 + x) + 1] << 8));
                        else
                            gray = (ushort)((ushort)(pixelData[524286]) + (ushort)(pixelData[524287] << 8));
                        double valgray = gray & mask;

                        if (pixelRepresentation == 1)
                        {
                            if (valgray > (maxval / 2))
                                valgray = (valgray - maxval);

                        }
                        pix[x, y] = (int)(slope * valgray + intercept);
                    }
                }
                return pix;
            }
            else
                return null;
        }

        //значение в Хаунсфилдах для конкретного пикселя
        public double GetHounsfieldOfPixel(int x, int y, int slice)
        {
            var dcm = DICOMObject.Read(allfiles[slice]);
            var dicomfile = DicomFile.Open(allfiles[slice]);
            DicomDataset dataset = new DicomDataset();
            dataset = dicomfile.Dataset.Clone();
            DicomTranscoder xx = new DicomTranscoder(DicomTransferSyntax.JPEGProcess14SV1, DicomTransferSyntax.ImplicitVRLittleEndian);
            IByteBuffer newpixelData1 = xx.DecodeFrame(dataset, 0);

            List<byte> newpixelData = newpixelData1.Data.ToList();
            List<byte> pixelData = newpixelData;

            ushort bitsAllocated = (ushort)dcm.FindFirst(TagHelper.BitsAllocated).DData;
            ushort bitsStored = (ushort)dcm.FindFirst(TagHelper.BitsStored).DData;
            double intercept = (double)dcm.FindFirst(TagHelper.RescaleIntercept).DData;
            double slope = (double)dcm.FindFirst(TagHelper.RescaleSlope).DData;
            ushort pixelRepresentation = (ushort)dcm.FindFirst(TagHelper.PixelRepresentation).DData;
            //List<byte> pixelData = (List<byte>)dcm.FindFirst(TagHelper.PixelData).DData_;

            int index = 0;
            ushort mask = (ushort)(ushort.MaxValue >> (bitsAllocated - bitsStored));
            double maxval = Math.Pow(2, bitsStored);

            ushort gray;
            if ((2 * (y * 512 + x) + 1) <= 524288)
                gray = (ushort)((ushort)(pixelData[2 * (y * 512 + x)]) + (ushort)(pixelData[2 * (y * 512 + x) + 1] << 8));
            else
                gray = (ushort)((ushort)(pixelData[524286]) + (ushort)(pixelData[524287] << 8));
            double valgray = gray & mask;

            if (pixelRepresentation == 1)
            {
                if (valgray > (maxval / 2))
                    valgray = (valgray - maxval);

            }
            valgray = slope * valgray + intercept;

            return valgray;
        }


        //функция заливки по нажатию на область. 
        private Bitmap paintzone(Bitmap sourceImage, Bitmap brushFilter, int x, int y, int deltaX, int deltaY)
        {
            Bitmap whites = new Bitmap(1, 1);
            for (int i = 0; i < whites.Width; i++)
            {
                for (int j = 0; j < whites.Height; j++)
                {
                    whites.SetPixel(i, j, Color.White);
                }
            }
            Grayscale filter1 = new Grayscale(0.2125, 0.7154, 0.0721);
            Bitmap white = filter1.Apply(whites);
            Color color = white.GetPixel(0, 0);
            Color borderColor = white.GetPixel(0, 0);


            sourceImage.SetPixel(x, y, color);
            Bitmap image = (Bitmap)sourceImage.Clone();
            Bitmap imageNew = new Bitmap(sourceImage.Width, sourceImage.Height);

            Stack<Point> points = new Stack<Point>();
            points.Push(new Point(x, y));

            Point currentPoint;
            while (points.Count != 0)
            {
                currentPoint = points.Pop();
                image.SetPixel(currentPoint.X, currentPoint.Y, color);
                imageNew.SetPixel(currentPoint.X, currentPoint.Y, color);

                if ((currentPoint.X >= 0) && (currentPoint.X < image.Width) && ((currentPoint.Y + 1) >= 0) && ((currentPoint.Y + 1) < image.Height))
                {
                    Color topPixel = image.GetPixel(currentPoint.X, currentPoint.Y + 1);
                    Color topPixelGreen = brushFilter.GetPixel(currentPoint.X, currentPoint.Y + 1);

                    if ((topPixelGreen.R >= topPixelGreen.G || topPixelGreen.Name == "ffff") && topPixel.ToArgb() != borderColor.ToArgb() && topPixel.ToArgb() != color.ToArgb())
                    {
                        points.Push(new Point(currentPoint.X, currentPoint.Y + 1));
                    }
                }

                if (((currentPoint.X + 1) >= 0) && ((currentPoint.X + 1) < image.Width) && (currentPoint.Y >= 0) && (currentPoint.Y < image.Height))
                {
                    Color rightPixel = image.GetPixel(currentPoint.X + 1, currentPoint.Y);
                    Color rightPixelGreen = brushFilter.GetPixel(currentPoint.X + 1, currentPoint.Y);

                    if ((rightPixelGreen.R >= rightPixelGreen.G || rightPixelGreen.Name == "ffff") && rightPixel.ToArgb() != borderColor.ToArgb() && rightPixel.ToArgb() != color.ToArgb())
                    {
                        points.Push(new Point(currentPoint.X + 1, currentPoint.Y));
                    }
                }

                if ((currentPoint.X >= 0) && (currentPoint.X < image.Width) && ((currentPoint.Y - 1) >= 0) && ((currentPoint.Y - 1) < image.Height))
                {
                    Color bottomPixel = image.GetPixel(currentPoint.X, currentPoint.Y - 1);
                    Color bottomPixelGreen = brushFilter.GetPixel(currentPoint.X, currentPoint.Y - 1);

                    if ((bottomPixelGreen.R >= bottomPixelGreen.G || bottomPixelGreen.Name == "ffff") && bottomPixel.ToArgb() != borderColor.ToArgb() && bottomPixel.ToArgb() != color.ToArgb())
                    {
                        points.Push(new Point(currentPoint.X, currentPoint.Y - 1));
                    }
                }

                if (((currentPoint.X - 1) >= 0) && ((currentPoint.X - 1) < image.Width) && (currentPoint.Y >= 0) && (currentPoint.Y < image.Height))
                {
                    Color leftPixel = image.GetPixel(currentPoint.X - 1, currentPoint.Y);
                    Color leftPixelGreen = brushFilter.GetPixel(currentPoint.X - 1, currentPoint.Y);

                    if ((leftPixelGreen.R >= leftPixelGreen.G || leftPixelGreen.Name == "ffff") && leftPixel.ToArgb() != borderColor.ToArgb() && leftPixel.ToArgb() != color.ToArgb())
                    {
                        points.Push(new Point(currentPoint.X - 1, currentPoint.Y));
                    }
                }
            }

            foreach (Point po in points)
            {
                for (int i = 1; i < 5; i++)
                {
                    image.SetPixel(po.X, po.Y - i, color);
                    imageNew.SetPixel(po.X, po.Y - i, color);
                    image.SetPixel(po.X, po.Y + i, color);
                    imageNew.SetPixel(po.X, po.Y + i, color);
                    image.SetPixel(po.X - i, po.Y, color);
                    imageNew.SetPixel(po.X - i, po.Y, color);
                    image.SetPixel(po.X + i, po.Y, color);
                    imageNew.SetPixel(po.X + i, po.Y, color);
                }

            }
            return imageNew;
        }


        //бинаризация по порогу значения хаунсфилда
        public Image Binarization(Image img, int[,] allPix, int porog)
        {
            if (pictureBox1 != null)
            {
                Bitmap b = new Bitmap(img);
                Bitmap bb = new Bitmap(img);

                for (int i = 0; i < b.Width; i++)
                {
                    for (int j = 0; j < b.Height; j++)
                    {
                        if (allPix[i, j] >= porog)

                            bb.SetPixel(i, j, Color.White);
                        else
                            bb.SetPixel(i, j, Color.Black);
                    }
                }
                Image output = bb;
                return output;
            }
            return null;
        }


        //эрозия отбинаризованного изображения
        public Bitmap Eroz(Image img)
        {
            Bitmap b = new Bitmap(img);
            Grayscale filter1 = new Grayscale(0.2125, 0.7154, 0.0721);
            Bitmap grayImage = filter1.Apply(b);
            BinaryErosion3x3 filter2 = new BinaryErosion3x3();
            Bitmap res = filter2.Apply(grayImage);
            return res;
        }


        // масштабирование скроллбаром
        private async void scroll()
        {

            //this.DoubleBuffered = true;
            //this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);

            if (pictureBox1.Image != null)
            {

                pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
                pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;

                pictureBox1.Width = pictureBox1.Width * scale.Value / 100;
                pictureBox1.Height = pictureBox1.Height * scale.Value / 100;

                scalle = scale.Value;
                scaleStatusLabel.Text = "Масштаб: " + scalle.ToString() + "%";
                dif_window.change_scale(scalle);
            }

        }

        //прокрутка слоев колесиком мыши
        void pictureBox_MouseWheel(object sender, MouseEventArgs e) //прокручивание мыши
        {
            ((HandledMouseEventArgs)e).Handled = true; //чтобы не работала мышь на скролл изображения вверх-вниз
            if (pictureBox1.Image != null)
            {
                if (e.Delta < 0 && SliceNum + 1 <= constborderright)
                {
                    SliceNum += 1;
                }
                else if (e.Delta > 0 && SliceNum - 1 >= constborderleft)
                {
                    SliceNum -= 1;
                }

                //if (current_view_mode == 3)
                //{
                    dif_window.change_image(SliceNum);
                //}

                if (current_view_mode == 2)
                {
                    pictureBox1.Image = imageList2[SliceNum];
                }
                else if (!Filtered)
                {
                    pictureBox1.Image = imageList[SliceNum];
                    if (points.Count != 0)
                    {
                        TextImage.Screen filter = new TextImage.Screen(brushBitmap[SliceNum - borderleft]);
                        Bitmap tmpBitmap = new Bitmap(imageListWithPoints[SliceNum - borderleft]);
                        pictureBox1.Image = filter.Apply(tmpBitmap);
                    }
                }
                else
                {
                    pictureBox1.Image = coloredImageList[SliceNum - borderleft];
                }
                panel1.Refresh();
                hScrollBar1.Value = SliceNum;
                //hscrollvalueStatusLabel.Text = SliceNum.ToString() + "/" + (imageList.Count - 1).ToString();
                hscrollvalueStatusLabel.Text = (SliceNum + 1).ToString() + "/" + (imageList.Count).ToString();
            }
        }


        //Прокрутка слоев скроллбаром
        private void hScrollBar1_Scroll(object sender, ScrollEventArgs e) //прокрутка по слоям
        {
            if (pictureBox1.Image != null)
            {
                SliceNum = hScrollBar1.Value;

                //if (current_view_mode == 3)
                //{
                    dif_window.change_image(SliceNum);
                //}

                if (current_view_mode == 2)
                {
                    pictureBox1.Image = imageList2[SliceNum];
                }
                else if (!Filtered)
                {
                    pictureBox1.Image = imageList[SliceNum];
                    if (points.Count != 0)
                    {
                        TextImage.Screen filter = new TextImage.Screen(brushBitmap[SliceNum - borderleft]);
                        Bitmap tmpBitmap = new Bitmap(imageListWithPoints[SliceNum - borderleft]);
                        pictureBox1.Image = filter.Apply(tmpBitmap);
                    }
                }
                else
                {
                    pictureBox1.Image = coloredImageList[SliceNum - borderleft];
                }
                panel1.Refresh();
                // hscrollvalueStatusLabel.Text = SliceNum.ToString() + "/" + (imageList.Count - 1).ToString();
                hscrollvalueStatusLabel.Text = (SliceNum + 1).ToString() + "/" + (imageList.Count).ToString();
            }
        }


        //Сохранение текстового отчета — НЕ ИСПОЛЬЗУЕТСЯ
        private void endToolStripButton_Click(object sender, EventArgs e)
        {
            make_report();
        }


        //Изменение левой границы слоев
        private void changeborderleftToolStripButton_Click(object sender, EventArgs e) //левая граница слоев
        {
            if (pictureBox1.Image != null)
            {
                if (SliceNum < borderright)
                {
                    borderleft = SliceNum;
                    borderrleftToolStrip.Text = (borderleft + 1).ToString();
                }
                else
                {
                    borderleft = borderright;
                    borderright = SliceNum;
                    borderrleftToolStrip.Text = (borderleft + 1).ToString();
                    borderrrightToolStrip.Text = (borderright + 1).ToString();
                }
            }
        }


        //Изменение правой границы слоев
        private void changeborderrightToolStripButton_Click(object sender, EventArgs e) //правая граница слоев
        {
            if (pictureBox1.Image != null)
            {
                if (SliceNum > borderleft)
                {
                    borderright = SliceNum;
                    borderrrightToolStrip.Text = (borderright + 1).ToString();
                }
                else
                {
                    borderright = borderleft;
                    borderleft = SliceNum;
                    borderrleftToolStrip.Text = (borderleft + 1).ToString();
                    borderrrightToolStrip.Text = (borderright + 1).ToString();
                }
            }
        }

        //вкл/выкл режим показа хаунсфилдов
        private void huToolStripButton_Click(object sender, EventArgs e)
        {
            hounsfield = !hounsfield;
            huToolStripButton.BackColor = (huToolStripButton.BackColor == SystemColors.ControlLight) ? SystemColors.ControlDark : SystemColors.ControlLight;

        }


        //закрашивание замкнутой области по щелчку мыши/вывод значения пикселя в Хайнсфилдах при выбранном режиме
        private void pictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            if (Painting || Erasing || aorta) return;

            int x; int y;
            if (!Coloring)
            {
                //x = e.X * 100 / scale.Value;
                //y = e.Y * 100 / scale.Value;
                //double valgray = GetHounsfieldOfPixel(x, y, SliceNum);

                //string t = "x: " + x.ToString() + " y: " + y.ToString() + " H: " + valgray.ToString();
                //MessageBox.Show(t);
                //MessageBox.Show("Чтобы выделить область, активируйте соответствующий режим в панели инструментов.", "Выбор области", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            x = e.X * 100 / scale.Value;
            y = e.Y * 100 / scale.Value;

            int deltaX = Cursor.Position.X - e.X;
            int deltaY = Cursor.Position.Y - e.Y;

            if (Filtered)
            {
                change_to_default_mode();
                DialogResult result = MessageBox.Show("Хотите дополнить область на выбранном слое?\n\nНажмите \"Да\", чтобы дополнить.\nНажмите \"Нет\", чтобы изменить выделение на данном слое.", "Выбор области", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                if (result == DialogResult.Cancel) { return; }

                else if (result == DialogResult.Yes)
                {
                    Cursor = Cursors.WaitCursor;
                    //выделение доп куска области на конкретном слое
                    Bitmap resultBmp = AreaColoring(coloredImageList[SliceNum - borderleft], SliceNum, x, y, deltaX, deltaY);

                    coloredImageList[SliceNum - borderleft] = resultBmp;
                    pictureBox1.Image = coloredImageList[SliceNum - borderleft];

                    findmaxrect();
                    Cursor = Cursors.Arrow;
                }
                else
                {
                    Cursor = Cursors.WaitCursor;
                    //выбор другой области для выделения на конкретном слое
                    Bitmap resultBmp = AreaColoring(imageList[SliceNum], SliceNum, x, y, deltaX, deltaY);

                    coloredImageList[SliceNum - borderleft] = resultBmp;
                    pictureBox1.Image = coloredImageList[SliceNum - borderleft];

                    findmaxrect();
                    Cursor = Cursors.Arrow;
                }
            }
            else
            {
                change_to_default_mode();
                if (points.Count == 0)
                {
                    cursorPosition = new Point { X = Cursor.Position.X, Y = Cursor.Position.Y };
                    for (int i = borderleft; i <= borderright; i++)
                    {
                        points.Add(new Point(x, y));
                        pointBitmap = CombineBitmap(new Bitmap(imageList[i]), drawPoint(points[i - borderleft]));
                        imageListWithPoints.Add(pointBitmap);
                    }
                    brushToolStripButton.Enabled = true;
                    eraserToolStripDropDown.Enabled = true;

                }
                else
                {
                    points[SliceNum - borderleft] = new Point(x, y);
                    pointBitmap = CombineBitmap(new Bitmap(imageList[SliceNum]), drawPoint(points[SliceNum - borderleft]));
                    imageListWithPoints[SliceNum - borderleft] = pointBitmap;
                }

                pictureBox1.Image = imageListWithPoints[SliceNum - borderleft];
                TextImage.Screen filter = new TextImage.Screen(brushBitmap[SliceNum - borderleft]);
                Bitmap tmpBitmap = new Bitmap(imageListWithPoints[SliceNum - borderleft]);
                pictureBox1.Image = filter.Apply(tmpBitmap);

            }
        }

        public Bitmap drawPoint(Point point)
        {
            Bitmap bitmap = new Bitmap(imageList[0].Width, imageList[0].Height);
            for (int i = point.X - 2; i < point.X + 2; i++)
                for (int j = point.Y - 2; j < point.Y + 2; j++)
                    bitmap.SetPixel(i, j, Color.Yellow);

            return bitmap;
        }

        //замена выбранного цвета в изображении
        private Bitmap ColorChange(Bitmap bmp, Color oldColor, Color newColor)
        {
            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    if (bmp.GetPixel(x, y) == oldColor)
                        bmp.SetPixel(x, y, newColor);
                }
            }
            return bmp;
        }

        //поиск прямоугольника для выделенной области на одном слое
        int mtx = 512, mty = 512, mbx = 0, mby = 0;
        public void findrect(Bitmap b)
        {
            int tx = 0, ty = 0, bx = 0, by = 0;
            Bitmap whites = new Bitmap(1, 1);
            for (int i = 0; i < whites.Width; i++)
            {
                for (int j = 0; j < whites.Height; j++)
                {
                    whites.SetPixel(i, j, Color.White);
                }
            }
            Grayscale filter1 = new Grayscale(0.2125, 0.7154, 0.0721);
            Bitmap white = filter1.Apply(whites);
            Color color = white.GetPixel(0, 0);


            for (int i = 0; i < b.Height; i++)
            {
                for (int j = 0; j < b.Width; j++)
                {
                    Color pix = b.GetPixel(i, j);
                    if ((tx == 0) && (pix == color))
                    {
                        tx = i;
                    }
                    if (pix == color)
                    {
                        bx = i;
                    }
                }
            }

            for (int j = 0; j < b.Width; j++)
            {
                for (int i = 0; i < b.Height; i++)
                {
                    Color pix = b.GetPixel(i, j);
                    if ((ty == 0) && (pix == color))
                    {
                        ty = j;
                    }
                    if (pix == color)
                    {
                        by = j;
                    }
                }
            }

            if (tx < mtx)
                mtx = tx;
            if (ty < mty)
                mty = ty;
            if (bx > mbx)
                mbx = bx;
            if (by > mby)
                mby = by;

        }

        //поиск прямоугольника, подходящего для всех слоев
        void findmaxrect()
        {
            mtx = 512; mty = 512; mbx = 0; mby = 0;
            foreach (Bitmap b in maskList)
            {
                findrect(b);
            }
            //вывод координат прямоугольника
            //MessageBox.Show(mtx.ToString() + " " + mty.ToString() + "\n" + mbx.ToString() + " " + mby.ToString());

            if (mbx - mtx > 50 || mby - mty > 50)
            {
                MessageBox.Show("Внимательно проверьте все слои на предмет ошибочного выделения.", "Возможны проблемы", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

        }


        //выделение области
        public Bitmap AreaColoring(Image inputImg, int slice, int x, int y, int deltaX, int deltaY)
        {
            int porog = Convert.ToInt32(GetHounsfieldOfPixel(x, y, slice) - 120);
            Image binarizedImg = Binarization(inputImg, GetHounsfieldOfSlice(slice), porog);

            //erozion
            filterbet = new Bitmap(binarizedImg);
            Grayscale filter1 = new Grayscale(0.2125, 0.7154, 0.0721);
            Bitmap img1 = filter1.Apply(filterbet);
            Bitmap img2 = Eroz(binarizedImg);

            Difference filter = new Difference(img2);
            Bitmap res = filter.Apply(img1);
            GrayscaleToRGB filterrgb = new GrayscaleToRGB();
            Bitmap rgbres = filterrgb.Apply(res);

            //paintzone
            Bitmap coloredAreaImg = paintzone(rgbres, brushBitmap[slice - borderleft], x, y, deltaX, deltaY);

            try
            {
                maskList[slice - borderleft] = coloredAreaImg;
            }
            catch (Exception ex)
            {
                maskList.Add(coloredAreaImg);
            }

            //color change
            Bitmap original = new Bitmap(inputImg);
            Bitmap result = ColorChange(new Bitmap(coloredAreaImg), Color.FromArgb(255, 254, 254, 254), Color.FromArgb(120, 255, 0, 0));
            Bitmap outputImg = CombineBitmap(original, result);

            return outputImg;
        }


        //наложение 2х изображений
        public static Bitmap CombineBitmap(Bitmap picture1, Bitmap picture2)
        {
            List<Bitmap> images = new List<Bitmap>();
            Bitmap finalImage = null;

            try
            {
                int width = picture1.Width;
                int height = picture1.Height;

                picture2.MakeTransparent(Color.Black);

                images.Add(picture1);
                images.Add(picture2);

                finalImage = new Bitmap(width, height);

                using (Graphics g = Graphics.FromImage(finalImage))
                {
                    g.Clear(Color.Transparent);
                    foreach (Bitmap image in images)
                    {
                        g.DrawImage(image, new Rectangle(0, 0, image.Width, image.Height));
                    }
                }

                return finalImage;
            }
            catch (Exception)
            {
                if (finalImage != null) finalImage.Dispose();
                throw;
            }
            finally
            {
                foreach (Bitmap image in images)
                {
                    image.Dispose();
                }
            }
        }

        //создание файла отчета
        public void make_report()
        {
            if (Filtered)
            {
                Cursor = Cursors.WaitCursor;
                string a = "";

                //запись путей к файлам исследования в первую строку (порядок правильный)

                for (int i = borderleft; i <= borderright; i++)
                {
                    a = a + allfiles[i] + ";";
                }
                int lay_num = borderright - borderleft + 1;
                for (int q = 0; q < (mbx - mtx + 1) * (mby - mty + 1) - lay_num - 1; q++)
                    a = a + ";";
                a = a + "\n";
                //запись среднего значения по аорте для нормализации во вторую строку 
                a = a + aorta_value;
                for (int q = 0; q < (mbx - mtx + 1) * (mby - mty + 1) - 1; q++)
                    a = a + ";";
                a = a + "\n";
                //запись координат прямоугольника в третью строку (граница слева,граница справа, верхний левый угол прямоугольника, правый нижний угол прямоугольника)
                a = a + borderleft.ToString() + ";" + borderright.ToString() + ";" + mtx.ToString() + ";" + mty.ToString() + ";" + mbx.ToString() + ";" + mby.ToString();
                for (int q = 0; q < (mbx - mtx + 1) * (mby - mty + 1) - 6; q++)
                    a = a + ";";
                a = a + "\n";


                Bitmap whites = new Bitmap(1, 1);
                for (int i = 0; i < whites.Width; i++)
                {
                    for (int j = 0; j < whites.Height; j++)
                    {
                        whites.SetPixel(i, j, Color.White);
                    }
                }
                Grayscale filter1 = new Grayscale(0.2125, 0.7154, 0.0721);
                Bitmap white = filter1.Apply(whites);
                Color color = white.GetPixel(0, 0);

                for (int i = borderleft; i <= borderright; i++)
                {
                    var dcm = DICOMObject.Read(allfiles[i]);
                    var dicomfile = DicomFile.Open(allfiles[i]);
                    DicomDataset dataset = new DicomDataset();
                    dataset = dicomfile.Dataset.Clone();
                    DicomTranscoder xx = new DicomTranscoder(DicomTransferSyntax.JPEGProcess14SV1, DicomTransferSyntax.ImplicitVRLittleEndian);
                    IByteBuffer newpixelData1 = xx.DecodeFrame(dataset, 0);

                    List<byte> newpixelData = newpixelData1.Data.ToList();
                    List<byte> pixelData = newpixelData;

                    ushort bitsAllocated = (ushort)dcm.FindFirst(TagHelper.BitsAllocated).DData;
                    ushort bitsStored = (ushort)dcm.FindFirst(TagHelper.BitsStored).DData;
                    double intercept = (double)dcm.FindFirst(TagHelper.RescaleIntercept).DData;
                    double slope = (double)dcm.FindFirst(TagHelper.RescaleSlope).DData;
                    ushort pixelRepresentation = (ushort)dcm.FindFirst(TagHelper.PixelRepresentation).DData;
                    //List<byte> pixelData = (List<byte>)dcm.FindFirst(TagHelper.PixelData).DData_;

                    ushort mask = (ushort)(ushort.MaxValue >> (bitsAllocated - bitsStored));
                    double maxval = Math.Pow(2, bitsStored);
                    int[] pix = new int[(mbx - mtx + 1) * (mby - mty + 1)];
                    int k = 0;
                    for (int x = mtx; x <= mbx; x++)
                    {
                        for (int y = mty; y <= mby; y++)
                        {
                            ushort gray = (ushort)((ushort)(pixelData[2 * (y * 512 + x)]) + (ushort)(pixelData[2 * (y * 512 + x) + 1] << 8));
                            double valgray = gray & mask;

                            if (pixelRepresentation == 1)
                            {
                                if (valgray > (maxval / 2))
                                    valgray = (valgray - maxval);

                            }
                            if (maskList[i - borderleft].GetPixel(x, y) == color)
                                pix[k] = (int)(slope * valgray + intercept);
                            else
                                pix[k] = -2048;
                            k++;
                        }
                    }

                    for (int l = 0; l < k - 1; l++)
                    {
                        a = a + pix[l].ToString() + ";";
                    }
                    a = a + pix[k - 1].ToString() + "\n";

                }
                StreamWriter fstream = null;
                FileInfo fstreaminfo = null;
                string fname = new DirectoryInfo(folder).Name; string reportName;
                string path = Application.StartupPath + @"\..\..\..\Отчеты\" + fname + @"\";  //папка Отчеты в папке с проектом
                if (reportNum == 0)
                    reportName = fname + ".csv"; //имя файлика .csv = имя папки с dicom + номер отчета
                else
                    reportName = fname + "_" + reportNum.ToString() + ".csv";

                if (!Directory.Exists(path))    //создаем папку Отчеты, если ее еще не было
                {
                    Directory.CreateDirectory(path);
                }
                path += reportName;
                fstream = new StreamWriter(path, false, Encoding.Default); fstreaminfo = new FileInfo(path);
                fstream.WriteLine($"{a}");
                fstream.Close();
                Cursor = Cursors.Arrow;
                MessageBox.Show("Отчёт успешно сохранен в\n" + fstreaminfo.DirectoryName.ToString() + @"\" + reportName, "Отчет сохранен!");
                reportNum++;
                openToolStripButton.Enabled = true;
            }
            else
            {
                MessageBox.Show("Cначала выделите область. Чтобы выделить область, активируйте соответствующий режим в панели инструментов.", "Сохранение не удалось", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

        }

        //показать свойства дайком файла
        private void PropertyStripLabel_Click(object sender, EventArgs e)
        {

            var dcm = DICOMObject.Read(allfiles[SliceNum]);

            string index = "";
            if (dcm.FindFirst(TagHelper.InstanceNumber) != null)
                index = dcm.FindFirst(TagHelper.InstanceNumber).DData.ToString();
            string transfer = "";
            if (dcm.FindFirst(TagHelper.Transfer​Syntax​UID) != null)
                transfer = dcm.FindFirst(TagHelper.Transfer​Syntax​UID).DData.ToString();
            string option = "";
            if (dcm.FindFirst(TagHelper.ScanOptions) != null)
                option = dcm.FindFirst(TagHelper.ScanOptions).DData.ToString();
            string thickness = "";
            if (dcm.FindFirst(TagHelper.Slice​Thickness) != null)
                thickness = dcm.FindFirst(TagHelper.Slice​Thickness).DData.ToString();
            string photo = "";
            if (dcm.FindFirst(TagHelper.PhotometricInterpretation) != null)
                photo = dcm.FindFirst(TagHelper.PhotometricInterpretation).DData.ToString();
            string intercept = "";
            if (dcm.FindFirst(TagHelper.RescaleIntercept) != null)
                intercept = dcm.FindFirst(TagHelper.RescaleIntercept).DData.ToString();
            string slope = "";
            if (dcm.FindFirst(TagHelper.RescaleSlope) != null)
                slope = dcm.FindFirst(TagHelper.RescaleSlope).DData.ToString();
            string rows = "";
            if (dcm.FindFirst(TagHelper.Rows) != null)
                rows = dcm.FindFirst(TagHelper.Rows).DData.ToString();
            string columns = "";
            if (dcm.FindFirst(TagHelper.Columns) != null)
                columns = dcm.FindFirst(TagHelper.Columns).DData.ToString();

            string pixelRepresentation = "";
            if (dcm.FindFirst(TagHelper.PixelRepresentation) != null)
                pixelRepresentation = dcm.FindFirst(TagHelper.PixelRepresentation).DData.ToString();

            string window = "";
            if (dcm.FindFirst(TagHelper.WindowWidth) != null)
                window = dcm.FindFirst(TagHelper.WindowWidth).DData.ToString();
            string level = "";
            if (dcm.FindFirst(TagHelper.WindowCenter) != null)
                level = dcm.FindFirst(TagHelper.WindowCenter).DData.ToString();

            List<byte> pixelData = (List<byte>)dcm.FindFirst(TagHelper.PixelData).DData_;


            var dicomfile = DicomFile.Open(allfiles[SliceNum]);
            DicomDataset dataset = new DicomDataset();
            dataset = dicomfile.Dataset.Clone();
            DicomTranscoder x = new DicomTranscoder(DicomTransferSyntax.JPEGProcess14SV1, DicomTransferSyntax.ImplicitVRLittleEndian);
            IByteBuffer newpixelData1 = x.DecodeFrame(dataset, 0);
            List<byte> newpixelData = newpixelData1.Data.ToList();

            string pixels = string.Join(" ", pixelData.Take(100).ToArray());
            string newpixels = string.Join(" ", newpixelData.Take(100).ToArray());

            MessageBox.Show("InstanceNumber: " + index + "\n" +
                            "Transfer​Syntax​UID: " + transfer + "\n" +
                            "Scan​Options: " + option + "\n" +
                            "Slice​Thickness: " + thickness + "\n" +
                            "PhotometricInterpretation: " + photo + "\n" +
                            "RescaleIntercept: " + intercept + "\n" +
                            "RescaleSlope: " + slope + "\n" +
                            "Rows: " + rows + "\n" +
                            "Columns: " + columns + "\n" +
                            "PixelRepresentation: " + pixelRepresentation + "\n" +
                            "WindowWidth: " + window + "\n" +
                            "WindowCenter: " + level + "\n" +
                            "PixelData: " + pixels + "\n" +
                            "PixelData after decoding: " + newpixels,
                "Cвойства", MessageBoxButtons.OK);
        }

        //центрирование изображения в окне
        private void center()
        {
            int x, y;

            if (((panel1.Width - pictureBox1.Width) / 2) <= 0)
                x = 0;
            else
                x = (panel1.Width - pictureBox1.Width) / 2;

            if (((panel1.Height - pictureBox1.Height) / 2) <= 0)
                y = 0;
            else
                y = (panel1.Height - pictureBox1.Height) / 2;

            pictureBox1.Location = new Point(x, y);

            //подгон скроллов под центр
            panel1.VerticalScroll.Value = (int)((panel1.VerticalScroll.Maximum) / 4.0);
            panel1.HorizontalScroll.Value = (int)((panel1.HorizontalScroll.Maximum) / 3.5);
        }


        //центрирование изображения в окне при изменении размера окна
        private void Form1_ClientSizeChanged(object sender, EventArgs e)
        {
            center();
            toolStripStatusLabel1.Margin = new Padding(this.Width / 3 - 10, 4, 0, 2);
            toolStripStatusLabel2.Margin = new Padding(this.Width / 3 - 10, 4, 0, 2);

        }


        //сброс изменений
        private void restartToolStripButton_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Хотите оставить границы слоёв?", "Сброс", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            

            if (result == DialogResult.Cancel)
            {
                return;
            }

            else if (result == DialogResult.No)
            {
                if (dif_window.Visible == true) { dif_window.Close(); }
                change_to_default_mode();

                pointToolStripButton1.Enabled = true;
                aortaToolStripButton.Enabled = false;
                openToolStripButton.Enabled = false;
                brushBitmap.Clear();

                aortaToolStripButton.BackColor = SystemColors.ControlLight; aorta = false;
                pointToolStripButton1.BackColor = SystemColors.ControlLight; Coloring = false;
                brushToolStripButton.BackColor = SystemColors.ControlLight; Painting = false;
                eraserToolStripDropDown.BackColor = SystemColors.ControlLight; Erasing = false;

                //сброс выбранных границ
                changeborderleftToolStripButton.Enabled = true;
                changeborderrightToolStripButton.Enabled = true;
                openToolStripButton.Enabled = true;
                aorta_value = 0;

                borderleft = 0;
                hScrollBar1.Minimum = borderleft;
                borderrleftToolStrip.Text = (borderleft + 1).ToString();

                borderright = imageList.Count() - 1;
                hScrollBar1.Maximum = borderright;
                borderrrightToolStrip.Text = (borderright + 1).ToString();

                constborderleft = borderleft;
                constborderright = borderright;
                SliceNum = 0;
                hScrollBar1.Value = 0;
                hscrollvalueStatusLabel.Text = "0/" + (imageList.Count - 1).ToString(); ;
                savingToolStripButton.Enabled = true;
                scale.Value = 100;
                scaleStatusLabel.Text = "Масштаб: " + "100%";
                pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
                pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                pointToolStripButton1.Enabled = false;
                aortaToolStripButton.Enabled = true;
                endToolStripButton.Enabled = false;

                brushBitmap.Add(new Bitmap(512, 512));
                using (Graphics gfx = Graphics.FromImage(brushBitmap[0]))
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(0, 0, 0)))
                {
                    gfx.FillRectangle(brush, 0, 0, 512, 512);
                }

                bigpic();
            }

            else if (result == DialogResult.Yes)
            {
                if (dif_window.Visible == true) { dif_window.Close(); }
                change_to_default_mode();

                pointToolStripButton1.Enabled = true;
                aortaToolStripButton.Enabled = false;
                openToolStripButton.Enabled = false;
                brushBitmap.Clear();

                aortaToolStripButton.BackColor = SystemColors.ControlLight; aorta = false;
                pointToolStripButton1.BackColor = SystemColors.ControlLight; Coloring = false;
                brushToolStripButton.BackColor = SystemColors.ControlLight; Painting = false;
                eraserToolStripDropDown.BackColor = SystemColors.ControlLight; Erasing = false;

                for (int i = 0; i <= constborderright - constborderleft; i++)
                {
                    brushBitmap.Add(new Bitmap(512, 512));
                    using (Graphics gfx = Graphics.FromImage(brushBitmap[i]))
                    using (SolidBrush brush = new SolidBrush(Color.FromArgb(0, 0, 0)))
                    {
                        gfx.FillRectangle(brush, 0, 0, 512, 512);
                    }
                }
            }

            coloringToolStripButton.BackColor = SystemColors.ControlLight;
            pointToolStripButton1.BackColor = SystemColors.ControlLight;
            brushToolStripButton.BackColor = SystemColors.ControlLight;
            eraserToolStripDropDown.BackColor = SystemColors.ControlLight;
            coloredImageList = new List<Image>();
            imageListWithPoints = new List<Image>();
            maskList = new List<Bitmap>();
            points = new List<Point>();
            pictureBox1.Image = imageList[SliceNum];
            Filtered = false;
            Coloring = false;
            Painting = false;
            Erasing = false;
            brushToolStripButton.Enabled = false;
            eraserToolStripDropDown.Enabled = false;
            coloringToolStripButton.Enabled = false;

            aortaBitmap = new Bitmap(512, 512);
            using (Graphics gfx2 = Graphics.FromImage(brushBitmap[0]))
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(0, 0, 0)))
            {
                gfx2.FillRectangle(brush, 0, 0, 512, 512);
            }
        }


        //Выбор границ
        private void savingToolStripButton_Click(object sender, EventArgs e)
        {
            if (aorta_value == 0)
            {
                MessageBox.Show("Сначала вам нужно выделить аорту!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            change_to_default_mode();

            changeborderleftToolStripButton.Enabled = false;
            changeborderrightToolStripButton.Enabled = false;
            savingToolStripButton.Enabled = false;
            endToolStripButton.Enabled = true;
            pointToolStripButton1.Enabled = true;
            openToolStripButton.Enabled = false;
            aortaToolStripButton.Enabled = false;

            constborderleft = borderleft;
            constborderright = borderright;
            hScrollBar1.Minimum = constborderleft;
            hScrollBar1.Maximum = constborderright;
            SliceNum = constborderright;
            hScrollBar1.Value = SliceNum;

            pictureBox1.Image = imageList[SliceNum];
            dif_window.change_image(SliceNum);

            hscrollvalueStatusLabel.Text = (SliceNum + 1).ToString() + "/" + (imageList.Count).ToString();
            brushBitmap.Clear();

            for (int i = 0; i <= constborderright - constborderleft; i++)
            {
                brushBitmap.Add(new Bitmap(512, 512));
                using (Graphics gfx = Graphics.FromImage(brushBitmap[i]))
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(0, 0, 0)))
                {
                    gfx.FillRectangle(brush, 0, 0, 512, 512);
                }
            }

            MessageBox.Show("Границы успешно выбраны.", "Границы выбраны!");
        }

        //перезапуск приложения (в любой момент)
        private void rebootStripButton_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Вы хотите перезапустить приложение?", "Перезапуск", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result == DialogResult.No) { return; }

            else if (result == DialogResult.Yes)
            {
                Application.Restart();
            }
        }

        // масштабирование скроллбаром
        private void scale_Scroll(object sender, EventArgs e) //масштабирование
        {
            ToggleAntiFlicker(true);
            scroll();
            ToggleAntiFlicker(false);
            center();
        }



        private string HUToTissue(double valgray)
        {
            switch (valgray)
            {
                case >= -5 and <= 5:
                    return "вода";
                case -1000:
                    return "воздух";
                case >= -900 and <= -500:
                    return "легочная";
                case >= 16 and <= 20:
                    return "транссудат";
                case >= 40 and < 50:
                    return "лимфа";
                case >= 50 and <= 60:
                    return "кровь";
                case >= 30 and <= 230:
                    return "костная губчатая";
                case >= 250:
                    return "костная компактная";
                default:
                    return "";
            }
        }


        //вкл/выкл режима выделения области
        private void coloringToolStripButton_Click(object sender, EventArgs e)
        {
            change_to_default_mode();

            DialogResult result = MessageBox.Show("Вы уверены в выборе точек на всех слоях?\nПри нажатии \"да\" начнется закрашивание областей к которым принадлежат точки.", "Начать закрашивание?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.No) { return; }

            else if (result == DialogResult.Yes)
            {
                if (points.Count == 0)
                {
                    MessageBox.Show("Сперва укажите точку внутри области интереса!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                brushToolStripButton.BackColor = SystemColors.ControlLight;
                brushToolStripButton.Enabled = false;

                eraserToolStripDropDown.BackColor = SystemColors.ControlLight;
                eraserToolStripDropDown.Enabled = false;

                Cursor = Cursors.WaitCursor;
                pointToolStripButton1.Enabled = false;
                coloringToolStripButton.BackColor = SystemColors.ControlDark;
                pointToolStripButton1.BackColor = SystemColors.ControlLight;

                //выделение области на выбранном диапазоне слоев

                coloredImageList = new List<Image>();
                mtx = 512; mty = 512; mbx = 0; mby = 0;

                for (int i = borderleft; i <= borderright; i++)
                {
                    int x = points[i - borderleft].X;
                    int y = points[i - borderleft].Y;
                    float ex = x / 100.0f * scale.Value;
                    float ey = y / 100.0f * scale.Value;
                    int deltaX = (int)(cursorPosition.X - ex);
                    int deltaY = (int)(cursorPosition.Y - ey);

                    Bitmap resultBmp = AreaColoring(imageList[i], i, x, y, deltaX, deltaY);

                    coloredImageList.Add(resultBmp);
                }
                Filtered = true;
                pictureBox1.Image = coloredImageList[SliceNum - borderleft];

                findmaxrect();
                Cursor = Cursors.Arrow;
            }
        }


        //режим выделения аорты
        private void aortaToolStripButton_Click(object sender, EventArgs e)
        {
            aortaToolStripButton.BackColor = (aortaToolStripButton.BackColor == SystemColors.ControlLight) ? SystemColors.ControlDark : SystemColors.ControlLight;
            pointToolStripButton1.BackColor = SystemColors.ControlLight; Coloring = false;
            brushToolStripButton.BackColor = SystemColors.ControlLight; Painting = false;
            eraserToolStripDropDown.BackColor = SystemColors.ControlLight; Erasing = false;

            change_to_default_mode();
        }


        //режим выбор точки интереса
        private void pointToolStripButton1_Click(object sender, EventArgs e)
        {
            openToolStripButton.Enabled = false;
            coloringToolStripButton.Enabled = true;
            Coloring = !Coloring;
            pointToolStripButton1.BackColor = (pointToolStripButton1.BackColor == SystemColors.ControlLight) ? SystemColors.ControlDark : SystemColors.ControlLight;
            aortaToolStripButton.BackColor = SystemColors.ControlLight; aorta = false;
            brushToolStripButton.BackColor = SystemColors.ControlLight; Painting = false;
            eraserToolStripDropDown.BackColor = SystemColors.ControlLight; Erasing = false;

            change_to_default_mode();
        }

        //режим кисти
        private void brushToolStripButton_Enable()
        {
            eraserToolStripDropDown.BackColor = (brushToolStripButton.BackColor == SystemColors.ControlDark) ? SystemColors.ControlLight : eraserToolStripDropDown.BackColor;
            aortaToolStripButton.BackColor = SystemColors.ControlLight; aorta = false;
            pointToolStripButton1.BackColor = SystemColors.ControlLight; Coloring = false;
        }

        //режим ластика
        private void eraserToolStripButton_Enable()
        {
            brushToolStripButton.BackColor = (eraserToolStripDropDown.BackColor == SystemColors.ControlDark) ? SystemColors.ControlLight : brushToolStripButton.BackColor;
            aortaToolStripButton.BackColor = SystemColors.ControlLight; aorta = false;
            pointToolStripButton1.BackColor = SystemColors.ControlLight; Coloring = false;

        }


        private TextImage.Screen BrushFilter(Bitmap brushBitmap, Color color, MouseEventArgs e)
        {
            Pen p = new Pen(color, 5);
            using (Graphics g = Graphics.FromImage(brushBitmap))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.DrawLine(p, new Point(initX ?? e.X * 100 / scale.Value, initY ?? e.Y * 100 / scale.Value), new Point(e.X * 100 / scale.Value, e.Y * 100 / scale.Value));
                initX = e.X * 100 / scale.Value;
                initY = e.Y * 100 / scale.Value;
            }
            TextImage.Screen filter = new TextImage.Screen(brushBitmap);
            return filter;
        }



        //работа при движении мышью (кисть, аорта, показ хаунсфилдов)
        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            Color brushColor = Color.Black;
            if (Painting)
                brushColor = Color.FromArgb(150, 0, 255, 0);
            else if (Erasing)
                brushColor = Color.FromArgb(0, 0, 0);

            if (Painting || Erasing)
            {
                change_to_default_mode();
                Bitmap brushBitmaptmp = new Bitmap(512, 512);

                TextImage.Screen filt = BrushFilter(brushBitmaptmp, brushColor, e);
                TextImage.Screen filter = BrushFilter(brushBitmap[SliceNum - borderleft], brushColor, e);
                using (Graphics grfx = Graphics.FromImage(brushBitmap[SliceNum - borderleft]))
                {
                    grfx.DrawImage(brushBitmaptmp, 0, 0);
                }

                if (!bOneLayerBrush)
                {
                    for (int i = 0; i <= borderright - borderleft; i++)
                       if (i != SliceNum - borderleft)
                       {
                            // brushBitmap[i] = (Bitmap)brushBitmap[SliceNum - borderleft].Clone();
                            using (Graphics grfx = Graphics.FromImage(brushBitmap[i]))
                            {
                                grfx.DrawImage(brushBitmaptmp, 0, 0);
                            }
                       }
                }
                Bitmap tmpBitmap = new Bitmap(imageListWithPoints[SliceNum - borderleft]);
                pictureBox1.Image = filter.Apply(tmpBitmap);
            }

            if (aorta)
            {
                change_to_default_mode();
                fx = e.X * 100 / scale.Value;
                fy = e.Y * 100 / scale.Value;
                sx = e.X * 100 / scale.Value - cx;
                sy = e.Y * 100 / scale.Value - cy;
                SolidBrush p = new SolidBrush(Color.FromArgb(255, 255, 0, 255));
                aortaBitmap = new Bitmap(512, 512);
                using (Graphics gfx2 = Graphics.FromImage(aortaBitmap))
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(0, 0, 0)))
                {
                    gfx2.FillRectangle(brush, 0, 0, 512, 512);
                }
                using (Graphics g = Graphics.FromImage(aortaBitmap))
                {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.FillEllipse(p, cx, cy, sx, sy);

                }
                TextImage.Screen filter = new TextImage.Screen(aortaBitmap);
                Bitmap tmpBitmap = new Bitmap(1, 1);
                if (points.Count == 0)
                    tmpBitmap = new Bitmap(imageList[SliceNum]);
                else
                    tmpBitmap = new Bitmap(imageListWithPoints[SliceNum - borderleft]);
                pictureBox1.Image = filter.Apply(tmpBitmap);
            }

            if (hounsfield)
            {
                int x = e.X * 100 / scale.Value;
                int y = e.Y * 100 / scale.Value;
                double valgray = GetHounsfieldOfPixel(x, y, SliceNum);

                toolTip1.SetToolTip(pictureBox1, valgray.ToString());
                /* string tissue = HUToTissue(valgray);
                 toolStripStatusLabel2.Text = "Ткань: " + tissue;*/
            }
            else
            {
                toolTip1.SetToolTip(pictureBox1, "");
                //toolStripStatusLabel2.Text = "";

            }

            if (pictureBox1.Image != null && Scrolling_m)
            {
                int diff_y = e.Y - y_m_past;
                if (diff_y < 0 && SliceNum - 1 >= constborderleft)
                {
                    SliceNum -= 1;
                }
                else if (diff_y < 0 && SliceNum - 1 <= constborderleft)
                {
                    SliceNum = constborderright;
                }
                else if (diff_y > 0 && SliceNum + 1 <= constborderright)
                {
                    SliceNum += 1;
                }
                else if (diff_y > 0 && SliceNum + 1 >= constborderright)
                {
                    SliceNum = constborderleft;
                }
                y_m_past = e.Y;

                //if (current_view_mode == 3)
                //{
                    dif_window.change_image(SliceNum);
                //}
                if (current_view_mode == 2)
                {
                    pictureBox1.Image = imageList2[SliceNum];
                }
                else if (!Filtered)
                {
                    pictureBox1.Image = imageList[SliceNum];
                    if (points.Count != 0)
                    {
                        TextImage.Screen filter = new TextImage.Screen(brushBitmap[SliceNum - borderleft]);
                        Bitmap tmpBitmap = new Bitmap(imageListWithPoints[SliceNum - borderleft]);
                        pictureBox1.Image = filter.Apply(tmpBitmap);
                    }
                }
                else
                {
                    pictureBox1.Image = coloredImageList[SliceNum - borderleft];
                }
                panel1.Refresh();
                hScrollBar1.Value = SliceNum;
                // hscrollvalueStatusLabel.Text = SliceNum.ToString() + "/" + (imageList.Count - 1).ToString();
                hscrollvalueStatusLabel.Text = (SliceNum + 1).ToString() + "/" + (imageList.Count).ToString();
            }

        }



        //действия при первом зажатии кнопки мыши (кисть,аорта)
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {

            MouseEventArgs mouse = (MouseEventArgs)e;

            if (brushToolStripButton.BackColor == SystemColors.ControlDark && mouse.Button == MouseButtons.Left)
            {
                change_to_default_mode();
                Painting = true;

            }
            else if (eraserToolStripDropDown.BackColor == SystemColors.ControlDark && mouse.Button == MouseButtons.Left)
            {
                change_to_default_mode();
                Erasing = true;
            }
            else if (aortaToolStripButton.BackColor == SystemColors.ControlDark && mouse.Button == MouseButtons.Left)
            {
                change_to_default_mode();
                aorta = true;
                cx = e.X * 100 / scale.Value; ;
                cy = e.Y * 100 / scale.Value; ;
                aortaBitmap = new Bitmap(512, 512);
                using (Graphics gfx2 = Graphics.FromImage(aortaBitmap))
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(0, 0, 0)))
                {
                    gfx2.FillRectangle(brush, 0, 0, 512, 512);
                }
            }
            else 
            {
                Scrolling_m = true;
            }

        }




        //действия при отпускании кнопки мыши (кисть,аорта)
        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (Painting || Erasing)
            {
                change_to_default_mode();
                if (Painting)
                    Painting = false;
                else if (Erasing)
                    Erasing = false;

                initX = null;
                initY = null;

                TextImage.Screen filter = new TextImage.Screen(brushBitmap[SliceNum - borderleft]);
                Bitmap tmpBitmap = new Bitmap(imageListWithPoints[SliceNum - borderleft]);
                pictureBox1.Image = filter.Apply(tmpBitmap);
            }

            if (aorta)
            {
                change_to_default_mode();
                aorta = false;
                Cursor = Cursors.WaitCursor;
                aorta_value = count_aorta_value();
                DialogResult result; string show_message;
                Cursor = Cursors.Default;
                if (aorta_value < 15 || aorta_value > 55)
                {
                    show_message = "Среднее значение по аорте на всех слоях:: " + Math.Round(aorta_value, 2).ToString() + "\nВероятно вы ошиблись при выделении! Вы уверены в своем выборе?\nПри нажатии \"да\" вы больше не сможете изменять эту область.\n\nПодсказка: Выбирайте небольшую область в центре аорты, не старайтесь выделить ее полностью.";
                    result = MessageBox.Show(show_message, "Возможна ошибка", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
                }
                else
                {
                    show_message = "Среднее значение по аорте на всех слоях: " + Math.Round(aorta_value, 2).ToString() + "\nВы уверены в выборе области аорты?\nПри нажатии \"да\" вы больше не сможете изменять эту область.\n\nПодсказка: Выбирайте небольшую область в центре аорты, не старайтесь выделить ее полностью.";
                    result = MessageBox.Show(show_message, "Вы уверены?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                }

                if (result == DialogResult.No)
                {
                    aorta_value = 0;
                }

                if (result == DialogResult.Yes)
                {
                    aortaToolStripButton.BackColor = SystemColors.ControlLight;
                    aortaToolStripButton.Enabled = false;
                }

                pictureBox1.Image = imageList[SliceNum];

            }

            if (Scrolling_m)
            {
                Scrolling_m = false;
            }



        }


        //ТЕКУЩИЙ ВАРИАНТ: расчет среднего значения по аорте (по интервалу (x-5,x+5))
        private double count_aorta_value()
        {
            int sum_aorta = 0;
            int count_aorta = 0;

            int Left_b = SliceNum - 5;
            if (Left_b < 0) { Left_b = 0; }
            int Right_b = SliceNum + 5;
            if (Right_b > constborderright) { Right_b = constborderright; }
            Color check_color = Color.FromArgb(255, 255, 0, 255);
            if (allfiles != null)
            {
                for (int i = Left_b; i <= Right_b; i++)
                {
                    var dcm = DICOMObject.Read(allfiles[i]);

                    var dicomfile = DicomFile.Open(allfiles[i]);
                    DicomDataset dataset = new DicomDataset();
                    dataset = dicomfile.Dataset.Clone();
                    DicomTranscoder xx = new DicomTranscoder(DicomTransferSyntax.JPEGProcess14SV1, DicomTransferSyntax.ImplicitVRLittleEndian);
                    IByteBuffer newpixelData1 = xx.DecodeFrame(dataset, 0);

                    List<byte> newpixelData = newpixelData1.Data.ToList();
                    List<byte> pixelData = newpixelData;

                    ushort bitsAllocated = (ushort)dcm.FindFirst(TagHelper.BitsAllocated).DData;
                    ushort bitsStored = (ushort)dcm.FindFirst(TagHelper.BitsStored).DData;
                    double intercept = (double)dcm.FindFirst(TagHelper.RescaleIntercept).DData;
                    double slope = (double)dcm.FindFirst(TagHelper.RescaleSlope).DData;
                    ushort pixelRepresentation = (ushort)dcm.FindFirst(TagHelper.PixelRepresentation).DData;

                    ushort mask = (ushort)(ushort.MaxValue >> (bitsAllocated - bitsStored));
                    double maxval = Math.Pow(2, bitsStored);
                    int[,] pix = new int[512, 512];
                    for (int x = 0; x < 512; x++)
                    {
                        for (int y = 0; y < 512; y++)
                        {
                            Color pix_color = aortaBitmap.GetPixel(x, y);
                            if (pix_color == check_color)
                            {
                                ushort gray;
                                if ((2 * (y * 512 + x) + 1) <= 524288)
                                    gray = (ushort)((ushort)(pixelData[2 * (y * 512 + x)]) + (ushort)(pixelData[2 * (y * 512 + x) + 1] << 8));
                                else
                                    gray = (ushort)((ushort)(pixelData[524286]) + (ushort)(pixelData[524287] << 8));
                                double valgray = gray & mask;

                                if (pixelRepresentation == 1)
                                {
                                    if (valgray > (maxval / 2))
                                        valgray = (valgray - maxval);

                                }
                                sum_aorta = sum_aorta + (int)(slope * valgray + intercept);
                                count_aorta++;
                            }

                        }
                    }
                }

                return (sum_aorta * 1.0 / count_aorta);
            }
            else
                return 0;
        }
        //расчет среднего значения по аорте: по одному слою, сейчас не используется
        private double count_aorta_value2(int slice)
        {
            int sum_aorta = 0;
            int count_aorta = 0;

            Color check_color = Color.FromArgb(255, 255, 0, 255);
            if (allfiles != null)
            {
                var dcm = DICOMObject.Read(allfiles[slice]);

                var dicomfile = DicomFile.Open(allfiles[slice]);
                DicomDataset dataset = new DicomDataset();
                dataset = dicomfile.Dataset.Clone();
                DicomTranscoder xx = new DicomTranscoder(DicomTransferSyntax.JPEGProcess14SV1, DicomTransferSyntax.ImplicitVRLittleEndian);
                IByteBuffer newpixelData1 = xx.DecodeFrame(dataset, 0);

                List<byte> newpixelData = newpixelData1.Data.ToList();
                List<byte> pixelData = newpixelData;

                ushort bitsAllocated = (ushort)dcm.FindFirst(TagHelper.BitsAllocated).DData;
                ushort bitsStored = (ushort)dcm.FindFirst(TagHelper.BitsStored).DData;
                double intercept = (double)dcm.FindFirst(TagHelper.RescaleIntercept).DData;
                double slope = (double)dcm.FindFirst(TagHelper.RescaleSlope).DData;
                ushort pixelRepresentation = (ushort)dcm.FindFirst(TagHelper.PixelRepresentation).DData;

                ushort mask = (ushort)(ushort.MaxValue >> (bitsAllocated - bitsStored));
                double maxval = Math.Pow(2, bitsStored);
                int[,] pix = new int[512, 512];
                for (int x = 0; x < 512; x++)
                {
                    for (int y = 0; y < 512; y++)
                    {
                        Color pix_color = aortaBitmap.GetPixel(x, y);
                        if (pix_color == check_color)
                        {
                            ushort gray;
                            if ((2 * (y * 512 + x) + 1) <= 524288)
                                gray = (ushort)((ushort)(pixelData[2 * (y * 512 + x)]) + (ushort)(pixelData[2 * (y * 512 + x) + 1] << 8));
                            else
                                gray = (ushort)((ushort)(pixelData[524286]) + (ushort)(pixelData[524287] << 8));
                            double valgray = gray & mask;

                            if (pixelRepresentation == 1)
                            {
                                if (valgray > (maxval / 2))
                                    valgray = (valgray - maxval);

                            }
                            sum_aorta = sum_aorta + (int)(slope * valgray + intercept);
                            count_aorta++;
                        }

                    }
                }
                return (sum_aorta * 1.0 / count_aorta);
            }
            else
                return 0;

        }


        //расчет среднего значения по аорте: для проверок, сейчас не используется
        private void count_aorta_value3()
        {
            int sum_aorta = 0;
            int count_aorta = 0;
            string show = "";
            Color check_color = Color.FromArgb(255, 255, 0, 255);
            if (allfiles != null)
            {
                for (int i = borderleft; i <= borderright; i++)
                {
                    var dcm = DICOMObject.Read(allfiles[i]);

                    var dicomfile = DicomFile.Open(allfiles[i]);
                    DicomDataset dataset = new DicomDataset();
                    dataset = dicomfile.Dataset.Clone();
                    DicomTranscoder xx = new DicomTranscoder(DicomTransferSyntax.JPEGProcess14SV1, DicomTransferSyntax.ImplicitVRLittleEndian);
                    IByteBuffer newpixelData1 = xx.DecodeFrame(dataset, 0);

                    List<byte> newpixelData = newpixelData1.Data.ToList();
                    List<byte> pixelData = newpixelData;

                    ushort bitsAllocated = (ushort)dcm.FindFirst(TagHelper.BitsAllocated).DData;
                    ushort bitsStored = (ushort)dcm.FindFirst(TagHelper.BitsStored).DData;
                    double intercept = (double)dcm.FindFirst(TagHelper.RescaleIntercept).DData;
                    double slope = (double)dcm.FindFirst(TagHelper.RescaleSlope).DData;
                    ushort pixelRepresentation = (ushort)dcm.FindFirst(TagHelper.PixelRepresentation).DData;

                    ushort mask = (ushort)(ushort.MaxValue >> (bitsAllocated - bitsStored));
                    double maxval = Math.Pow(2, bitsStored);
                    int[,] pix = new int[512, 512];
                    for (int x = 0; x < 512; x++)
                    {
                        for (int y = 0; y < 512; y++)
                        {
                            Color pix_color = aortaBitmap.GetPixel(x, y);
                            if (pix_color == check_color)
                            {
                                ushort gray;
                                if ((2 * (y * 512 + x) + 1) <= 524288)
                                    gray = (ushort)((ushort)(pixelData[2 * (y * 512 + x)]) + (ushort)(pixelData[2 * (y * 512 + x) + 1] << 8));
                                else
                                    gray = (ushort)((ushort)(pixelData[524286]) + (ushort)(pixelData[524287] << 8));
                                double valgray = gray & mask;

                                if (pixelRepresentation == 1)
                                {
                                    if (valgray > (maxval / 2))
                                        valgray = (valgray - maxval);

                                }
                                sum_aorta = sum_aorta + (int)(slope * valgray + intercept);
                                count_aorta++;
                            }
                        }
                    }
                    show = show + "Номер слоя: " + i.ToString() + " Cреднее: " + (Math.Round(sum_aorta * 1.0 / count_aorta, 2).ToString()) + "\n";
                    sum_aorta = 0;
                    count_aorta = 0;
                }
                MessageBox.Show(show);
            }
            else { }
        }

        private void наОдномСлоеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bOneLayerBrush = true;
            brushToolStripButton.BackColor = SystemColors.ControlDark;
            brushToolStripButton_Enable();

            change_to_default_mode();
        }

        private void наВсехСлояхToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bOneLayerBrush = false;
            brushToolStripButton.BackColor = SystemColors.ControlDark;
            brushToolStripButton_Enable();

            change_to_default_mode();
        }

        private void наОдномСлоеEraserToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bOneLayerBrush = true;
            eraserToolStripDropDown.BackColor = SystemColors.ControlDark;
            eraserToolStripButton_Enable();

            change_to_default_mode();
        }

        private void наВсехСлояхEraserToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bOneLayerBrush = false;
            eraserToolStripDropDown.BackColor = SystemColors.ControlDark;
            eraserToolStripButton_Enable();

            change_to_default_mode();
        }

        //прокрутка зажатием кнопки мыши и тасканием ее вверх-вниз
        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            Scrolling_m = true;
            y_m_past = e.Y;
        }

        //прокрутка зажатием кнопки мыши и тасканием ее вверх-вниз
        private void panel1_MouseMove(object sender, MouseEventArgs e)
        {
            if (pictureBox1.Image != null && Scrolling_m)
                {
                int diff_y = e.Y - y_m_past;
                if (diff_y < 0 && SliceNum - 1 >= constborderleft)
                {
                    SliceNum -= 1;
                }
                else if (diff_y < 0 && SliceNum - 1 <= constborderleft)
                {
                    SliceNum = constborderright;
                }
                else if (diff_y > 0 && SliceNum + 1 <= constborderright)
                {
                    SliceNum += 1;
                }
                else if (diff_y > 0 && SliceNum + 1 >= constborderright)
                {
                    SliceNum = constborderleft;
                }
                y_m_past = e.Y;

                
                //if (current_view_mode == 3)
                //{
                    dif_window.change_image(SliceNum);
                //}
                
                if (current_view_mode == 2)
                {
                    pictureBox1.Image = imageList2[SliceNum];
                }
                else if (!Filtered)
                {
                    pictureBox1.Image = imageList[SliceNum];
                    if (points.Count != 0)
                    {
                        TextImage.Screen filter = new TextImage.Screen(brushBitmap[SliceNum - borderleft]);
                        Bitmap tmpBitmap = new Bitmap(imageListWithPoints[SliceNum - borderleft]);
                        pictureBox1.Image = filter.Apply(tmpBitmap);
                    }
                }
                else
                {
                    pictureBox1.Image = coloredImageList[SliceNum - borderleft];
                }
                hScrollBar1.Value = SliceNum;
                panel1.Refresh();
                // hscrollvalueStatusLabel.Text = SliceNum.ToString() + "/" + (imageList.Count - 1).ToString();
                hscrollvalueStatusLabel.Text = (SliceNum + 1).ToString() + "/" + (imageList.Count).ToString();
            }
            
        }

        //прокрутка зажатием кнопки мыши и тасканием ее вверх-вниз
        private void panel1_MouseUp(object sender, MouseEventArgs e)
        {
            Scrolling_m = false;
            
        }

        //смена режима просмотра по нажатию на клавиши: 1 - дефолт, 2 - режим "легочный"
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            //нажатие на клавишу 1
            if (e.KeyCode == Keys.D1 && pictureBox1.Image != null)
            {
                change_to_default_mode();
                if (dif_window.Visible == true) { dif_window.Close(); }
            }

            //нажатие на клавишу 2
            if (e.KeyCode == Keys.D2 && pictureBox1.Image != null)
            {
                current_view_mode = 2;
                if (dif_window.Visible == true) { dif_window.Close(); }
                pictureBox1.Image = imageList2[SliceNum];
            }


            if (e.KeyCode == Keys.D3 && pictureBox1.Image != null && dif_window.Visible == false)
            {
                current_view_mode = 3;
                dif_window = new Form3(imageList2);
                dif_window.Show();
                dif_window.change_image(SliceNum);
                dif_window.change_scale(scalle);
            }
            

        }

        private void change_to_default_mode()
        {
            current_view_mode = 1;
            if (!Filtered)
            {
                pictureBox1.Image = imageList[SliceNum];
                if (points.Count != 0)
                {
                    TextImage.Screen filter = new TextImage.Screen(brushBitmap[SliceNum - borderleft]);
                    Bitmap tmpBitmap = new Bitmap(imageListWithPoints[SliceNum - borderleft]);
                    pictureBox1.Image = filter.Apply(tmpBitmap);
                }
            }
            else
            {
                pictureBox1.Image = coloredImageList[SliceNum - borderleft];
            }
        }


       








    }
}