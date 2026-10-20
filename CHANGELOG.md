# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

All 10 packages are co-versioned and released together. Each entry notes which
package(s) a change affects; a package whose only change is the shared version
bump is listed under **Version bump only**.

> **Release process:** Before tagging a new `v*` release, move the entries under
> `[Unreleased]` into a new `## [x.y.z] - YYYY-MM-DD` section (adding new
> `### Added` / `### Changed` / `### Fixed` / … headings as needed), update the
> compare links at the bottom of this file, and bump `<Version>` in the packable
> projects. Commit this **before** pushing the tag so the packaged
> `PackageReleaseNotes` link points at up-to-date notes.

## [Unreleased]

### Added

- This `CHANGELOG.md`, and a `PackageReleaseNotes` link to it on the NuGet.org
  listing of every package. *(affects: all packages)*

## [2.1.0] - 2026-07-16

### Added

- `MessageValidation.ImmediateValidations`: new validation adapter bridging
  [Immediate.Validations](https://github.com/ImmediatePlatform/Immediate.Validations)
  source-generated, AOT-friendly, zero-reflection validators into the pipeline
  via `AddMessageImmediateValidations()`. *(affects: MessageValidation.ImmediateValidations — new package)*

### Changed

- Version bump only — no functional change. *(affects: MessageValidation,
  MessageValidation.DataAnnotations, MessageValidation.FluentValidation,
  MessageValidation.MqttNet, MessageValidation.RabbitMQ, MessageValidation.Kafka,
  MessageValidation.AzureServiceBus, MessageValidation.AzureEventHubs,
  MessageValidation.NatsNet)*

## [2.0.0] - 2026-04-28

### Added

- Middleware-style pipeline mirroring the ASP.NET Core `IApplicationBuilder`
  shape: `IMessageMiddleware`, `IMessagePipelineBuilder` with `Use`,
  `UseMiddleware<T>`, and `Map`, plus a new
  `AddMessageValidation(configure, configurePipeline)` overload for composing a
  custom stack. *(affects: MessageValidation)*
- `MessageContext` gains `MessageType`, `Message`, `ValidationResult`,
  `Services`, and an `Items` extensibility bag so custom middleware can share
  state across the pipeline. *(affects: MessageValidation)*
- `MessageValidation.NatsNet`: new transport adapter for
  [NATS.Net](https://github.com/nats-io/nats.net) via
  `INatsConnection.SubscribeWithMessageValidationAsync()`. *(affects: MessageValidation.NatsNet — new package)*
- `MessageValidation.AzureEventHubs`: new transport adapter for Azure Event Hubs
  (`EventProcessorClient` / `EventHubConsumerClient`, passwordless auth), first
  published at 2.0.0. *(affects: MessageValidation.AzureEventHubs — new package)*
- All packages now multi-target `net8.0` and `net10.0` (previously `net10.0`
  only). *(affects: all packages)*

### Changed

- **Breaking:** The core pipeline was refactored from a fixed sequence of stages
  to a composable middleware architecture. `MessageValidationPipeline` is now
  built from a middleware chain; the default stack (metrics → type resolution →
  deserialization → validation → failure handling → handler dispatch) is
  registered automatically by `MessageValidationPipeline.ConfigureDefaults`.
  Code that only calls `AddMessageValidation(...)` and consumes
  `IMessageValidationPipeline` is source-compatible; code that constructs or
  derives from the internal stage classes must be updated.
  *(affects: MessageValidation)*

### Version bump only

- `MessageValidation.DataAnnotations`, `MessageValidation.FluentValidation`,
  `MessageValidation.MqttNet`, `MessageValidation.RabbitMQ`,
  `MessageValidation.Kafka`, and `MessageValidation.AzureServiceBus` carry no
  functional change beyond tracking the core 2.0.0 release.

[Unreleased]: https://github.com/Romain-OD/MessageValidation/compare/v2.1.0...HEAD
[2.1.0]: https://github.com/Romain-OD/MessageValidation/compare/v2.0.0...v2.1.0
[2.0.0]: https://github.com/Romain-OD/MessageValidation/compare/v1.1.0...v2.0.0
