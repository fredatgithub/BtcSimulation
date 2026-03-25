using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BtcSimulation
{
  public partial class ChartView : UserControl
  {
    private readonly List<(DateTime t, decimal price)> _points = new List<(DateTime t, decimal price)>();
    private readonly CultureInfo _fr = CultureInfo.GetCultureInfo("fr-FR");
    private const double MarginLeft = 55, MarginBottom = 30, MarginTop = 10, MarginRight = 10;

    public ChartView()
    {
      InitializeComponent();
      Loaded += (_, __) => Render();
    }

    public void AddPoint(DateTime time, decimal priceEur, int maxPoints = 240)
    {
      _points.Add((time, priceEur));
      if (_points.Count > maxPoints)
        _points.RemoveRange(0, _points.Count - maxPoints);
      Render();
    }

    private void ChartView_OnSizeChanged(object sender, SizeChangedEventArgs e) => Render();

    private void Render()
    {
      AxesCanvas.Children.Clear();

      if (_points.Count < 2 || PlotCanvas.ActualWidth <= 1 || PlotCanvas.ActualHeight <= 1)
      {
        EmptyText.Visibility = Visibility.Visible;
        PriceLine.Points = new PointCollection();
        RangeText.Text = "";
        LastPointText.Text = "";
        return;
      }

      EmptyText.Visibility = Visibility.Collapsed;

      var w = PlotCanvas.ActualWidth;
      var h = PlotCanvas.ActualHeight;
      var min = _points.Min(p => p.price);
      var max = _points.Max(p => p.price);
      if (max <= min) max = min + 1m;

      var pts = new PointCollection(_points.Count);
      for (int i = 0; i < _points.Count; i++)
      {
        var x = i * (w / (_points.Count - 1));
        var yNorm = (double)((_points[i].price - min) / (max - min));
        pts.Add(new Point(x, h - yNorm * h));
      }
      PriceLine.Points = pts;

      DrawAxes(w, h, min, max);

      RangeText.Text = $"Min/Max: {min.ToString("N2", _fr)} € / {max.ToString("N2", _fr)} €";
      var last = _points[_points.Count - 1];
      LastPointText.Text = $"{last.t:HH:mm:ss}  {last.price.ToString("N2", _fr)} €";
    }

    private void DrawAxes(double w, double h, decimal min, decimal max)
    {
      var axisColor = new SolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99));
      var textColor = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44));
      double ox = MarginLeft, oy = MarginTop, pw = w, ph = h;

      // Axe Y
      AxesCanvas.Children.Add(new Line { X1 = ox, Y1 = oy, X2 = ox, Y2 = oy + ph, Stroke = axisColor, StrokeThickness = 1 });
      // Axe X
      AxesCanvas.Children.Add(new Line { X1 = ox, Y1 = oy + ph, X2 = ox + pw, Y2 = oy + ph, Stroke = axisColor, StrokeThickness = 1 });

      // Graduations Y (5 niveaux)
      const int yTicks = 5;
      for (int i = 0; i <= yTicks; i++)
      {
        double yRatio = (double)i / yTicks;
        double yPos = oy + ph - yRatio * ph;
        decimal price = min + (max - min) * (decimal)yRatio;

        AxesCanvas.Children.Add(new Line { X1 = ox - 4, Y1 = yPos, X2 = ox, Y2 = yPos, Stroke = axisColor, StrokeThickness = 1 });
        var lbl = new TextBlock { Text = price.ToString("N0", _fr), Foreground = textColor, FontSize = 10 };
        Canvas.SetRight(lbl, AxesCanvas.ActualWidth - ox + 6);
        Canvas.SetTop(lbl, yPos - 7);
        AxesCanvas.Children.Add(lbl);
      }

      // Graduations X (4 labels de temps)
      const int xTicks = 4;
      for (int i = 0; i <= xTicks; i++)
      {
        double xRatio = (double)i / xTicks;
        double xPos = ox + xRatio * pw;
        int idx = (int)(xRatio * (_points.Count - 1));
        string time = _points[idx].t.ToString("HH:mm");

        AxesCanvas.Children.Add(new Line { X1 = xPos, Y1 = oy + ph, X2 = xPos, Y2 = oy + ph + 4, Stroke = axisColor, StrokeThickness = 1 });
        var lbl = new TextBlock { Text = time, Foreground = textColor, FontSize = 10 };
        Canvas.SetLeft(lbl, xPos - 14);
        Canvas.SetTop(lbl, oy + ph + 6);
        AxesCanvas.Children.Add(lbl);
      }
    }
  }
}

