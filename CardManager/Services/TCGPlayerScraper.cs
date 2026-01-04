using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CardManager.Models;

namespace CardManager.Services
{
    public class TCGPlayerScraper
    {
        private readonly HttpClient _httpClient;
        private const string DetailsApiUrl = "https://mp-search-api.tcgplayer.com/v1/product/{0}/details";
        private const string LatestSalesApiUrl = "https://mpapi.tcgplayer.com/v2/product/{0}/latestsales?mpfev=4622";

        public TCGPlayerScraper()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", 
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<Card?> ScrapeCardAsync(int cardId)
        {
            try
            {
                var card = new Card
                {
                    Id = cardId,
                    LastUpdated = DateTime.Now
                };

                // Fetch product details
                await FetchProductDetailsAsync(card);
                
                // Fetch latest sales data
                await FetchLatestSalesAsync(card);

                return card;
            }
            catch (HttpRequestException)
            {
                return null;
            }
            catch (TaskCanceledException)
            {
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private async Task FetchProductDetailsAsync(Card card)
        {
            try
            {
                string url = string.Format(DetailsApiUrl, card.Id);
                string json = await _httpClient.GetStringAsync(url);

                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                // Extract product name
                if (root.TryGetProperty("productName", out var nameElement))
                {
                    card.Name = nameElement.GetString() ?? $"Card #{card.Id}";
                }
                else
                {
                    card.Name = $"Card #{card.Id}";
                }

                // Extract market price
                if (root.TryGetProperty("marketPrice", out var marketPriceElement) && 
                    marketPriceElement.ValueKind == JsonValueKind.Number)
                {
                    card.MarketPrice = marketPriceElement.GetDecimal();
                }

                // Extract lowest price
                if (root.TryGetProperty("lowestPrice", out var lowestPriceElement) && 
                    lowestPriceElement.ValueKind == JsonValueKind.Number)
                {
                    card.LowestPrice = lowestPriceElement.GetDecimal();
                }
            }
            catch
            {
                // If details fetch fails, keep default values
                if (string.IsNullOrEmpty(card.Name))
                {
                    card.Name = $"Card #{card.Id}";
                }
            }
        }

        private async Task FetchLatestSalesAsync(Card card)
        {
            try
            {
                string url = string.Format(LatestSalesApiUrl, card.Id);
                
                // This endpoint requires POST
                var content = new StringContent("{}", Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);
                
                if (!response.IsSuccessStatusCode)
                    return;

                string json = await response.Content.ReadAsStringAsync();
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                // Get the data array
                if (root.TryGetProperty("data", out var dataElement) && 
                    dataElement.ValueKind == JsonValueKind.Array &&
                    dataElement.GetArrayLength() > 0)
                {
                    // Get the first (most recent) sale
                    var firstSale = dataElement[0];

                    // Extract purchase price
                    if (firstSale.TryGetProperty("purchasePrice", out var priceElement) &&
                        priceElement.ValueKind == JsonValueKind.Number)
                    {
                        card.LatestSalePrice = priceElement.GetDecimal();
                    }

                    // Extract order date
                    if (firstSale.TryGetProperty("orderDate", out var dateElement) &&
                        dateElement.ValueKind == JsonValueKind.String)
                    {
                        string? dateStr = dateElement.GetString();
                        if (!string.IsNullOrEmpty(dateStr) && DateTime.TryParse(dateStr, out DateTime orderDate))
                        {
                            card.LatestSaleDate = orderDate.ToLocalTime();
                        }
                    }
                }
            }
            catch
            {
                // If latest sales fetch fails, leave as null
            }
        }
    }
}
