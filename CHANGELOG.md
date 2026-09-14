# Changelog

## [0.1.0] - Unreleased
### Changed
- Apex Unity SDK (`com.pixovr.apexunitysdk`) is now a required dependency (consuming projects
  add it via git URL); `Runtime/Apex` no longer gates on `PIXO_APEX_SDK`. The platform
  session falls back to `NullPlatformSession` (with a `Log.Warning`) only when no provider
  is installed — the null provider is intended for tests only.

### Added
- Initial package scaffold.
- Core runtime: Identity (GuidComponent/GuidRegistry/GuidReference), Events (ObservableSubject/Subject/EventBus/InteractionEventArgs), Commands (ICommand/CommandHistory + concrete commands), Interactions middlemen + I*Behaviour interfaces, Platform (IPlatformSession/SessionContext), Multiuser abstractions, GameModes, SceneManagement, Settings, Utility.
