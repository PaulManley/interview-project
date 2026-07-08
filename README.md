# Settlement Reconciliation

Thanks for your interest in the role. This is a take-home exercise meant to be done in **your own environment, on your own schedule** - we've found that gives you the best chance to show how you actually work. Plan for roughly **3–5 hours**. It is not a race, and it is not meant to be gold-plated; we care far more about the quality of what you build than the quantity.

You'll build a small full-stack app. There will be a follow up conversation where you walk us through your code and your decisions, so **be sure the end product is something you can explain**, not just checks every box.

---

## The Setup

We're a payments company. When we run a customer's card, two systems end up with a record of that same money - and they never agree cleanly:

- **Our internal ledger** - what _our_ system believes happened the moment we captured the payment: the merchant, the card, the **gross** amount.
- **The processor's settlement file** - what the card networks and our processor _actually settled_ a day or two later, and what they'll pay out: a **net** amount, **after** interchange and processor fees are deducted.

Reconciliation is the daily job of matching those two sides against each other and surfacing everything that _doesn't_ line up - money we're owed but never received, amounts that don't match, fees we may have been overcharged, things settled that we have no record of. It's core payments work, and getting it right is the exercise.

---

## What You'll Build

A web application that ingests both files, reconciles them, and reports the results:

1. **Import** the two provided files (formats differ - see below).
2. **Reconcile** internal transactions against settlement records.
3. **Persist** the results so they survive a restart.
4. **Report** a reconciliation summary and a drill-down list of every break (mismatch).

Implement it however you like. That said - see [Stack](#stack) for what would fit our team best.

---

## The Data

Two datasets are provided:

| Directory | Purpose                                                                                                                   |
| --------- | ------------------------------------------------------------------------------------------------------------------------- |
| `test/`   | Small, hand-verifiable. `test/EXPECTED.md` gives the correct break counts so you can check your matching logic as you go. |
| `data/`   | Larger, realistic set with breaks of every category mixed in. This is what a "real" day looks like.                       |

**The formats are intentionally different**, as they are in real life - you're integrating two systems that were never designed to talk to each other:

### `internal_transactions.csv` - our ledger (CSV)

| Column            | Notes                                                                           |
| ----------------- | ------------------------------------------------------------------------------- |
| `internal_txn_id` | Our primary key. **Does not appear anywhere in the settlement file.**           |
| `merchant_id`     | e.g. `MERCH-004`                                                                |
| `merchant_ref`    | Our order reference. The processor _usually_ echoes this back - but not always. |
| `card_type`       | `VISA`, `MASTERCARD`, `AMEX`, `DISCOVER`                                        |
| `card_last4`      | Last four of the card                                                           |
| `gross_amount`    | The full amount, **before fees**. Negative for refunds.                         |
| `currency`        | `USD`                                                                           |
| `type`            | `SALE` or `REFUND`                                                              |
| `captured_at`     | ISO 8601, when we captured the payment                                          |

### `processor_settlement.json` - the processor (JSON array)

| Field                                    | Notes                                                                         |
| ---------------------------------------- | ----------------------------------------------------------------------------- |
| `network_ref`                            | The network's reference (ARN). Unrelated to our IDs.                          |
| `merchant_ref`                           | Our order reference, _echoed back_ - **but sometimes blank**.                 |
| `merchant_id`, `card_last4`, `card_type` | As reported by the processor                                                  |
| `settled_amount`                         | The **net** amount actually settled (gross minus fees). Negative for refunds. |
| `interchange_fee`, `processor_fee`       | The fees the processor deducted                                               |
| `currency`                               |                                                                               |
| `settlement_date`                        | The date it settled - **typically 1–3 days after** `captured_at`              |

> **The central challenge:** there is no shared primary key. `internal_txn_id` never appears on the settlement side, and `merchant_ref` is your best link but isn't always present. Deciding **how to match the two sides** is the heart of this exercise.

---

## The Fee Rule

A settled amount will **not** equal the gross amount - fees come out first. To know whether a settlement is _correct_, you compute the fees you expect from the published schedule and compare.

For a **SALE** of `gross` on a given card type:

```
interchange_fee = round(gross × interchange.percent + interchange.flat)
processor_fee   = round(gross × markup.percent      + markup.flat)
expected_settled = gross − interchange_fee − processor_fee
```

Rounding is to the cent (half-up). The schedule is in **`fee_schedule.json`**:

| Card type  | Interchange % | Interchange flat |
| ---------- | ------------- | ---------------- |
| VISA       | 1.80%         | $0.10            |
| MASTERCARD | 1.90%         | $0.10            |
| AMEX       | 2.50%         | $0.15            |
| DISCOVER   | 2.00%         | $0.10            |

Plus a flat **processor markup** applied to every card: **0.30% + $0.05**.

**Refunds** settle at the full negative gross with **no fees** (fees are not returned).

Because a correct match depends on the fee math, a settlement can be wrong in two different ways, and you should tell them apart:

- the **principal** is off (the settled amount doesn't reconcile even after correct fees), vs.
- the **fees** are off (fees deviate from the schedule, even though the settled amount is internally consistent with the fees the processor reported).

---

## What "Doesn't Line Up" Looks Like

Your report should identify at least these break categories. Names are yours to choose:

- **Unmatched - internal**: in our ledger, never settled. Money owed to us, or a dropped payout.
- **Unmatched - settlement**: settled with no ledger record. A real risk (possible fraud or a missed booking).
- **Amount mismatch**: matched, but the settled principal is off by more than rounding.
- **Fee discrepancy**: fees deviate from the published schedule - we may have been overcharged.
- **Duplicate settlement**: the same payment settled more than once. We'd be double-paid.
- **Orphan refund**: a refund with no corresponding original sale.

---

## Reporting

At minimum, the app should show:

- A **reconciliation summary**: count and total dollar amount per break category, plus how many matched cleanly.
- **Expected payout vs. actual settled**, and the total discrepancy.
- **Total fees** deducted.
- A **per-merchant** rollup.
- A **drill-down** list of breaks - each showing both sides (where they exist) and the reason it broke - so someone in ops could actually act on it.

---

## A Few Things We Left Open (On Purpose)

These have no single right answer. Make a call, and be ready to explain it:

1. **Amount tolerance** - how close is "matched"? What do you do about sub-cent rounding?
2. **Date window** - settlement usually lands in 1–3 days. The data contains a few that settle much later. Do you still match those, or flag them? Why?
3. **Split settlements** - a single capture can settle as multiple partial rows that sum to the expected net. How would you handle that? _(bonus)_

We'd rather see a documented, defensible choice than an attempt to handle everything.

---

## Stack

You may use any stack. **However**, the team you'd be joining works in a **Java / Spring Boot** backend and a **React** frontend. A solution in a similar shape is ideal - it lets us evaluate the work in the environment you'd actually be working in.

Stack choice itself isn't scored; quality within your chosen stack is.

---

## If You Use an LLM

Using an AI assistant is completely fine and expected - we use them too. We're interested in _how_ you use it. **If you use one, include a `PROMPTS.md`** (or a section in your writeup) with:

- The key prompts you used, roughly in order.
- Where the assistant helped, and where you had to correct, override, or discard its output.
- Any decisions you made _against_ its suggestion, and why.

We'll talk through your approach in the follow-up. There's no penalty for AI use and no bonus for avoiding it - we're evaluating your judgment in directing the tool.

---

## What We're Looking For

Baseline expectations:

- **It works end-to-end** - import → reconcile → view results - against the `data/` set.
- **Correct reconciliation** - matching logic is sound; the fee math is right; breaks are categorized correctly.
- **Separation of concerns** - the reconciliation engine is a distinct, testable unit, not tangled into a controller or a React component.
- **Persistence** - results survive a restart.
- **It handles bad input** - malformed rows, missing fields, and unexpected values degrade gracefully instead of crashing.
- **Tests** - the core matching and fee logic is covered.
- **It runs from your README** - clear, correct setup instructions.

Bonus / differentiators:

- Handling of the open questions above (tolerance, date window, split settlements).
- Thoughtful data modeling and query design.
- Idempotent re-imports; import history.
- A UI that an operations person could genuinely use to work the breaks.
- A short writeup of the tradeoffs you made and what you'd do with more time.

---

## Senior-Level Context

This is a senior role, so we're reading for judgment, not just a working demo: sound architecture, appropriate (not excessive) abstraction, clear naming, real error handling, and awareness of failure modes and operational concerns. Acknowledged gaps ("I didn't get to X; here's how I'd approach it") read better than silent ones. Ship something solid and tell us what you'd improve.

---

## Submitting

1. **Fork** this repository.
2. Build your solution in the fork.
3. Include a **README** with setup/run instructions, and a **`PROMPTS.md`** if you used an LLM.
4. Open a **pull request** back to this repo (or share a link to your fork), and let us know it's ready.

Meaningful commit history is welcome - it helps us see how you work. Don't commit secrets (`.env`, keys, credentials) or large generated artifacts.

Questions? Reach out. Good luck - we're looking forward to seeing what you build.
