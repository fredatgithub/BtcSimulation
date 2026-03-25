using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Threading;
using System.Globalization;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BtcSimulation
{
  /// <summary>
  /// Logique d'interaction pour MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    private static readonly HttpClient Http = new HttpClient();
    private readonly DispatcherTimer _btcTimer;
    private CancellationTokenSource _cts = new CancellationTokenSource();
    private bool _isUpdating;

    public MainWindow()
    {
      InitializeComponent();

      if (!Http.DefaultRequestHeaders.UserAgent.Any())
        Http.DefaultRequestHeaders.UserAgent.ParseAdd("BtcSimulation/1.0 (+WPF .NET 4.8)");

      _btcTimer = new DispatcherTimer
      {
        Interval = TimeSpan.FromSeconds(30),
      };
      _btcTimer.Tick += async (_, __) => await UpdateBtcPriceAsync();
    }

    private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
      await UpdateBtcPriceAsync();
      _btcTimer.Start();
    }

    private void MainWindow_OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      _btcTimer.Stop();
      _cts.Cancel();
      _cts.Dispose();
    }

    private async Task UpdateBtcPriceAsync()
    {
      if (_isUpdating) return;
      _isUpdating = true;

      try
      {
        StatusText.Text = "";
        _cts.Cancel();
        _cts.Dispose();
        _cts = new CancellationTokenSource();

        var prices = await FetchBtcEurUsdAsync(_cts.Token);
        BtcPriceText.Text = FormatPrice(prices.Eur, "fr-FR", "€");
        BtcPriceUsdText.Text = FormatPrice(prices.Usd, "en-US", "$");
        LastUpdatedText.Text = $"Dernière mise à jour : {DateTime.Now:HH:mm:ss}";
      }
      catch (OperationCanceledException)
      {
        // Ignoré : une requête précédente a été annulée.
      }
      catch (Exception ex)
      {
        StatusText.Text = $"Impossible de récupérer le taux BTC : {ex.Message}";
      }
      finally
      {
        _isUpdating = false;
      }
    }

    private static string FormatPrice(decimal amount, string cultureName, string suffix)
    {
      return string.Format(CultureInfo.GetCultureInfo(cultureName), "{0:N2} {1}", amount, suffix);
    }

    private static async Task<BtcEurUsd> FetchBtcEurUsdAsync(CancellationToken ct)
    {
      // CoinGecko: {"bitcoin":{"eur":12345.67,"usd":13579.24}}
      var url = "https://api.coingecko.com/api/v3/simple/price?ids=bitcoin&vs_currencies=eur,usd";
      using (var req = new HttpRequestMessage(HttpMethod.Get, url))
      using (var resp = await Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct))
      {
        resp.EnsureSuccessStatusCode();
        using (var stream = await resp.Content.ReadAsStreamAsync())
        {
          var serializer = new DataContractJsonSerializer(typeof(CoinGeckoSimplePriceResponse));
          var obj = (CoinGeckoSimplePriceResponse)serializer.ReadObject(stream);

          if (obj?.Bitcoin == null)
            throw new InvalidDataException("Réponse API inattendue.");

          return new BtcEurUsd
          {
            Eur = obj.Bitcoin.Eur,
            Usd = obj.Bitcoin.Usd
          };
        }
      }
    }

    private sealed class BtcEurUsd
    {
      public decimal Eur { get; set; }
      public decimal Usd { get; set; }
    }

    [DataContract]
    private sealed class CoinGeckoSimplePriceResponse
    {
      [DataMember(Name = "bitcoin")]
      public CoinGeckoBitcoinPrices Bitcoin { get; set; }
    }

    [DataContract]
    private sealed class CoinGeckoBitcoinPrices
    {
      [DataMember(Name = "eur")]
      public decimal Eur { get; set; }

      [DataMember(Name = "usd")]
      public decimal Usd { get; set; }
    }
  }
}
