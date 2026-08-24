## TD 800k mono (lambda=0.90) gen16

#### 300k game sample based on gen15-2 self play
- Output constraints: rows=19'383'185 invalid=0 (0.00%) rules=0 avgSeverity=0.00000 maxSeverity=0.00000
- Avg pred variance: 0.10134

#### 200k game sample based on gen15-2 vs. wildbg
- Output constraints: rows=11'685'576 invalid=0 (0.00%) rules=0 avgSeverity=0.00000 maxSeverity=0.00000
- Avg pred variance: 0.09939

#### 100k game sample based on gen15-2 vs. gen15-1
- Output constraints: rows=6'050'334 invalid=0 (0.00%) rules=0 avgSeverity=0.00000 maxSeverity=0.00000
- Avg pred variance: 0.10216

#### 100k game sample based on gen15-2 vs. gen14 TD 400k
- Output constraints: rows=6'113'698 invalid=0 (0.00%) rules=0 avgSeverity=0.00000 maxSeverity=0.00000
- Avg pred variance: 0.10166

#### 50k game sample based on gen15-2 vs. gen14 MC 800k
- Output constraints: rows=3'068'166 invalid=0 (0.00%) rules=0 avgSeverity=0.00000 maxSeverity=0.00000
- Avg pred variance: 0.09913

#### 50k game sample based on gen15-2 vs. gen13 MC 400k
- Output constraints: rows=3'041'728 invalid=0 (0.00%) rules=0 avgSeverity=0.00000 maxSeverity=0.00000
- Avg pred variance: 0.10070

### Trainings Data
|    Metric    |    Value     |
| ------------ | ------------ |
| prior gen    | see above    |
| sample size  | 800_000      |
| lambda       | 0.90         |
| gamma        | 1            |
| small s gap  | 0.02         |
| big s gap    | 0.2          |
| completed    | -            |
| discarded    | -            |
| samples      | 49_342_687   |
| avg turns    | -            |
| avg pred var | -            |
| duration     | -            |

### Training Model
- larger network architecture (B)

|    Metric    |    Value     |
| ------------ | ------------ |
| train data   | above        |
| train size   | 41_941_283   |
| val size     | 7_401_404    |
| train mean   | 0.5040       |
| train min    | 0            |
| train max    | 1            |
| t-near 0.5   | 8%           |
| val mean     | 0.5040       |
| val min      | 0            |
| val max      | 1            |
| v-near 0.5   | 8%           |
| e1 t-loss    | -            |
| e1 v-loss    | 0.25600      |
| e80  t-loss  | -            |
| e80  v-loss  | 0.25200      |
| val gap      | -            |

### Tournaments

===========================================
  TD(0.9) 800k gen16 vs wildbg
===========================================
  Model A : training_net.td09.800k (gen16)
  Model B : wildbg
  Modus   : (from game)
  Total games  : 5000
  Decisive     : 5000
  Draws        : 0
  Discarded    : 0
  Avg turns    : 55.4
  Model A wins : 1962 (39.2%)
  Model B wins : 3038 (60.8%)
  A win rate   : 39.24%
  95% CI       : [37.90%, 40.60%]
  Significance : p<0.001 (z=15.22) ? highly significant
  Win rate last 10 checkpoints: 39.22% ? 0.01%
  Verdict: B is STRONGER (significant).
===========================================

===========================================
  TD(0.9) 800k gen16 vs TD(0.9) 400k gen15-2
===========================================
  Model A : training_net.td09.800k
  Model B : training_net.td09.400k.gen15-2
  Modus   : (from game)
  Total games  : 10000
  Decisive     : 10000
  Draws        : 0
  Discarded    : 0
  Avg turns    : 57.1
  Model A wins : 5157 (51.6%)
  Model B wins : 4843 (48.4%)
  A win rate   : 51.57%
  95% CI       : [50.59%, 52.55%]
  Significance : p<0.01  (z=3.14) ? significant
  Win rate last 10 checkpoints: 51.58% ? 0.01%
  Verdict: INCONCLUSIVE.
===========================================

===========================================
  TD(0.9) 800k gen16 vs. TD(0.9) 400k gen15-1
===========================================
  Model A : training_net.td09.800k (gen16)
  Model B : training_net.td09.400k (gen15-1)
  Modus   : (from game)
  Total games  : 5000
  Decisive     : 5000
  Draws        : 0
  Discarded    : 0
  Avg turns    : 56.8
  Model A wins : 2575 (51.5%)
  Model B wins : 2425 (48.5%)
  A win rate   : 51.50%
  95% CI       : [50.11%, 52.88%]
  Significance : p<0.05  (z=2.12) ? significant
  Win rate last 10 checkpoints: 51.51% ? 0.01%
  Verdict: INCONCLUSIVE.
===========================================

===========================================
  TD(0.9) 800k gen16 vs. TD 400k gen14
===========================================
  Model A : training_net.td09.800k (gen16)
  Model B : training_net.td095.400k (gen14)
  Modus   : (from game)
  Total games  : 5000
  Decisive     : 5000
  Draws        : 0
  Discarded    : 0
  Avg turns    : 57.3
  Model A wins : 2542 (50.8%)
  Model B wins : 2458 (49.2%)
  A win rate   : 50.84%
  95% CI       : [49.45%, 52.22%]
  Significance : p>0.10  (z=1.19) ? not significant
  Win rate last 10 checkpoints: 50.87% ? 0.01%
  Verdict: INCONCLUSIVE.
===========================================

===========================================
  TD(0.9) 800k gen16 vs. MC 800k gen14
===========================================
  Model A : training_net.td09.800k (gen16)
  Model B : training_net.mc.800k (gen14)
  Modus   : (from game)
  Total games  : 5000
  Decisive     : 5000
  Draws        : 0
  Discarded    : 0
  Avg turns    : 57.7
  Model A wins : 2647 (52.9%)
  Model B wins : 2353 (47.1%)
  A win rate   : 52.94%
  95% CI       : [51.55%, 54.32%]
  Significance : p<0.001 (z=4.16) ? highly significant
  Win rate last 10 checkpoints: 52.94% ? 0.01%
  Verdict: INCONCLUSIVE.
===========================================

===========================================
  TD(0.9) 800k gen16 vs. MC 400k gen13
===========================================
  Model A : training_net.td09.800k (gen16)
  Model B : training_net.mc.400k (gen13)
  Modus   : (from game)
  Total games  : 5000
  Decisive     : 5000
  Draws        : 0
  Discarded    : 0
  Avg turns    : 57.5
  Model A wins : 2688 (53.8%)
  Model B wins : 2312 (46.2%)
  A win rate   : 53.76%
  95% CI       : [52.38%, 55.14%]
  Significance : p<0.001 (z=5.32) ? highly significant
  Win rate last 10 checkpoints: 53.75% ? 0.01%
  Verdict: A is STRONGER (significant).
===========================================