# D-AUTH: Versioned authentication and profile compatibility

- **Status:** PROPOSED, RF-04 draft.
- **Sources:** TARGET_CONFIRMED PR-36A/37; CURRENT_VERIFIED bearer registration and `/profile`/`/me`/two reset routes in RF-01/CG02-03; historical ADR 002.
- **Proposal:** add web cookie/server session and Android token contract through a versioned transition while current bearer/profile/reset behavior is characterized and consumers are identified. Define CSRF/CORS, session invalidation, PII projection and password-reset secret delivery before cutover. Project membership checks remain current at command time.
- **Alternative:** coordinated breaking release after all consumer owners sign off. No option is selected as Accepted.
- **Gate:** Q-RF02-02, external web/Android inventory, isolated wrong-actor/expiry/replay tests, approved wire examples. Do not remove an endpoint by name alone.
