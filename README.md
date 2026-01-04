# CardManager

A Windows C# desktop application for managing and tracking TCGPlayer card information.

<img width="1422" height="805" alt="image" src="https://github.com/user-attachments/assets/05009f99-e582-46ff-acf0-2958db302fc5" />


## Features

- **Add Cards by ID**: Enter a TCGPlayer product ID to fetch and store card information
- **Price Tracking**: View current market prices for your cards
- **Search & Filter**: Quickly find cards by name or ID
- **Sorting**: Sort by ID, Name, Price, or Last Updated date
- **Dark/Light Theme**: Toggle between dark and light modes
- **Refresh Data**: Update prices for individual cards or all cards at once
- **Persistent Storage**: Cards are saved locally in a JSON file

## Requirements

- Windows 10 or later
- .NET 8.0 Runtime

## Usage

1. **Add a Card**: Enter a card ID in the "Card ID" field and click "Add Card"
   - Example: Enter `646522` for a specific card from https://www.tcgplayer.com/product/646522

2. **Search Cards**: Type in the search box to filter by card name or ID

3. **Refresh Prices**: 
   - Click "Refresh All" to update all card prices
   - Select a card and click "Refresh Selected" to update one card

4. **Delete Cards**: Right-click on a card and select "Delete Card"

5. **Open on TCGPlayer**: Right-click a card and select "Open on TCGPlayer" to view it on the website

6. **Change Theme**: Click the theme toggle button in the top-right corner

## Building from Source

```bash
dotnet build
dotnet run --project CardManager
```

## Data Storage

Card data is stored in `cards.json` in the application directory.

## Dependencies

- HtmlAgilityPack (for HTML parsing)
- .NET 8.0 WPF framework

