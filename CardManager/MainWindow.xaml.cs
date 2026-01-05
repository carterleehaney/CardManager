using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using CardManager.Models;
using CardManager.Services;

namespace CardManager
{
    public partial class MainWindow : Window
    {
        private readonly CardService _cardService;
        private List<Card> _allCards;
        private ICollectionView? _cardsView;
        private string _selectedCategoryFilter = "";

        // Windows API for dark title bar
        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        public MainWindow()
        {
            InitializeComponent();
            _cardService = new CardService();
            _allCards = new List<Card>();
            
            Loaded += MainWindow_Loaded;
            SourceInitialized += MainWindow_SourceInitialized;
        }

        private void MainWindow_SourceInitialized(object? sender, EventArgs e)
        {
            // Use dark title bar
            SetDarkTitleBar(true);
        }

        private void SetDarkTitleBar(bool dark)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            int value = dark ? 1 : 0;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadCardsAsync();
            await RefreshCategoryDropdownsAsync();
        }

        private async System.Threading.Tasks.Task LoadCardsAsync()
        {
            try
            {
                SetStatus("Loading cards...");
                _allCards = await _cardService.GetAllCardsAsync();
                RefreshDataGrid();
                SetStatus($"Loaded {_allCards.Count} cards");
            }
            catch (Exception ex)
            {
                SetStatus($"Error loading cards: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task RefreshCategoryDropdownsAsync()
        {
            var categories = await _cardService.GetAllCategoriesAsync();
            
            // Update the add card category dropdown
            CategoryComboBox.Items.Clear();
            CategoryComboBox.Items.Add(""); // Empty option
            foreach (var cat in categories)
            {
                CategoryComboBox.Items.Add(cat);
            }

            // Update the filter dropdown
            string currentFilter = _selectedCategoryFilter;
            FilterCategoryComboBox.Items.Clear();
            FilterCategoryComboBox.Items.Add("All Categories");
            foreach (var cat in categories)
            {
                FilterCategoryComboBox.Items.Add(cat);
            }
            
            // Restore selection
            if (!string.IsNullOrEmpty(currentFilter) && categories.Contains(currentFilter))
            {
                FilterCategoryComboBox.SelectedItem = currentFilter;
            }
            else
            {
                FilterCategoryComboBox.SelectedIndex = 0;
            }
        }

        private void RefreshDataGrid()
        {
            var filteredCards = ApplyFilter(_allCards);
            CardsDataGrid.ItemsSource = filteredCards;
            _cardsView = CollectionViewSource.GetDefaultView(CardsDataGrid.ItemsSource);
            UpdateCardCount(filteredCards.Count);
            UpdateTotalPrice();
        }

        private void UpdateTotalPrice()
        {
            decimal total = _allCards
                .Where(c => c.TotalValue.HasValue)
                .Sum(c => c.TotalValue!.Value);
            
            TotalPriceText.Text = $"${total:N2}";
        }

        private List<Card> ApplyFilter(List<Card> cards)
        {
            string searchText = SearchTextBox?.Text?.Trim().ToLower() ?? "";
            
            var filtered = cards.AsEnumerable();

            // Apply category filter
            if (!string.IsNullOrEmpty(_selectedCategoryFilter))
            {
                filtered = filtered.Where(c => c.Category == _selectedCategoryFilter);
            }

            // Apply search text filter
            if (!string.IsNullOrEmpty(searchText))
            {
                filtered = filtered.Where(c => 
                    c.Name.ToLower().Contains(searchText) ||
                    c.Id.ToString().Contains(searchText) ||
                    c.Category.ToLower().Contains(searchText)
                );
            }

            return filtered.ToList();
        }

        private void UpdateCardCount(int count)
        {
            CardCountText.Text = count == 1 ? "1 card" : $"{count} cards";
        }

        private void SetStatus(string message)
        {
            StatusText.Text = message;
        }

        private void SetButtonsEnabled(bool enabled)
        {
            AddCardButton.IsEnabled = enabled;
            RefreshAllButton.IsEnabled = enabled;
            DeleteSelectedButton.IsEnabled = enabled;
        }

        #region Button Handlers

        private async void AddCardButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(CardIdTextBox.Text.Trim(), out int cardId))
            {
                MessageBox.Show("Please enter a valid card ID (numbers only).", 
                    "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(AmountTextBox.Text.Trim(), out int amount) || amount < 1)
            {
                MessageBox.Show("Please enter a valid quantity (1 or more).", 
                    "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_allCards.Any(c => c.Id == cardId))
            {
                var result = MessageBox.Show(
                    $"Card ID {cardId} already exists. Do you want to refresh its data?",
                    "Card Exists", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result != MessageBoxResult.Yes)
                    return;
            }

            try
            {
                SetButtonsEnabled(false);
                SetStatus($"Fetching card {cardId}...");

                string? category = CategoryComboBox.Text?.Trim();
                var card = await _cardService.AddCardAsync(cardId, category, amount);

                if (card != null)
                {
                    // Update or add to local list
                    var existingIndex = _allCards.FindIndex(c => c.Id == cardId);
                    if (existingIndex >= 0)
                    {
                        _allCards[existingIndex] = card;
                    }
                    else
                    {
                        _allCards.Add(card);
                    }

                    RefreshDataGrid();
                    await RefreshCategoryDropdownsAsync();
                    CardIdTextBox.Clear();
                    AmountTextBox.Text = "1";
                    SetStatus($"Added card: {card.Name} (x{amount})");
                }
                else
                {
                    SetStatus($"Could not fetch card {cardId}. Check the ID and try again.");
                    MessageBox.Show($"Could not fetch card with ID {cardId}. Please verify the ID is correct.",
                        "Card Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Error: {ex.Message}");
                MessageBox.Show($"An error occurred: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetButtonsEnabled(true);
            }
        }

        private async void RefreshAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (_allCards.Count == 0)
            {
                MessageBox.Show("No cards to refresh.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                SetButtonsEnabled(false);

                var progress = new Progress<(int current, int total)>(p =>
                {
                    SetStatus($"Refreshing card {p.current} of {p.total}...");
                });

                _allCards = await _cardService.RefreshAllCardsAsync(progress);
                RefreshDataGrid();
                SetStatus($"Refreshed all {_allCards.Count} cards");
            }
            catch (Exception ex)
            {
                SetStatus($"Error: {ex.Message}");
                MessageBox.Show($"An error occurred: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetButtonsEnabled(true);
            }
        }

        private async void DeleteSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            if (CardsDataGrid.SelectedItem is not Card selectedCard)
            {
                MessageBox.Show("Please select a card to delete.", 
                    "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            await DeleteCardAsync(selectedCard);
        }


        #endregion

        #region Context Menu Handlers

        private async void EditCategoryMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (CardsDataGrid.SelectedItem is not Card selectedCard)
                return;

            string currentCategory = selectedCard.Category ?? "";
            var categories = await _cardService.GetAllCategoriesAsync();
            
            var dialog = new InputDialog(
                $"Enter category for '{selectedCard.Name}':",
                currentCategory,
                categories);
            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                string newCategory = dialog.InputValue;
                if (newCategory != currentCategory)
                {
                    try
                    {
                        await _cardService.UpdateCardCategoryAsync(selectedCard.Id, newCategory);
                        
                        // Update local list
                        var card = _allCards.FirstOrDefault(c => c.Id == selectedCard.Id);
                        if (card != null)
                        {
                            card.Category = newCategory;
                        }

                        RefreshDataGrid();
                        await RefreshCategoryDropdownsAsync();
                        SetStatus($"Updated category for: {selectedCard.Name}");
                    }
                    catch (Exception ex)
                    {
                        SetStatus($"Error updating category: {ex.Message}");
                    }
                }
            }
        }

        private async void ClearCategoryMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (CardsDataGrid.SelectedItem is not Card selectedCard)
                return;

            if (string.IsNullOrEmpty(selectedCard.Category))
                return;

            try
            {
                await _cardService.UpdateCardCategoryAsync(selectedCard.Id, "");
                
                // Update local list
                var card = _allCards.FirstOrDefault(c => c.Id == selectedCard.Id);
                if (card != null)
                {
                    card.Category = "";
                }

                RefreshDataGrid();
                await RefreshCategoryDropdownsAsync();
                SetStatus($"Cleared category for: {selectedCard.Name}");
            }
            catch (Exception ex)
            {
                SetStatus($"Error clearing category: {ex.Message}");
            }
        }

        private async void EditAmountMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (CardsDataGrid.SelectedItem is not Card selectedCard)
                return;

            var dialog = new InputDialog(
                $"Enter quantity for '{selectedCard.Name}':",
                selectedCard.Amount.ToString(),
                new List<string>());
            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                if (int.TryParse(dialog.InputValue, out int newAmount) && newAmount >= 1)
                {
                    if (newAmount != selectedCard.Amount)
                    {
                        try
                        {
                            await _cardService.UpdateCardAmountAsync(selectedCard.Id, newAmount);
                            
                            // Update local list
                            var card = _allCards.FirstOrDefault(c => c.Id == selectedCard.Id);
                            if (card != null)
                            {
                                card.Amount = newAmount;
                            }

                            RefreshDataGrid();
                            SetStatus($"Updated quantity for: {selectedCard.Name} (x{newAmount})");
                        }
                        catch (Exception ex)
                        {
                            SetStatus($"Error updating quantity: {ex.Message}");
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Please enter a valid quantity (1 or more).",
                        "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private async void RefreshCardMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (CardsDataGrid.SelectedItem is Card selectedCard)
            {
                await RefreshCardAsync(selectedCard.Id);
            }
        }

        private void OpenTCGPlayerMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (CardsDataGrid.SelectedItem is Card selectedCard)
            {
                string url = $"https://www.tcgplayer.com/product/{selectedCard.Id}?Language=English";
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
        }

        private async void DeleteCardMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (CardsDataGrid.SelectedItem is Card selectedCard)
            {
                await DeleteCardAsync(selectedCard);
            }
        }

        #endregion

        #region Search and Sorting

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshDataGrid();
        }

        private void FilterCategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FilterCategoryComboBox.SelectedIndex == 0 || FilterCategoryComboBox.SelectedItem == null)
            {
                _selectedCategoryFilter = "";
            }
            else
            {
                _selectedCategoryFilter = FilterCategoryComboBox.SelectedItem.ToString() ?? "";
            }

            RefreshDataGrid();
        }

        // Track current sort state
        private DataGridColumn? _currentSortColumn;
        private ListSortDirection? _currentSortDirection;

        private void CardsDataGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            e.Handled = true;

            var column = e.Column;
            
            // Determine new sort direction
            ListSortDirection direction;
            if (_currentSortColumn == column && _currentSortDirection == ListSortDirection.Ascending)
            {
                direction = ListSortDirection.Descending;
            }
            else if (_currentSortColumn == column && _currentSortDirection == ListSortDirection.Descending)
            {
                direction = ListSortDirection.Ascending;
            }
            else
            {
                direction = ListSortDirection.Ascending;
            }

            // Store current sort state
            _currentSortColumn = column;
            _currentSortDirection = direction;

            // Apply sorting
            string sortBy = column.SortMemberPath;
            var filteredCards = ApplyFilter(_allCards);

            IOrderedEnumerable<Card> sorted = sortBy switch
            {
                "Id" => direction == ListSortDirection.Ascending 
                    ? filteredCards.OrderBy(c => c.Id) 
                    : filteredCards.OrderByDescending(c => c.Id),
                "Name" => direction == ListSortDirection.Ascending 
                    ? filteredCards.OrderBy(c => c.Name) 
                    : filteredCards.OrderByDescending(c => c.Name),
                "Category" => direction == ListSortDirection.Ascending 
                    ? filteredCards.OrderBy(c => c.Category) 
                    : filteredCards.OrderByDescending(c => c.Category),
                "Amount" => direction == ListSortDirection.Ascending 
                    ? filteredCards.OrderBy(c => c.Amount) 
                    : filteredCards.OrderByDescending(c => c.Amount),
                "MarketPrice" => direction == ListSortDirection.Ascending 
                    ? filteredCards.OrderBy(c => c.MarketPrice ?? decimal.MaxValue) 
                    : filteredCards.OrderByDescending(c => c.MarketPrice ?? decimal.MinValue),
                "TotalValue" => direction == ListSortDirection.Ascending 
                    ? filteredCards.OrderBy(c => c.TotalValue ?? decimal.MaxValue) 
                    : filteredCards.OrderByDescending(c => c.TotalValue ?? decimal.MinValue),
                "LatestSalePrice" => direction == ListSortDirection.Ascending 
                    ? filteredCards.OrderBy(c => c.LatestSalePrice ?? decimal.MaxValue) 
                    : filteredCards.OrderByDescending(c => c.LatestSalePrice ?? decimal.MinValue),
                "LatestSaleDate" => direction == ListSortDirection.Ascending 
                    ? filteredCards.OrderBy(c => c.LatestSaleDate ?? DateTime.MinValue) 
                    : filteredCards.OrderByDescending(c => c.LatestSaleDate ?? DateTime.MinValue),
                "LastUpdated" => direction == ListSortDirection.Ascending 
                    ? filteredCards.OrderBy(c => c.LastUpdated) 
                    : filteredCards.OrderByDescending(c => c.LastUpdated),
                _ => filteredCards.OrderBy(c => c.Id)
            };

            CardsDataGrid.ItemsSource = sorted.ToList();
            UpdateCardCount(filteredCards.Count);

            // Restore sort direction on columns after ItemsSource change
            foreach (var col in CardsDataGrid.Columns)
            {
                if (col == column)
                    col.SortDirection = direction;
                else
                    col.SortDirection = null;
            }
        }

        #endregion

        #region Helper Methods

        private async System.Threading.Tasks.Task RefreshCardAsync(int cardId)
        {
            try
            {
                SetButtonsEnabled(false);
                SetStatus($"Refreshing card {cardId}...");

                var refreshedCard = await _cardService.RefreshCardAsync(cardId);

                if (refreshedCard != null)
                {
                    var index = _allCards.FindIndex(c => c.Id == cardId);
                    if (index >= 0)
                    {
                        _allCards[index] = refreshedCard;
                    }

                    RefreshDataGrid();
                    SetStatus($"Refreshed card: {refreshedCard.Name}");
                }
                else
                {
                    SetStatus($"Could not refresh card {cardId}");
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Error: {ex.Message}");
            }
            finally
            {
                SetButtonsEnabled(true);
            }
        }

        private async System.Threading.Tasks.Task DeleteCardAsync(Card card)
        {
            var result = MessageBox.Show(
                $"Are you sure you want to delete '{card.Name}'?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                SetButtonsEnabled(false);
                await _cardService.DeleteCardAsync(card.Id);
                _allCards.RemoveAll(c => c.Id == card.Id);
                RefreshDataGrid();
                await RefreshCategoryDropdownsAsync();
                SetStatus($"Deleted card: {card.Name}");
            }
            catch (Exception ex)
            {
                SetStatus($"Error deleting card: {ex.Message}");
            }
            finally
            {
                SetButtonsEnabled(true);
            }
        }

        private void CardIdTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddCardButton_Click(sender, e);
            }
        }

        #endregion
    }
}
