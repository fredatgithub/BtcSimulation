using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BtcSimulation
{
  public partial class ChartView : UserControl
  {
    private readonly List<(DateTime t, decimal price)> _points = new List<(DateTime t, decimal price)>();

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

    private void ChartView_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
      Render();
    }

    private void Render()
    {
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

      if (max <= min)
        max = min + 1m;

      var pts = new PointCollection(_points.Count);
      for (int i = 0; i < _points.Count; i++)
      {
        var x = i * (w / (_points.Count - 1));
        var yNorm = (double)((_points[i].price - min) / (max - min));
        var y = h - (yNorm * h);
        pts.Add(new Point(x, y));
      }

      PriceLine.Points = pts;

      var fr = CultureInfo.GetCultureInfo("fr-FR");
      RangeText.Text = $"Min/Max: {min.ToString("N2", fr)} € / {max.ToString("N2", fr)} €";

      var last = _points[_points.Count - 1];
      LastPointText.Text = $"{last.t:HH:mm:ss}  {last.price.ToString("N2", fr)} €";
    }
  }
}

