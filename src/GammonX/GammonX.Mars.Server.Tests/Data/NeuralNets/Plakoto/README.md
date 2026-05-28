### Trainings Data|
    Metric    |    Value     |
| ------------ | ------------ |
| prior gen    | gen5         |
| sample size  | 20_000       |
| bootstrap    | 0.85         |
| completed    | 19_985       |
| discarded    | 15           |
| samples      | 1_612_347    |
| avg turns    | 97.5         |
| avg pred var | 0.09896      |
| duration     | 00:03:24:54  |

### Training Model
|    Metric    |    Value     |
| ------------ | ------------ |
| train data   | 5-6          |
| train size   | 3_210_713    |
| val size     | 566_597      |
| train mean   | 0.5348       |
| train min    | 0            |
| train max    | 1            |
| t-near 0.5   | 9.8%         |
| val mean     | 0.5349       |
| val min      | 0            |
| val max      | 1            |
| v-near 0.5   | 9.8%         |
| e1 t-loss    | 0.47618      |
| e1 v-loss    | 0.45370      |
| e188 t-loss  | 0.45035      |
| e188 v-loss  | 0.44782      |
| val gap      | 0.00253      |

### Tournament

===========================================
  Tournament Results gen6 vs. gen5
===========================================
  Model A : training_net (gen6)
  Model B : training_net (gen5)
  Modus   : (from game)
  Total games  : 1000
  Decisive     : 1000
  Draws        : 0
  Discarded    : 0
  Avg turns    : 96.8
  Model A wins : 561 (56.1%)
  Model B wins : 439 (43.9%)
  A win rate   : 56.10%
  95% CI       : [53.01%, 59.15%]
  Significance : p<0.001 (z=3.86) - highly significant
  Win rate last 10 checkpoints: 56.10% ± 0.05%
  Verdict: A is STRONGER (significant).
===========================================

===========================================
  Tournament Results gen6 vs. gen4
===========================================
  Model A : training_net (gen6)
  Model B : training_net (gen4)
  Modus   : (from game)
  Total games  : 1000
  Decisive     : 1000
  Draws        : 0
  Discarded    : 0
  Avg turns    : 97.0
  Model A wins : 605 (60.5%)
  Model B wins : 395 (39.5%)
  A win rate   : 60.50%
  95% CI       : [57.44%, 63.48%]
  Significance : p<0.001 (z=6.64) - highly significant
  Win rate last 10 checkpoints: 60.61% ± 0.06%

===========================================
  Tournament Results gen6 vs. gen0
===========================================
  Model A : training_net (gen6)
  Model B : training_net (gen0)
  Modus   : (from game)
  Total games  : 1000
  Decisive     : 1000
  Draws        : 0
  Discarded    : 0
  Avg turns    : 95.8
  Model A wins : 874 (87.4%)
  Model B wins : 126 (12.6%)
  A win rate   : 87.40%
  95% CI       : [85.20%, 89.31%]
  Significance : p<0.001 (z=23.65) - highly significant
  Win rate last 10 checkpoints: 87.39% ± 0.03%
  Verdict: A is STRONGER (significant).
===========================================