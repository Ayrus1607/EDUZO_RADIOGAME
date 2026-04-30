# Data Handling Game

Teacher inputs category data → Student scans QR flashcards to answer data representation questions.

## Flow
Form → Mode Selection → Player Data → Gameplay → Score Screen

## Modes
- **Practice Mode**: No timer, no lives, unlimited attempts
- **Test Mode**: Countdown timer, 3 lives, score tracking

## Visualization Types
- **Bar Graph** (Mode 0) — Dynamic bar chart with chalk colors
- **Pie Chart** (Mode 1) — Percentage-based pie chart with legends
- **Tally Chart** (Mode 2) — Tally marks grouped in bundles of 5
- **Look & Count** (Mode 3) — Custom image-based counting

## Scripts
- `DataGameManager.cs` — Core game loop, QR scanning integration, scoring
- `DataFormController.cs` — Teacher data input form with validation
- `BarGraphManager.cs` — Dynamic bar chart generation
- `PieChartManager.cs` — Pie chart slice/legend generation
- `TallyMarkManager.cs` — Tally mark row generation
- `DataTableManager.cs` — Data table display
