# Expected Results - `test/`

This small dataset is a **known good day**: a hand-verified reconciliation you can check your engine against before running the larger `data/` set. It exercises every category the exercise defines, so if you reproduce this table exactly, your pipeline handles the full problem. If your numbers don't match, your matching or fee math is off somewhere.

**Input:** 18 internal transactions, 19 settlement records. Five of those rows are deliberately malformed and must be quarantined, not reconciled - they are not breaks and don't appear in any count below.

## Reconciliation summary

| Outcome                                                       | Count |
| ------------------------------------------------------------- | ----- |
| Cleanly matched (6 sales + 2 refunds)                         | 8     |
| Unmatched - internal (in ledger, never settled)               | 1     |
| Unmatched - settlement (settled, no ledger record)            | 1     |
| Amount mismatch (principal off beyond rounding)               | 1     |
| Fee discrepancy (fees deviate from schedule)                  | 1     |
| Duplicate settlement (one payment settled twice)              | 1     |
| Orphan refund (refund with no originating sale)               | 1     |
| Split settlement (one capture settled in two parts) _(bonus)_ | 1     |
| Wide-window timing (matched, but settled far outside T+1..3)  | 1     |
| Malformed - quarantined (3 internal + 2 settlement)           | 5     |

Row accounting: 18 internal = 6 clean sales + 2 clean refunds + 1 each of unmatched-internal, amount-mismatch, fee-discrepancy, duplicate, orphan-refund, wide-timing, split + 3 malformed. 19 settlement = 6 clean sales + 2 clean refunds + 1 unmatched-settlement + 1 amount + 1 fee + 2 duplicate + 1 orphan + 1 wide-timing + 2 split + 2 malformed.

## Money checks

Raw sums over the **valid** rows (the 5 malformed rows excluded). These confirm your ingest and fee math independently of how you model breaks:

| Check                                         | Value      |
| --------------------------------------------- | ---------- |
| Total gross (valid sales)                     | `6804.12`  |
| Total refund gross (internal)                 | `-1557.02` |
| Total settled (sum of all settlement rows)    | `5161.00`  |
| Total fees deducted (interchange + processor) | `151.74`   |

## Reading the table

- The **wide-window timing** row is a fully matched pair - correct amount, correct fees - that simply settled outside the usual window. It's counted on its own line, not as a clean match, which is why "cleanly matched" is 6 sales rather than 7; whether you ultimately treat it as matched or flag it is your call.
- The five **malformed** rows are quarantined, not breaks, and are excluded from every count and money check above.
