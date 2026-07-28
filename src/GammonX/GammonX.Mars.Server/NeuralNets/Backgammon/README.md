### Trainings Data
|    Metric    |    Value     |
| ------------ | ------------ |
| prior gen    | gen7 vs. wild|
| sample size  | 200_000      |
| bootstrap    | 1            |
| completed    | 200_000      |
| discarded    | 0            |
| samples      | 10_491_690   |
| avg turns    | 57.7         |
| avg pred var | 0.12850      |
| duration     | 00:09:51:52  |

### Training Model
|    Metric    |    Value     |
| ------------ | ------------ |
| train data   | 7(4-6)-8     |
| train size   | 26_959_976   |
| val size     | 4_757_643    |
| train mean   | 0.5467       |
| train min    | 0            |
| train max    | 1            |
| t-near 0.5   | 0.0%         |
| val mean     | 0.5466       |
| val min      | 0            |
| val max      | 1            |
| v-near 0.5   | 0.0%         |
| e1 t-loss    | 0.22474      |
| e1 v-loss    | 0.21848      |
| e186 t-loss  | 0.21578      |
| e186 v-loss  | 0.21289      |
| val gap      | 0.00289      |

### Tournaments

#### Tournament Results gen8 vs. wildbg
|          Metric          |              Value                    |
| ------------------------ | ------------------------------------- |
| Model A                  | training_net (gen8)                   |
| Model B                  | wildbg                                |
| Modus                    | (from game)                           |
| Total games              | 5000                                  |
| Decisive                 | 5000                                  |
| Draws                    | 0                                     |
| Discarded                | 0                                     |
| Avg turns                | 56.6                                  |
| Model A wins             | 2655 (53.1%)                          |
| Model B wins             | 2345 (46.9%)                          |
| A win rate               | 53.10%                                |
| 95% CI                   | [51.71%, 54.48%]                      |
| Significance             | p<0.001 (z=4.38) - highly significant |
| Win rate last 10         | 53.09% ± 0.01%                        |
| Verdict                  | INCONCLUSIVE                          |

#### Tournament Results gen8 vs. gen7
|          Metric          |              Value                    |
| ------------------------ | ------------------------------------- |
| Model A                  | training_net (gen8)                   |
| Model B                  | training_net (gen7)                   |
| Modus                    | (from game)                           |
| Total games              | 5000                                  |
| Decisive                 | 5000                                  |
| Draws                    | 0                                     |
| Discarded                | 0                                     |
| Avg turns                | 57.4                                  |
| Model A wins             | 2693 (53.9%)                          |
| Model B wins             | 2307 (46.1%)                          |
| A win rate               | 53.86%                                |
| 95% CI                   | [52.48%, 55.24%]                      |
| Significance             | p<0.001 (z=5.46) - highly significant |
| Win rate last 10         | 53.89% ± 0.03%                        |
| Verdict                  | A is STRONGER (significant)           |

#### Tournament Results gen8 vs. gen3
|          Metric          |              Value                    |
| ------------------------ | ------------------------------------- |
| Model A                  | training_net (gen8)                   |
| Model B                  | training_net (gen3)                   |
| Modus                    | (from game)                           |
| Total games              | 5000                                  |
| Decisive                 | 5000                                  |
| Draws                    | 0                                     |
| Discarded                | 0                                     |
| Avg turns                | 60.3                                  |
| Model A wins             | 2730 (54.6%)                          |
| Model B wins             | 2270 (45.4%)                          |
| A win rate               | 54.60%                                |
| 95% CI                   | [53.22%, 55.98%]                      |
| Significance             | p<0.001 (z=6.51) - highly significant |
| Win rate last 10         | 54.60% ± 0.01%                        |
| Verdict                  | A is STRONGER (significant)           |