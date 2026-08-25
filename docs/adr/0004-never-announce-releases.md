# GHX releases are never announced

SponsorLink's org webhook posts a published GitHub release to X when the body matches changelog section titles or contains `<!-- x -->`, and injects a sponsors section unless the body contains `<!-- nosponsors -->`. GHX notes are the upstream GitHub CLI changelog.

Every draft from `release.yml` appends `<!-- !x -->` (skip-announce) and `<!-- nosponsors -->`. `<!-- x -->` must not appear: that marker forces an announcement.
