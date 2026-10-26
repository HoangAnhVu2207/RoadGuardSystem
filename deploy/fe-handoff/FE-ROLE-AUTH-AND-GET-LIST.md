# Roles and discovery

Supervisor uses the authorized project directory; current PM membership scopes project access. Reporter own reports/evidence is a separate read model, not PM catalog authority. Operator uses assigned survey tasks/work-package. Crew uses assigned FIELD work. Member candidate pools do not prove command eligibility. Current actor, membership, assignment and lifecycle are rechecked by services.

G01 is owner-approved and implemented: project → road/version → paged SegmentSet list → detail/segments. A known-ID detail does not establish a global list. G02 repair, G03 Fast Track, G04 export history, G05 manager survey/AI and G06 retention/offline catalogs remain unapproved new contracts. Do not require hardcoded demo IDs as a production interface.

Follow nextCursor even when a post-filtered page is empty. Do not invent page numbers/totalCount or implicit default SegmentSet selection. Wrong-project denial must not issue a new private capability. Previously issued links can expire normally at TTL300.

See FE-DISCOVERY-GAPS.md, FE-DISCOVERY-WORKFLOWS.md and FE-GET-LIST-AUTH-MATRIX.md. Runtime PASS is limited to executed dimensions, not inferred from these descriptions.
