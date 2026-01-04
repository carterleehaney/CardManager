using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using CardManager.Models;

namespace CardManager.Services
{
    public class JsonCardRepository
    {
        private readonly string _filePath;
        private readonly JsonSerializerOptions _jsonOptions;

        public JsonCardRepository()
        {
            string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            _filePath = Path.Combine(appDirectory, "cards.json");
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<List<Card>> LoadCardsAsync()
        {
            if (!File.Exists(_filePath))
            {
                return new List<Card>();
            }

            try
            {
                string json = await File.ReadAllTextAsync(_filePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return new List<Card>();
                }

                return JsonSerializer.Deserialize<List<Card>>(json, _jsonOptions) ?? new List<Card>();
            }
            catch (Exception)
            {
                return new List<Card>();
            }
        }

        public async Task SaveCardsAsync(List<Card> cards)
        {
            string json = JsonSerializer.Serialize(cards, _jsonOptions);
            await File.WriteAllTextAsync(_filePath, json);
        }

        public async Task AddOrUpdateCardAsync(Card card)
        {
            var cards = await LoadCardsAsync();
            var existingIndex = cards.FindIndex(c => c.Id == card.Id);

            if (existingIndex >= 0)
            {
                cards[existingIndex] = card;
            }
            else
            {
                cards.Add(card);
            }

            await SaveCardsAsync(cards);
        }

        public async Task DeleteCardAsync(int cardId)
        {
            var cards = await LoadCardsAsync();
            cards.RemoveAll(c => c.Id == cardId);
            await SaveCardsAsync(cards);
        }
    }
}

