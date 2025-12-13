  ## Backgammon Match Start Use Case

  When `exclude_doubles: true`, this function is specifically designed for Backgammon
  match initialization where:
  - First dice value determines the host player's opening move
  - Second dice value determines the guest player's opening move
  - Both players must have different opening moves (no doubles allowed)
  - Ensures fair and varied opening moves for both players


Backgammon/Portes/Tavla:
- Each player rolls one die.
- Higher die goes first.
- The two dice (yours + opponent’s) become the first move.
- If tied, reroll.

Plakoto/Fevga
- Each player rolls two dice and only the player who rolls higher total plays.
- The player who starts uses their own dice only (not both players’ dice combined).
- No reroll unless totals are identical.