# Owner decision packet

| Decision | Options | Recommendation | Consequence if accepted |
|---|---|---|---|
| Callback identity domain | all eight fields; subset; provider event id | all identity fields plus checksum/detections as v2 envelope | caller must send stable attempt/event identity |
| Legacy receipt policy | A version-aware reader; B namespace; C reject after window | A if schema marker approved, otherwise C | preserves exact legacy retry without guessing missing identity |
| Detection ordering | source order semantic; canonical sort; provider contract | retain source order until contract decides | reordered arrays remain distinct requests |
| Provider key scope | global event; job/attempt; project | job + attempt + provider event id | defines retry collision boundary |
| Validation stored failures | preserve original failure on replay; remap all to generic 422 | preserve original status (implemented) | first and replay HTTP outcomes match |
| A09-01 crash after final lease | recover on expiry; dead-letter at cap; operator repair | owner-selected terminal rule required | determines whether capped leased rows can re-enter queue |
| A09-02 fencing | owner string only; generation/token; DB lease row | generation/token + expiry check | likely additive schema/contract work; outside current allowlist |

No decision in this table is treated as accepted merely because it is recommended. The owner must record the selected option and caller/schema impact before A08-01 or A09 remediation proceeds.
