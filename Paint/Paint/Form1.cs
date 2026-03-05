using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace Paint
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            SetSize();
            this.KeyPreview = true;
            this.KeyDown += Form1_KeyDown;

            pictureBox1.Paint += pictureBox1_Paint;
            this.DoubleBuffered = true;
            pictureBox1.MouseWheel += pictureBox1_MouseWheel;
            
            SetButtonIcons();
        }
        
        private void SetButtonIcons()
        {
            button15.Image = CreateBrushIcon();
            button15.ImageAlign = ContentAlignment.MiddleCenter;
            button15.TextImageRelation = TextImageRelation.Overlay;
            
            button12.Image = CreateSquareIcon();
            button12.ImageAlign = ContentAlignment.MiddleCenter;
            button12.TextImageRelation = TextImageRelation.Overlay;
            
            button13.Image = CreateCircleIcon();
            button13.ImageAlign = ContentAlignment.MiddleCenter;
            button13.TextImageRelation = TextImageRelation.Overlay;
            
            button14.Image = CreateHexagonIcon();
            button14.ImageAlign = ContentAlignment.MiddleCenter;
            button14.TextImageRelation = TextImageRelation.Overlay;
            
            button16.Image = CreateFillIcon();
            button16.ImageAlign = ContentAlignment.MiddleCenter;
            button16.TextImageRelation = TextImageRelation.Overlay;
            
            button11.Image = CreateColorPickerIcon();
            button11.ImageAlign = ContentAlignment.MiddleCenter;
            button11.TextImageRelation = TextImageRelation.Overlay;
        }
        
        private Bitmap CreateBrushIcon()
        {
            Bitmap bmp = new Bitmap(18, 18);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                using (Pen p = new Pen(Color.Black, 2.5f))
                {
                    p.StartCap = LineCap.Round;
                    p.EndCap = LineCap.Round;
                    g.DrawLine(p, 3, 15, 15, 3);
                }
                g.FillEllipse(Brushes.Black, 13, 1, 4, 4);
            }
            return bmp;
        }
        
        private Bitmap CreateSquareIcon()
        {
            Bitmap bmp = new Bitmap(18, 18);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                using (Pen p = new Pen(Color.Black, 2f))
                {
                    g.DrawRectangle(p, 3, 3, 12, 12);
                }
            }
            return bmp;
        }
        
        private Bitmap CreateCircleIcon()
        {
            Bitmap bmp = new Bitmap(18, 18);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                using (Pen p = new Pen(Color.Black, 2f))
                {
                    g.DrawEllipse(p, 3, 3, 12, 12);
                }
            }
            return bmp;
        }
        
        private Bitmap CreateHexagonIcon()
        {
            Bitmap bmp = new Bitmap(18, 18);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                PointF[] pts = new PointF[6];
                float cx = 9f, cy = 9f, r = 7f;
                for (int i = 0; i < 6; i++)
                {
                    double angle = Math.PI / 3 * i;
                    pts[i] = new PointF(
                        cx + r * (float)Math.Cos(angle),
                        cy + r * (float)Math.Sin(angle)
                    );
                }
                using (Pen p = new Pen(Color.Black, 2f))
                {
                    g.DrawPolygon(p, pts);
                }
            }
            return bmp;
        }
        
        private Bitmap CreateFillIcon()
        {
            Bitmap bmp = new Bitmap(18, 18);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                
                Point[] bucket = new Point[]
                {
                    new Point(9, 2),
                    new Point(6, 5),
                    new Point(6, 12),
                    new Point(12, 12),
                    new Point(12, 5)
                };
                
                using (Brush br = new SolidBrush(Color.FromArgb(100, 100, 255)))
                {
                    g.FillPolygon(br, bucket);
                }
                
                using (Pen p = new Pen(Color.Black, 1.5f))
                {
                    g.DrawPolygon(p, bucket);
                    g.DrawLine(p, 7, 5, 11, 5);
                }
                
                g.FillEllipse(Brushes.DodgerBlue, 13, 13, 4, 4);
            }
            return bmp;
        }
        
        private Bitmap CreateColorPickerIcon()
        {
            Bitmap bmp = new Bitmap(18, 18);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                
                g.FillPie(Brushes.Red, 2, 2, 14, 14, 0, 90);
                g.FillPie(Brushes.Yellow, 2, 2, 14, 14, 90, 90);
                g.FillPie(Brushes.Green, 2, 2, 14, 14, 180, 90);
                g.FillPie(Brushes.Blue, 2, 2, 14, 14, 270, 90);
                
                g.FillEllipse(Brushes.White, 6, 6, 6, 6);
                
                using (Pen p = new Pen(Color.Black, 1.5f))
                {
                    g.DrawEllipse(p, 2, 2, 14, 14);
                }
            }
            return bmp;
        }
        private abstract class Shape
        {
            public Pen Pen { get; set; }
            public Color FillColor { get; set; }
            public bool Filled { get; set; }

            public abstract void Draw(Graphics g);
            public abstract bool Contains(Point p);
            public abstract Rectangle GetBounds();
        }

        private class LineShape : Shape
        {
            public List<Point> Points = new List<Point>();

            public override void Draw(Graphics g)
            {
                if (Points.Count > 1)
                    g.DrawLines(Pen, Points.ToArray());
            }

            public override bool Contains(Point p)
            {
                if (Points.Count < 2) return false;

                using (var gp = new GraphicsPath())
                {
                    gp.AddLines(Points.ToArray());

                    float tolerance = Math.Max(4f, Pen.Width + 4f);
                    using (var hitPen = new Pen(Color.Black, tolerance))
                    {
                        return gp.IsOutlineVisible(p, hitPen);
                    }
                }
            }

            public override Rectangle GetBounds()
            {
                return Rectangle.FromLTRB(
                    Points.Min(pt => pt.X),
                    Points.Min(pt => pt.Y),
                    Points.Max(pt => pt.X),
                    Points.Max(pt => pt.Y)
                );
            }
        }


        private class RectShape : Shape
        {
            public Rectangle Rect;

            public override void Draw(Graphics g)
            {
                if (Filled)
                    g.FillRectangle(new SolidBrush(FillColor), Rect);

                g.DrawRectangle(Pen, Rect);
            }

            public override bool Contains(Point p)
            {
                return Rect.Contains(p);
            }

            public override Rectangle GetBounds() => Rect;
        }

        private class EllipseShape : Shape
        {
            public Rectangle Rect;

            public override void Draw(Graphics g)
            {
                if (Filled)
                    g.FillEllipse(new SolidBrush(FillColor), Rect);

                g.DrawEllipse(Pen, Rect);
            }

            public override bool Contains(Point p)
            {
                using (GraphicsPath gp = new GraphicsPath())
                {
                    gp.AddEllipse(Rect);
                    return gp.IsVisible(p);
                }
            }

            public override Rectangle GetBounds() => Rect;
        }

        private class HexShape : Shape
        {
            public Point[] Points;

            public override void Draw(Graphics g)
            {
                if (Filled)
                    g.FillPolygon(new SolidBrush(FillColor), Points);

                g.DrawPolygon(Pen, Points);
            }

            public override bool Contains(Point p)
            {
                using (GraphicsPath gp = new GraphicsPath())
                {
                    gp.AddPolygon(Points);
                    return gp.IsVisible(p);
                }
            }

            public override Rectangle GetBounds()
            {
                int minX = Points.Min(pt => pt.X);
                int minY = Points.Min(pt => pt.Y);
                int maxX = Points.Max(pt => pt.X);
                int maxY = Points.Max(pt => pt.Y);

                return Rectangle.FromLTRB(minX, minY, maxX, maxY);
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.Z)
                Undo();
            else if (e.KeyCode == Keys.Delete)
                DeleteSelectedShape();
        }
        private bool mdown = false;
        private Bitmap map = new Bitmap(100, 100);
        private Graphics graphics;
        private Pen pen = new Pen(Color.Black, 3f);
        private bool drawSquare = false;
        private bool drawEllipse = false;
        private bool drawHexagon = false;
        private Point startPoint;
        private Point currentPoint;
        private bool fillMode = false;
        private Stack<CanvasState> history = new Stack<CanvasState>();
        private const int maxHistory = 5;
        private List<Shape> shapes = new List<Shape>();
        private Shape selectedShape = null;
        private LineShape currentLine = null;
        private bool isDragging = false;
        private Point dragStart;
        private Shape dragOriginalClone;
        private Shape scaleOriginalClone = null;
        private float currentScale = 1f;
        private bool changeOutlineMode = false;

        private void SetSize()
        {
            Rectangle rectangle = Screen.PrimaryScreen.Bounds;
            map = new Bitmap(rectangle.Width, rectangle.Height);
            graphics = Graphics.FromImage(map);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            pen.StartCap = LineCap.Round;
            pen.EndCap = LineCap.Round;
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                SelectShapeAt(e.Location);
                if (selectedShape != null)
                {
                    isDragging = true;
                    dragStart = e.Location;
                    dragOriginalClone = CloneShape(selectedShape);
                }
                return;
            }

            if (e.Button == MouseButtons.Left)
            {
                mdown = true;
                startPoint = e.Location;
                currentPoint = e.Location;
                SaveState();

                if (!drawSquare && !drawEllipse && !drawHexagon)
                {
                    currentLine = new LineShape() { Pen = (Pen)pen.Clone() };
                    currentLine.Points.Add(e.Location);
                }
            }
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                isDragging = false;
                dragOriginalClone = null;
                scaleOriginalClone = null;
                currentScale = 1f;
                return;
            }

            if (e.Button != MouseButtons.Left)
                return;

            mdown = false;

            if (drawSquare)
                shapes.Add(new RectShape()
                {
                    Rect = GetRectangle(startPoint, e.Location),
                    Pen = (Pen)pen.Clone(),
                    Filled = fillMode,
                    FillColor = pen.Color
                });
            else if (drawEllipse)
                shapes.Add(new EllipseShape()
                {
                    Rect = GetRectangle(startPoint, e.Location),
                    Pen = (Pen)pen.Clone(),
                    Filled = fillMode,
                    FillColor = pen.Color
                });
            else if (drawHexagon)
                shapes.Add(new HexShape()
                {
                    Points = GetHexagonPoints(startPoint, e.Location),
                    Pen = (Pen)pen.Clone(),
                    Filled = fillMode,
                    FillColor = pen.Color
                });
            else if (currentLine != null)
            {
                shapes.Add(currentLine);
                currentLine = null;
            }

            pictureBox1.Invalidate();
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging && selectedShape != null)
            {
                ApplyOffset(selectedShape, dragOriginalClone, e.X - dragStart.X, e.Y - dragStart.Y);
                pictureBox1.Invalidate();
                return;
            }

            if (!mdown || e.Button != MouseButtons.Left)
                return;

            currentPoint = e.Location;

            if (!drawSquare && !drawEllipse && !drawHexagon && currentLine != null)
            {
                Point last = currentLine.Points[currentLine.Points.Count - 1];
                float dx = e.X - last.X;
                float dy = e.Y - last.Y;
                float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                
                if (dist > 2f)
                {
                    int steps = (int)(dist / 2f);
                    for (int i = 1; i <= steps; i++)
                    {
                        float t = i / (float)steps;
                        currentLine.Points.Add(new Point(
                            (int)(last.X + dx * t),
                            (int)(last.Y + dy * t)
                        ));
                    }
                }
                else
                    currentLine.Points.Add(e.Location);
            }

            pictureBox1.Invalidate();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Color newColor = ((Button)sender).BackColor;

            if (selectedShape != null)
            {
                selectedShape.Pen.Color = newColor;
                if (selectedShape.Filled && !changeOutlineMode)
                    selectedShape.FillColor = newColor;
                
                changeOutlineMode = false;
                pictureBox1.Invalidate();
            }
            else
                pen.Color = newColor;
        }




        private void button11_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                if (selectedShape != null)
                {
                    selectedShape.Pen.Color = colorDialog1.Color;
                    if (fillMode && selectedShape.Filled)
                        selectedShape.FillColor = colorDialog1.Color;
                    pictureBox1.Invalidate();
                }
                else
                {
                    pen.Color = colorDialog1.Color;
                    ((Button)sender).BackColor = colorDialog1.Color;
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            graphics.Clear(pictureBox1.BackColor);
            shapes.Clear();
            selectedShape = null;
            pictureBox1.Invalidate();
        }

        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            pen.Width = trackBar1.Value;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            saveFileDialog1.Filter = "JPG(*.JPG)|*.jpg";
            if (saveFileDialog1.ShowDialog() == DialogResult.OK && pictureBox1.Image != null)
                pictureBox1.Image.Save(saveFileDialog1.FileName);
        }
        private void button15_Click(object sender, EventArgs e)
        {
            drawSquare = false;
            drawEllipse = false;
            drawHexagon = false;
        }

        private void button12_Click(object sender, EventArgs e)
        {
            drawSquare = true;
            drawEllipse = false;
            drawHexagon = false;
        }
        private Rectangle GetRectangle(Point p1, Point p2)
        {
            return new Rectangle(
                Math.Min(p1.X, p2.X),
                Math.Min(p1.Y, p2.Y),
                Math.Abs(p1.X - p2.X),
                Math.Abs(p1.Y - p2.Y)
            );
        }

        private void button13_Click(object sender, EventArgs e)
        {
            drawEllipse = true;
            drawSquare = false;
            drawHexagon = false;
        }

        private void button14_Click(object sender, EventArgs e)
        {
            drawHexagon = true;
            drawSquare = false;
            drawEllipse = false;
        }
        private Point[] GetHexagonPoints(Point p1, Point p2)
        {
            Rectangle rect = GetRectangle(p1, p2);

            float cx = rect.X + rect.Width / 2f;
            float cy = rect.Y + rect.Height / 2f;

            float rx = rect.Width / 2f;
            float ry = rect.Height / 2f;

            Point[] pts = new Point[6];

            for (int i = 0; i < 6; i++)
            {
                double angle = Math.PI / 3 * i;

                pts[i] = new Point(
                    (int)(cx + rx * Math.Cos(angle)),
                    (int)(cy + ry * Math.Sin(angle))
                );
            }

            return pts;
        }


        private void button16_Click(object sender, EventArgs e)
        {
            if (selectedShape != null)
            {
                selectedShape.Filled = !selectedShape.Filled;
                pictureBox1.Invalidate();
            }
            else
            {
                fillMode = !fillMode;
                button16.BackColor = fillMode ? Color.LightGreen : SystemColors.Control;
            }
        }
        private void SaveState()
        {
            var state = new CanvasState()
            {
                Map = (Bitmap)map.Clone(),
                Shapes = shapes.Select(s => CloneShape(s)).ToList()
            };

            history.Push(state);

            if (history.Count > maxHistory)
                history = new Stack<CanvasState>(history.Take(maxHistory).Reverse());
        }

        private void Undo()
        {
            if (history.Count > 0)
            {
                var state = history.Pop();

                map = state.Map;
                shapes = state.Shapes;
                graphics = Graphics.FromImage(map);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;

                pictureBox1.Invalidate();
            }
        }

        private void SelectShapeAt(Point p)
        {
            selectedShape = null;
            for (int i = shapes.Count - 1; i >= 0; i--)
            {
                if (shapes[i].Contains(p))
                {
                    selectedShape = shapes[i];
                    break;
                }
            }
            pictureBox1.Invalidate();
        }
        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            if (map != null)
                e.Graphics.DrawImageUnscaled(map, 0, 0);

            foreach (var s in shapes)
                s.Draw(e.Graphics);

            currentLine?.Draw(e.Graphics);

            if (mdown && (drawSquare || drawEllipse || drawHexagon))
            {
                Rectangle rect = GetRectangle(startPoint, currentPoint);
                if (drawSquare)
                    e.Graphics.DrawRectangle(pen, rect);
                else if (drawEllipse)
                    e.Graphics.DrawEllipse(pen, rect);
                else if (drawHexagon)
                    e.Graphics.DrawPolygon(pen, GetHexagonPoints(startPoint, currentPoint));
            }

            if (selectedShape != null)
            {
                using (Pen selPen = new Pen(Color.Red, 2) { DashStyle = DashStyle.Dash })
                    e.Graphics.DrawRectangle(selPen, selectedShape.GetBounds());
            }
        }
        private void DeleteSelectedShape()
        {
            if (selectedShape != null)
            {
                shapes.Remove(selectedShape);
                selectedShape = null;
                pictureBox1.Invalidate();
            }
        }
        
        private Shape CloneShape(Shape s)
        {
            switch (s)
            {
                case LineShape ln:
                    return new LineShape()
                    {
                        Pen = (Pen)ln.Pen.Clone(),
                        Points = ln.Points.Select(p => new Point(p.X, p.Y)).ToList(),
                        FillColor = ln.FillColor,
                        Filled = ln.Filled
                    };

                case RectShape rs:
                    return new RectShape()
                    {
                        Pen = (Pen)rs.Pen.Clone(),
                        Rect = rs.Rect,
                        FillColor = rs.FillColor,
                        Filled = rs.Filled
                    };

                case EllipseShape es:
                    return new EllipseShape()
                    {
                        Pen = (Pen)es.Pen.Clone(),
                        Rect = es.Rect,
                        FillColor = es.FillColor,
                        Filled = es.Filled
                    };

                case HexShape hs:
                    return new HexShape()
                    {
                        Pen = (Pen)hs.Pen.Clone(),
                        Points = hs.Points.Select(p => new Point(p.X, p.Y)).ToArray(),
                        FillColor = hs.FillColor,
                        Filled = hs.Filled
                    };
            }
            return null;
        }
        private void ApplyOffset(Shape target, Shape original, int dx, int dy)
        {
            switch (target)
            {
                case LineShape ln:
                    var orLn = (LineShape)original;
                    ln.Points = orLn.Points
                        .Select(p => new Point(p.X + dx, p.Y + dy))
                        .ToList();
                    break;

                case RectShape rs:
                    var orRs = (RectShape)original;
                    rs.Rect = new Rectangle(
                        orRs.Rect.X + dx,
                        orRs.Rect.Y + dy,
                        orRs.Rect.Width,
                        orRs.Rect.Height);
                    break;

                case EllipseShape es:
                    var orEs = (EllipseShape)original;
                    es.Rect = new Rectangle(
                        orEs.Rect.X + dx,
                        orEs.Rect.Y + dy,
                        orEs.Rect.Width,
                        orEs.Rect.Height);
                    break;

                case HexShape hs:
                    var orHs = (HexShape)original;
                    hs.Points = orHs.Points
                        .Select(p => new Point(p.X + dx, p.Y + dy))
                        .ToArray();
                    break;
            }
        }
        private void pictureBox1_MouseWheel(object sender, MouseEventArgs e)
        {
            if (selectedShape == null)
                return;

            float step = (e.Delta > 0) ? 1.1f : 0.9f;

            if (scaleOriginalClone == null)
            {
                scaleOriginalClone = CloneShape(selectedShape);
                currentScale = 1f;
            }

            currentScale *= step;

            Rectangle bounds = scaleOriginalClone.GetBounds();
            Point center = new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);

            if (selectedShape is LineShape ln && scaleOriginalClone is LineShape lnOrig)
                ScaleLineFromOriginal(ln, lnOrig, currentScale, center);
            else
                ScaleShape(selectedShape, currentScale, center);

            pictureBox1.Invalidate();
        }


        private void ScaleShape(Shape s, float scale, Point center)
        {
            switch (s)
            {
                case LineShape ln:
                    for (int i = 0; i < ln.Points.Count; i++)
                    {
                        ln.Points[i] = new Point(
                            (int)(center.X + (ln.Points[i].X - center.X) * scale),
                            (int)(center.Y + (ln.Points[i].Y - center.Y) * scale)
                        );
                    }
                    break;

                case RectShape rs:
                    int newW = (int)(rs.Rect.Width * scale);
                    int newH = (int)(rs.Rect.Height * scale);
                    rs.Rect = new Rectangle(
                        center.X - newW / 2,
                        center.Y - newH / 2,
                        newW, newH
                    );
                    break;

                case EllipseShape es:
                    int ew = (int)(es.Rect.Width * scale);
                    int eh = (int)(es.Rect.Height * scale);
                    es.Rect = new Rectangle(
                        center.X - ew / 2,
                        center.Y - eh / 2,
                        ew, eh
                    );
                    break;

                case HexShape hs:
                    for (int i = 0; i < hs.Points.Length; i++)
                    {
                        hs.Points[i] = new Point(
                            (int)(center.X + (hs.Points[i].X - center.X) * scale),
                            (int)(center.Y + (hs.Points[i].Y - center.Y) * scale)
                        );
                    }
                    break;
            }
        }
        private void ScaleLineFromOriginal(LineShape target, LineShape original, float scale, Point center)
        {
            for (int i = 0; i < target.Points.Count; i++)
            {
                target.Points[i] = new Point(
                    (int)(center.X + (original.Points[i].X - center.X) * scale),
                    (int)(center.Y + (original.Points[i].Y - center.Y) * scale)
                );
            }
        }

        private void button17_Click(object sender, EventArgs e)
        {
            if (selectedShape != null)
                changeOutlineMode = true;
        }
        private class CanvasState
        {
            public Bitmap Map;
            public List<Shape> Shapes;
        }

    }
}