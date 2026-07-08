# Expected Results - `test/`

This small dataset exists so you can verify your reconciliation logic against a known answer before running the larger `data/` set. If your numbers don't match these, your matching or fee math is off somewhere.

**Input:** 12 internal transactions, 13 settlement records.

## Reconciliation summary

| Outcome                                            | Count |
| -------------------------------------------------- | ----- |
| Cleanly matched (6 sales + 2 refunds)              | 8     |
| Unmatched - internal (in ledger, never settled)    | 1     |
| Unmatched - settlement (settled, no ledger record) | 1     |
| Amount mismatch (principal off beyond rounding)    | 1     |
| Fee discrepancy (fees deviate from schedule)       | 1     |
| Duplicate settlement (one payment settled twice)   | 1     |

Notes:

- The duplicate produces **two** settlement rows for **one** internal transaction - which is why there are 13 settlement records but only 8 clean matches plus the breaks.
- The **fee discrepancy** row is deliberately tricky: its `settled_amount` is internally consistent with the fees the processor _reported_, so a check that only compares `settled_amount` against `gross − reported_fees` will miss it. You have to compare the reported fees against the **published schedule** (`fee_schedule.json`) to catch it.
- Every transaction here is in the normal settlement window. The curveballs (wide date windows, split settlements) live only in `data/`.
