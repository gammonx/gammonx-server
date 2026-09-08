## TD(0.95) 300k gen10 1.5-ply
- batch size 16_384
- learning rate 1e-4
- 25k replay game sample
- selective 2-ply candidate count 3

### Trainings Data self play selective 1.5-ply
|    Metric    |    Value     |
| ------------ | ------------ |
| prior gen    | gen9         |
| sample size  | 200_000      |
| lambda       | 0.95         |
| gamma        | 1            |
| small s gap  | 0.01         |
| small multi  | 1.5          |
| big s gap    | 0.2          |
| big multi    | 0.5          |
| completed    | 199_993      |
| discarded    | 7            |
| samples      | 12_393_503   |
| avg turns    | 62.0         |
| avg pred var | 0.10076      |
| duration     | ?  |

### Trainings Data wildbg selective 1.5-ply
|    Metric    |    Value     |
| ------------ | ------------ |
| prior gen    | gen9         |
| sample size  | 75_000       |
| lambda       | 0.95         |
| gamma        | 1            |
| small s gap  | 0.01         |
| small multi  | 1.5          |
| big s gap    | 0.2          |
| big multi    | 0.5          |
| completed    | 75_000       |
| discarded    | 0            |
| samples      | 4_300_139    |
| avg turns    | 57.3         |
| avg pred var | 0.10222      |
| duration     | 00:04:45:20  |

### Training Model
|    Metric    |    Value     |
| ------------ | ------------ |
| train data   | gen10        |
| train size   | 15_515_867   |
| val size     | 2_744_056    |
| train mean   | 0.5042       |
| train min    | 0            |
| train max    | 1            |
| t-near 0.5   | 5.1%         |
| val mean     | 0.5042       |
| val min      | 0            |
| val max      | 1            |
| v-near 0.5   | 5.1%         |
| parent v-loss| 0.24445      |
| e1 t-loss    | 0.24261      |
| e1 v-loss    | 0.24285      |
| e200 t-loss  | 0.24129      |
| e200 v-loss  | 0.24207      |
| val gap      | 0.00078      |

### Tournaments

===========================================
  gen10 2-ply vs. wildbg
===========================================
  Model A : training_net.td095.gen10
  Model B : wildbg
  Modus   : (from game)
  Total games  : 5000
  Decisive     : 5000
  Draws        : 0
  Discarded    : 0
  Avg turns    : 55.1
  Model A wins : 2396 (47.9%)
  Model B wins : 2604 (52.1%)
  A win rate   : 47.92%
  95% CI       : [46.54%, 49.31%]
  Significance : p<0.01  (z=2.94) ? significant
  Win rate last 10 checkpoints: 47.94% ? 0.01%
  Verdict: INCONCLUSIVE.
===========================================

===========================================
  gen10 1-ply vs. wildbg
===========================================
  Model A : training_net.td095.gen10
  Model B : wildbg
  Modus   : (from game)
  Total games  : 10000
  Decisive     : 10000
  Draws        : 0
  Discarded    : 0
  Avg turns    : 54.9
  Model A wins : 4481 (44.8%)
  Model B wins : 5519 (55.2%)
  A win rate   : 44.81%
  95% CI       : [43.84%, 45.79%]
  Significance : p<0.001 (z=10.38) ? highly significant
  Win rate last 10 checkpoints: 44.80% ? 0.01%
  Verdict: B is STRONGER (significant).
===========================================

===========================================
  gen10 1-ply vs. gen9-2 1-play
===========================================
  Model A : training_net.td095.gen10
  Model B : training_net.td095.gen9-2
  Modus   : (from game)
  Total games  : 10000
  Decisive     : 10000
  Draws        : 0
  Discarded    : 0
  Avg turns    : 55.4
  Model A wins : 5596 (56.0%)
  Model B wins : 4404 (44.0%)
  A win rate   : 55.96%
  95% CI       : [54.98%, 56.93%]
  Significance : p<0.001 (z=11.92) ? highly significant
  Win rate last 10 checkpoints: 55.96% ? 0.00%
  Verdict: A is STRONGER (significant).
===========================================

===========================================
  gen10 1-ply vs. gen7 1-ply
===========================================
  Model A : training_net.td095.gen10
  Model B : training_net.td095.gen7
  Modus   : (from game)
  Total games  : 10000
  Decisive     : 10000
  Draws        : 0
  Discarded    : 0
  Avg turns    : 56.3
  Model A wins : 5526 (55.3%)
  Model B wins : 4474 (44.7%)
  A win rate   : 55.26%
  95% CI       : [54.28%, 56.23%]
  Significance : p<0.001 (z=10.52) ? highly significant
  Win rate last 10 checkpoints: 55.26% ? 0.01%
  Verdict: A is STRONGER (significant).
===========================================