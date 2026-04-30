# Radio Game

Teacher creates True/False questions → Student picks A/B on a radio-themed UI.

## Flow
Form → Mode Selection → Player Data → Gameplay → Score Screen

## Modes
- **Practice Mode**: No timer, no lives, unlimited attempts
- **Test Mode**: Countdown timer, 3 lives, score tracking

## Scripts
- `RadioGameManager.cs` — Core game loop, state machine, scoring
- `RadioFormController.cs` — Teacher question input form
- `RadioQuestionData.cs` — Data model for a single question
- `RadioPulseEffect.cs` — UI breathing/pulse animation
