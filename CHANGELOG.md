# Changelog

## [2.0.1] - 2026-09-05

### Changed

- Renamed the internal tab manager for consistency.
- Simplified drag-and-drop hook registration.
- Removed redundant folder marker fallback logic.
- Improved editor test names and documentation.
- Updated package and copyright metadata.

## [2.0.0] - 2026-07-04

### Added

- Added multi-item drag and drop while preserving source order and ignoring duplicates.
- Added folder-tab restoration after editor reloads, project changes, and folder moves.
- Added editor tests for Unity's internal API contracts and tab restoration behavior.

### Changed

- Replaced custom BetterTabs windows with Unity's native locked Project Browser and Inspector tabs.
- Updated compatibility to target Unity 6000.x.

## [1.0.0] - 2026-05-08

- Initial release.
