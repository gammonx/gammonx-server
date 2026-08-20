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

Choose a number at the `Select mode:` prompt and follow the prompts for paths, model settings, and other options. Most modes read or write CSV files in the current working directory unless another path is provided.
