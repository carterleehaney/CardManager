using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CardManager.Models;

namespace CardManager.Services
{
    public class CardService
    {
        private readonly JsonCardRepository _repository;
        private readonly TCGPlayerScraper _scraper;

        public CardService()
        {
            _repository = new JsonCardRepository();
            _scraper = new TCGPlayerScraper();
        }

        public async Task<List<Card>> GetAllCardsAsync()
        {
            return await _repository.LoadCardsAsync();
        }

        public async Task<Card?> AddCardAsync(int cardId, string? category = null)
        {
            var card = await _scraper.ScrapeCardAsync(cardId);
            if (card != null)
            {
                if (!string.IsNullOrEmpty(category))
                {
                    card.Category = category;
                }
                await _repository.AddOrUpdateCardAsync(card);
            }
            return card;
        }

        public async Task<Card?> RefreshCardAsync(int cardId)
        {
            // Get existing card to preserve category
            var existingCards = await _repository.LoadCardsAsync();
            var existingCard = existingCards.FirstOrDefault(c => c.Id == cardId);
            string? existingCategory = existingCard?.Category;

            var card = await _scraper.ScrapeCardAsync(cardId);
            if (card != null)
            {
                // Preserve the category when refreshing
                if (!string.IsNullOrEmpty(existingCategory))
                {
                    card.Category = existingCategory;
                }
                await _repository.AddOrUpdateCardAsync(card);
            }
            return card;
        }

        public async Task<List<Card>> RefreshAllCardsAsync(IProgress<(int current, int total)>? progress = null)
        {
            var cards = await _repository.LoadCardsAsync();
            var updatedCards = new List<Card>();

            for (int i = 0; i < cards.Count; i++)
            {
                var refreshedCard = await _scraper.ScrapeCardAsync(cards[i].Id);
                if (refreshedCard != null)
                {
                    // Preserve the category when refreshing
                    refreshedCard.Category = cards[i].Category;
                    updatedCards.Add(refreshedCard);
                }
                else
                {
                    // Keep old data if refresh fails
                    updatedCards.Add(cards[i]);
                }

                progress?.Report((i + 1, cards.Count));
            }

            await _repository.SaveCardsAsync(updatedCards);
            return updatedCards;
        }

        public async Task UpdateCardCategoryAsync(int cardId, string category)
        {
            var cards = await _repository.LoadCardsAsync();
            var card = cards.FirstOrDefault(c => c.Id == cardId);
            if (card != null)
            {
                card.Category = category;
                await _repository.SaveCardsAsync(cards);
            }
        }

        public async Task<List<string>> GetAllCategoriesAsync()
        {
            var cards = await _repository.LoadCardsAsync();
            return cards
                .Where(c => !string.IsNullOrEmpty(c.Category))
                .Select(c => c.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();
        }

        public async Task DeleteCardAsync(int cardId)
        {
            await _repository.DeleteCardAsync(cardId);
        }
    }
}
