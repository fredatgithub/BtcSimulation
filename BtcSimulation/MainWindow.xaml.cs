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

        var price = await FetchBtcSpotPriceAsync("EUR", _cts.Token);
        BtcPriceText.Text = $"{price.Amount} {price.Currency}";
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

    private static async Task<CoinbaseSpotPrice> FetchBtcSpotPriceAsync(string currency, CancellationToken ct)
    {
      // Exemple réponse: {"data":{"base":"BTC","currency":"EUR","amount":"12345.67"}}
      var url = $"https://api.coinbase.com/v2/prices/spot?currency={Uri.EscapeDataString(currency)}";
      using (var req = new HttpRequestMessage(HttpMethod.Get, url))
      using (var resp = await Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct))
      {
        resp.EnsureSuccessStatusCode();
        using (var stream = await resp.Content.ReadAsStreamAsync())
        {
          var serializer = new DataContractJsonSerializer(typeof(CoinbaseSpotResponse));
          var obj = (CoinbaseSpotResponse)serializer.ReadObject(stream);

          if (obj?.Data == null || string.IsNullOrWhiteSpace(obj.Data.Amount) || string.IsNullOrWhiteSpace(obj.Data.Currency))
            throw new InvalidDataException("Réponse API inattendue.");

          return new CoinbaseSpotPrice
          {
            Amount = obj.Data.Amount,
            Currency = obj.Data.Currency
          };
        }
      }
    }

    private sealed class CoinbaseSpotPrice
    {
      public string Amount { get; set; }
      public string Currency { get; set; }
    }

    [DataContract]
    private sealed class CoinbaseSpotResponse
    {
      [DataMember(Name = "data")]
      public CoinbaseSpotData Data { get; set; }
    }

    [DataContract]
    private sealed class CoinbaseSpotData
    {
      [DataMember(Name = "base")]
      public string Base { get; set; }

      [DataMember(Name = "currency")]
      public string Currency { get; set; }

      [DataMember(Name = "amount")]
      public string Amount { get; set; }
    }
  }
}
