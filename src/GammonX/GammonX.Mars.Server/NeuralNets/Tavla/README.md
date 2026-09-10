## TD(0.95) 800k gen11 1.5-ply
- batch size 16_384
- learning rate 1e-4
- 68k replay game sample from gen10
- selective 2-ply candidate count 3 for wildbg games
- selective 2-ply candidate count 5 for self play games

### Trainings Data self play selective 1.5-ply (1)
|    Metric    |    Value     |
| ------------ | ------------ |
| prior gen    | gen10        |
| sample size  | 264_000      |
| lambda       | 1            |
| gamma        | 1            |
| small s gap  | 0.01         |
| small multi  | 1.5          |
| big s gap    | 0.2          |
| big multi    | 0.5          |
| completed    | 264_000      |
| discarded    | 0            |
| samples      | 16_216_917   |
| avg turns    | 61.4         |
| avg pred var | 0.10440      |
| duration     | 00:07:04:46  |

### Trainings Data self play selective 1.5-ply (2)
|    Metric    |    Value     |
| ------------ | ------------ |
| prior gen    | gen10        |
| sample size  | 264_000      |
| lambda       | 1            |
| gamma        | 1            |
| small s gap  | 0.01         |
| small multi  | 1.5          |
| big s gap    | 0.2          |
| big multi    | 0.5          |
| completed    | 264_000      |
| discarded    | 0            |
| samples      | 16_216_917   |
| avg turns    | 61.4         |
| avg pred var | 0.10440      |
| duration     | 00:07:04:46  |

### Trainings Data wildbg selective 1.5-ply
|    Metric    |    Value     |
| ------------ | ------------ |
| prior gen    | gen10        |
| sample size  | 204_000      |
| lambda       | 1            |
| gamma        | 1            |
| small s gap  | 0.01         |
| small multi  | 1.5          |
| big s gap    | 0.2          |
| big multi    | 0.5          |
| completed    | 204_000      |
| discarded    | 0            |
| samples      | 11_688_470   |
| avg turns    | 57.3         |
| avg pred var | 0.10635      |
| duration     | 00:15:45:13  |

### Training Model
|    Metric    |    Value     |
| ------------ | ------------ |
| train data   | gen 11       |
| train size   | 41_005_776   |
| val size     | 4_830_495    |
| train mean   | 0.5043       |
| train min    | 0            |
| train max    | 1            |
| t-near 0.5   | 5.0%         |
| val mean     | 0.5043       |
| val min      | 0            |
| val max      | 1            |
| v-near 0.5   | 5.0%         |
| parent v-loss| 0.22931      |
| e1 t-loss    | 0.22994      |
| e1 v-loss    | 0.22864      |
| e50  t-loss  | 0.22934      |
| e50  v-loss  | 0.22825      |
| val gap      | 0.00109      |

### Tournaments

===========================================
  gen11 2-ply vs. wildbg
===========================================
  Model A : training_net.td095.gen11
  Model B : wildbg
  Modus   : (from game)
  Total games  : 5000
  Decisive     : 4995
  Draws        : 0
  Discarded    : 5
  Avg turns    : 54.9
  Model A wins : 2509 (50.2%)
  Model B wins : 2486 (49.8%)
  A win rate   : 50.23%
  95% CI       : [48.84%, 51.62%]
  Significance : p>0.10  (z=0.33) ? not significant
  Win rate last 10 checkpoints: 50.24% ? 0.02%
  Verdict: EQUIVALENT (within noise).
===========================================

===========================================
  gen11 1-ply vs. wildbg
===========================================
  Model A : training_net.td095.gen11
  Model B : wildbg
  Modus   : (from game)
  Total games  : 10000
  Decisive     : 10000
  Draws        : 0
  Discarded    : 0
  Avg turns    : 55.5
  Model A wins : 4689 (46.9%)
  Model B wins : 5311 (53.1%)
  A win rate   : 46.89%
  95% CI       : [45.91%, 47.87%]
  Significance : p<0.001 (z=6.22) ? highly significant
  Win rate last 10 checkpoints: 46.89% ? 0.01%
  Verdict: B is STRONGER (significant).
===========================================

===========================================
  gen11 1-ply vs. gen10 1-ply
===========================================
  Model A : training_net.td095.gen11
  Model B : training_net.td095.gen10
  Modus   : (from game)
  Total games  : 10000
  Decisive     : 10000
  Draws        : 0
  Discarded    : 0
  Avg turns    : 54.6
  Model A wins : 5264 (52.6%)
  Model B wins : 4736 (47.4%)
  A win rate   : 52.64%
  95% CI       : [51.66%, 53.62%]
  Significance : p<0.001 (z=5.28) ? highly significant
  Win rate last 10 checkpoints: 52.63% ? 0.01%
  Verdict: INCONCLUSIVE.
===========================================

===========================================
  gen11 1-ply vs. gen9-2 1-ply
===========================================
  Model A : training_net.td095.gen11
  Model B : training_net.td095.gen9-2
  Modus   : (from game)
  Total games  : 10000
  Decisive     : 10000
  Draws        : 0
  Discarded    : 0
  Avg turns    : 55.8
  Model A wins : 5697 (57.0%)
  Model B wins : 4303 (43.0%)
  A win rate   : 56.97%
  95% CI       : [56.00%, 57.94%]
  Significance : p<0.001 (z=13.94) ? highly significant
  Win rate last 10 checkpoints: 56.95% ? 0.01%
  Verdict: A is STRONGER (significant).
===========================================