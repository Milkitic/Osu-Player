# DI Migration Guide

This project is currently prepared for dependency injection, but it still uses the lightweight
`Milky.OsuPlayer.Shared.Dependency.Service` registry in several places. This document exists to
guide the future migration to a real DI container, preferably `Microsoft.Extensions.DependencyInjection`.

When the migration is fully completed, either:

- update this document with a clear `Status: Completed` section and the completion date, or
- delete this document if it no longer provides useful maintenance value.

## Current State

- `OsuPlayer.Wpf.Services.AppServices` is the current composition helper.
- `IPlayerDataStore` is the raw data-access abstraction.
- `PlayerDataService` implements `IPlayerDataStore`.
- `IPlayerDataService` is the UI/application-facing data service abstraction.
- `NotifyingPlayerDataService` implements `IPlayerDataService` and wraps `IPlayerDataStore`.
- WPF UI and ViewModels should depend on `IPlayerDataService`.
- non-WPF code such as Common and Media.Audio should depend on `IPlayerDataStore`.

This split is intentional. Do not collapse `IPlayerDataStore` and `IPlayerDataService` during DI
migration, because doing so can create circular dependency resolution when using decorators.

## Target Registration Shape

Use this registration shape when introducing Microsoft DI:

```csharp
services.AddSingleton<IAppNotificationService, AppNotificationService>();
services.AddSingleton<IPlayerDataStore, PlayerDataService>();
services.AddSingleton<IPlayerDataService, NotifyingPlayerDataService>();
```

`NotifyingPlayerDataService` must depend on `IPlayerDataStore`, not `IPlayerDataService`.

Avoid this registration shape:

```csharp
services.AddSingleton<IPlayerDataService, NotifyingPlayerDataService>();
```

if `NotifyingPlayerDataService` depends on `IPlayerDataService`; that creates a self-referential
dependency graph.

## Migration Steps

1. Add DI package references to the WPF entry project.
2. Create a composition root in startup code, likely in `App` or a dedicated bootstrapper.
3. Register infrastructure services first, then application services, then UI entry points.
4. Replace `AppServices.RegisterDefaults()` with container registration.
5. Replace `AppServices.PlayerData` and `AppServices.PlayerDataStore` call sites with constructor
   injection where practical.
6. Replace `Service.Get<T>()` usages progressively, starting with services that are not created by
   XAML.
7. For XAML-created ViewModels and controls, either introduce a ViewModel locator/factory or move
   creation into code paths that can receive injected dependencies.
8. Keep `PlayerDataService` free of UI notification behavior.
9. Keep user-facing error notification in WPF-specific services or command/viewmodel error handling.
10. Remove `AppServices` only after all call sites have been migrated.

## Review Checklist

- Common and Media.Audio do not reference WPF services.
- UI code does not directly instantiate `OsuPlayerDbContext`.
- `NotifyingPlayerDataService` depends on `IPlayerDataStore`.
- `PlayerDataService` remains the only production implementation that creates `OsuPlayerDbContext`.
- No service depends on the same interface it implements.
- Startup creates one service provider and disposes it on application exit.
- Long-lived services do not capture short-lived `DbContext` instances.

## Known Transitional Code

These are expected during the transition:

- `AppServices`
- `Milky.OsuPlayer.Shared.Dependency.Service`
- parameterless constructors that delegate to fallback service access for XAML compatibility

These should be removed or minimized once DI owns object creation.
