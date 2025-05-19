using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using System.Windows.Forms;

namespace MandelbrotFractal
{
    public partial class MainForm : Form
    {
        private double scale = 1.0;
        private double offsetX = 0.0;
        private double offsetY = 0.0;
        private int maxIterations = 100;
        private double escapeRadius = 2.0;
        private double viewSize = 2.0;
        private Complex z0 = Complex.Zero;
        private Point? lastMousePos;

        private Color[] colorPalette;

        public MainForm()
        {
            InitializeColorPalette();
            this.DoubleBuffered = true;
            this.ClientSize = new Size(800, 600);
            this.Text = "Фрактальное множество Мандельброта z^5 + c";

            // Настройка элементов управления
            var paramsPanel = new Panel { Dock = DockStyle.Right, Width = 200 };
            var btnDraw = new Button { Text = "Построить", Dock = DockStyle.Top };
            var btnSave = new Button { Text = "Сохранить", Dock = DockStyle.Top };
            var btnReset = new Button { Text = "Сбросить", Dock = DockStyle.Top };

            var lblIterations = new Label { Text = "Макс. итераций (N):", Dock = DockStyle.Top };
            var tbIterations = new NumericUpDown { Value = maxIterations, Minimum = 10, Maximum = 1000, Dock = DockStyle.Top };

            var lblRadius = new Label { Text = "Радиус (R):", Dock = DockStyle.Top };
            var tbRadius = new NumericUpDown { Value = (decimal)escapeRadius, Minimum = 1, Maximum = 100, DecimalPlaces = 2, Dock = DockStyle.Top };

            var lblViewSize = new Label { Text = "Размер области (a):", Dock = DockStyle.Top };
            var tbViewSize = new NumericUpDown { Value = (decimal)viewSize, Minimum = 0.1M, Maximum = 10, DecimalPlaces = 2, Dock = DockStyle.Top };

            var lblZ0Real = new Label { Text = "z0 (вещественная):", Dock = DockStyle.Top };
            var tbZ0Real = new NumericUpDown { Value = 0, Minimum = -10, Maximum = 10, DecimalPlaces = 2, Dock = DockStyle.Top };

            var lblZ0Imag = new Label { Text = "z0 (мнимая):", Dock = DockStyle.Top };
            var tbZ0Imag = new NumericUpDown { Value = 0, Minimum = -10, Maximum = 10, DecimalPlaces = 2, Dock = DockStyle.Top };

            btnDraw.Click += (s, e) =>
            {
                maxIterations = (int)tbIterations.Value;
                escapeRadius = (double)tbRadius.Value;
                viewSize = (double)tbViewSize.Value;
                z0 = new Complex((double)tbZ0Real.Value, (double)tbZ0Imag.Value);
                this.Invalidate();
            };

            btnSave.Click += (s, e) =>
            {
                using (var saveDialog = new SaveFileDialog())
                {
                    saveDialog.Filter = "JPEG Image|*.jpg";
                    if (saveDialog.ShowDialog() == DialogResult.OK)
                    {
                        var bmp = new Bitmap(this.ClientSize.Width - paramsPanel.Width, this.ClientSize.Height);
                        this.DrawToBitmap(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height));
                        bmp.Save(saveDialog.FileName, ImageFormat.Jpeg);
                    }
                }
            };

            btnReset.Click += (s, e) =>
            {
                scale = 1.0;
                offsetX = 0.0;
                offsetY = 0.0;
                this.Invalidate();
            };

            paramsPanel.Controls.AddRange(new Control[] {
                btnReset, btnSave, btnDraw,
                lblIterations, tbIterations,
                lblRadius, tbRadius,
                lblViewSize, tbViewSize,
                lblZ0Real, tbZ0Real,
                lblZ0Imag, tbZ0Imag
            });

            this.Controls.Add(paramsPanel);

            this.MouseWheel += (s, e) =>
            {
                double zoomFactor = e.Delta > 0 ? 1.1 : 0.9;
                scale *= zoomFactor;
                this.Invalidate();
            };
            this.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                    lastMousePos = e.Location;
            };

            this.MouseMove += (s, e) =>
            {
                if (lastMousePos.HasValue)
                {
                    offsetX += (lastMousePos.Value.X - e.X) / scale;
                    offsetY += (lastMousePos.Value.Y - e.Y) / scale;
                    lastMousePos = e.Location;
                    this.Invalidate();
                }
            };

            this.MouseUp += (s, e) =>
            {
                lastMousePos = null;
            };

            this.Paint += (s, e) =>
            {
                DrawFractal(e.Graphics);
            };
        }

        private void InitializeColorPalette()
        {
            colorPalette = new Color[256];
            for (int i = 0; i < 256; i++)
            {
                int r = (int)(Math.Sin(0.3 * i + 0) * 127 + 128);
                int g = (int)(Math.Sin(0.3 * i + 2) * 127 + 128);
                int b = (int)(Math.Sin(0.3 * i + 4) * 127 + 128);
                colorPalette[i] = Color.FromArgb(r, g, b);
            }
        }

        private void DrawFractal(Graphics g)
        {
            int width = this.ClientSize.Width - 200;
            int height = this.ClientSize.Height;

            var bmp = new Bitmap(width, height);

            double xMin = -viewSize / scale + offsetX;
            double xMax = viewSize / scale + offsetX;
            double yMin = -viewSize / scale + offsetY;
            double yMax = viewSize / scale + offsetY;

            double xStep = (xMax - xMin) / width;
            double yStep = (yMax - yMin) / height;

            for (int px = 0; px < width; px++)
            {
                for (int py = 0; py < height; py++)
                {
                    double x = xMin + px * xStep;
                    double y = yMin + py * yStep;

                    Complex c = new Complex(x, y);
                    Complex z = z0;

                    int iteration = 0;
                    while (iteration < maxIterations && z.Magnitude < escapeRadius)
                    {
                        z = Complex.Pow(z, 5) + c;
                        iteration++;
                    }

                    if (iteration == maxIterations)
                    {
                        bmp.SetPixel(px, py, Color.Black);
                    }
                    else
                    {
                        double smoothed = iteration + 1 - Math.Log(Math.Log(z.Magnitude)) / Math.Log(5);
                        int colorIndex = (int)(Math.Sqrt(smoothed / maxIterations) * 255) % 256;
                        bmp.SetPixel(px, py, colorPalette[colorIndex]);
                    }
                }
            }

            g.DrawImage(bmp, 0, 0);
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}