# GammonX Mars Training Console

The console application provides tools for generating data, training models, and analyzing training results. Select a mode when the application starts:

1. **Generate Training Data** – Runs self-play games using the selected game variant and evaluation services. The positions encountered during the games are saved as training rows, together with labels and optional trajectory, game, exploration, and constraint-metric sidecar files.

2. **Train Mode** – Trains a neural network from an existing training dataset. The prompts select the input data, model configuration, training parameters, and output location for the saved model.

3. **Shuffle Mode** – Randomizes the order of training rows. Use this to remove ordering effects before training while keeping the corresponding data rows together.

4. **Noise Floor Mode** – Evaluates repeated or comparable positions to estimate the model's prediction noise. This helps distinguish meaningful prediction differences from variation caused by evaluation noise.

5. **Tournament Mode** – Runs games or a tournament between trained models. Use it to compare model strength through match results rather than only through training loss.

6. **Tournament Mode against wildbg** – Runs the selected model against GNU Backgammon, referenced by the application as `wildbg`. This provides an external playing-strength comparison.

7. **Rebuild TD targets** – Recalculates temporal-difference targets from training rows, trajectory data, and game metadata. This is useful when changing TD-lambda, discount gamma, or the number of output heads without generating new games.

8. **Select random replay games** – Creates a smaller dataset by randomly selecting complete games from replay or training data. The related position, trajectory, and game-metadata files are copied together so the selected games remain consistent.

9. **Analyze score-gap diagnostics** – Reads exploration diagnostics and summarizes the difference between the best and second-best candidate scores. It also reports score-gap percentiles, exploration choices, and groups results by game mode, game phase, opponent, and candidate count.

10. **Audit trajectory output constraints** – Checks whether recorded trajectory predictions satisfy the expected probability ranges and hierarchy rules. Optionally evaluates a current model on the same positions to compare source-model and current-model constraint violations.

## Model metadata

Models own their configuration through `INetModel.Metadata`. `Save(path)` writes the weights
and a `<path>.meta.json` sidecar together. Metadata includes the model filename (with its
extension), game mode, architecture, output semantics, and any configured training inputs
and settings. Saving under a different filename updates the metadata name.

Configure a run with `model.ConfigureTraining(new NetTrainingMetadata(...))`, then call
`NetTrainer.Train(model)`. For continued training, load the parent with
`NetModelFactory.CreateForModel(...)` first and configure the new run's paths and settings.
The loaded architecture and output semantics are preserved. Training paths in previously
saved metadata describe that earlier run; inference does not require those files.

Version 1 sidecars and legacy models without sidecars remain supported. Missing historical
training settings remain unknown, and the next save writes the current metadata version.
The `.dat` weight format is unchanged. Keep the weights and their sidecar together when
copying or embedding a model. A save stages both files before replacing them, but the pair
cannot be replaced atomically as a single filesystem operation.

## Tournament evaluation

Both tournament modes measure actual cubeless points per game from model A's perspective: singles are worth +/-1, gammons +/-2, backgammons +/-3, and genuine draws 0. Outcome types determine cubeless scores, independently of the doubling cube. The report leads with average points and an approximate 95% confidence interval. Win counts and decisive-game win rate remain secondary information.

To compare a candidate with the champion, select the candidate as A and the champion as B. A is reported as stronger only when the entire point-equity interval is above zero; B is stronger when it is entirely below zero. Otherwise the result is inconclusive. A higher win rate alone cannot establish superiority, and an inconclusive result does not establish equivalence. Promotion remains a manual decision.

The interval uses the sample standard error and a large-sample normal approximation with multiplier 1.96. At least 100 completed games and nonzero observed score variance are required to report an interval; this minimum does not guarantee adequate power. Genuine draws count as scored games, while unfinished or failed games are discarded. Any discarded or unprocessed games make the promotion verdict inconclusive; completed-game statistics remain diagnostic.

Choose the tournament size before starting and use its final report for the decision. Progress estimates are informational: repeatedly extending a run until its interval excludes zero does not preserve the stated confidence level. This interval assumes independent games; paired dice and sequential stopping need different analysis.

## Search-assisted TD

Each recorded afterstate stores one outcome prediction: the played move's two-ply evaluation when searched, otherwise its one-ply evaluation. If no prediction is available, the recorder evaluates Model A directly. Model B supplies moves in dual-model games; Model A remains the teacher for those positions.

All labels use ordinary TD(lambda), blending recorded future predictions with the final game result. Search improves those bootstrap estimates; the current position's prediction does not directly replace its label. With lambda = 1, labels depend only on terminal outcomes. Use lambda below one for searched predictions to affect labels.

Trajectory sidecars store one prediction vector in the nine-column format. Older dual-prediction sidecars remain readable: their searched vector is used when present, otherwise their raw vector. Rebuilding applies TD(lambda) to that single recorded prediction. A raw-only comparison requires re-evaluating the teacher or generating one-ply data; no separate raw vector or search-assistance flag is retained. Prediction variance and constraint diagnostics describe the recorded evaluations.

## Validation Splits

Training-data generation and shuffle mode assign complete games to training or validation with a stable hash of the game ID and validation seed. The default seed is `17`. Reusing the seed keeps an existing game's partition unchanged when files are reordered, merged, or extended with new games.

The requested 85/15 ratio is probabilistic, so exact row counts can vary. This creates stable membership, not a frozen cross-generation benchmark: newly generated games still add new validation positions. Use a separately retained validation dataset when candidates from different generations must be compared against exactly the same games.

Choose a number at the `Select mode:` prompt and follow the prompts for paths, model settings, and other options. Most modes read or write CSV files in the current working directory unless another path is provided.
