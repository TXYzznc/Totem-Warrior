# Framework Core Specification

## ADDED Requirements

### Requirement: Module Interface

The framework SHALL provide an `IGameModule` interface for explicit module lifecycle and dependency declaration.

#### Scenario: Default metadata is safe

- **GIVEN** a module does not override `ModuleCategory`
- **WHEN** ModuleRunner computes scheduling priority
- **THEN** the module is treated as category `4`
- **AND** it does not outrank infrastructure, service, interaction, or gameplay modules by default.

#### Scenario: Dependencies use concrete module types

- **GIVEN** a module declares `Dependencies`
- **WHEN** ModuleRunner validates the dependency graph
- **THEN** each dependency MUST be matched by exact registered concrete module type
- **AND** interface or base-class matching MUST NOT satisfy the dependency.

### Requirement: Module Registration And Validation

The framework SHALL separate module registration from graph validation so registration order does not become a hidden dependency rule.

#### Scenario: Modules registered out of dependency order

- **GIVEN** `UIModule` depends on `ResourceModule`
- **WHEN** `UIModule` is added before `ResourceModule`
- **AND** both modules are registered before startup validation
- **THEN** startup validation succeeds.

#### Scenario: Missing dependency fails before initialization

- **GIVEN** a module depends on an unregistered module type
- **WHEN** ModuleRunner validates before startup
- **THEN** startup fails with an `InvalidOperationException`
- **AND** FrameworkLogger records a `[ModuleRunner|ERROR]` missing dependency log.

#### Scenario: Circular dependency fails before initialization

- **GIVEN** registered modules form a dependency cycle
- **WHEN** ModuleRunner validates before startup
- **THEN** startup fails with an `InvalidOperationException`
- **AND** FrameworkLogger records a `[ModuleRunner|ERROR]` cycle log containing the dependency chain.

### Requirement: Module Startup Scheduling

The framework SHALL initialize modules with a `WhenAny` running-pool algorithm that starts newly-ready modules without waiting for unrelated modules in the same wave.

#### Scenario: Dependent module starts when its own dependency completes

- **GIVEN** `ResourceModule` and `AudioModule` have no dependencies
- **AND** `UIModule` depends only on `ResourceModule`
- **WHEN** `ResourceModule` completes before `AudioModule`
- **THEN** `UIModule` becomes eligible immediately
- **AND** it does not wait for `AudioModule` unless it explicitly depends on it.

#### Scenario: MaxConcurrency limits started modules

- **GIVEN** more ready modules exist than available concurrency slots
- **WHEN** `MaxConcurrency > 0`
- **THEN** ModuleRunner starts only the highest-priority ready modules that fit the available slots
- **AND** leaves the remaining ready modules pending until a slot is free.

### Requirement: Module Failure Handling

The framework SHALL fail fast during startup and clean up already-initialized modules.

#### Scenario: InitializeAsync throws

- **GIVEN** one module throws during `InitializeAsync`
- **WHEN** startup is in progress
- **THEN** ModuleRunner stops scheduling new modules
- **AND** cancels or waits for running initialization tasks according to the shared cancellation token
- **AND** calls `ShutdownAsync` in reverse initialization order for modules already initialized
- **AND** rethrows the startup exception.

#### Scenario: InitializeAsync times out

- **GIVEN** `InitTimeoutSeconds > 0`
- **WHEN** a module initialization exceeds the timeout
- **THEN** ModuleRunner logs a timeout warning
- **AND** treats the timeout as startup failure.

### Requirement: Module Lookup

The framework SHALL expose `GetModule<T>()` only for initialized modules.

#### Scenario: Lookup initialized module

- **GIVEN** a module has state `Initialized`
- **WHEN** `GetModule<T>()` is called for its concrete type
- **THEN** the module instance is returned.

#### Scenario: Lookup pending, initializing, shutdown, or missing module

- **GIVEN** a module is missing or its state is not exactly `Initialized`
- **WHEN** `GetModule<T>()` is called
- **THEN** ModuleRunner throws a clear exception.

### Requirement: Event Publishing

The framework SHALL provide an EventBus for exact-type event publish/subscribe.

#### Scenario: Fire-and-forget publish continues after handler failure

- **GIVEN** multiple subscribers exist for an event
- **WHEN** one subscriber throws
- **THEN** EventBus logs the failure
- **AND** continues invoking remaining subscribers.

#### Scenario: PublishAndWaitAsync aggregates failures

- **GIVEN** multiple async subscribers exist for an event
- **WHEN** multiple subscribers fail
- **THEN** EventBus logs each failure
- **AND** throws an `AggregateException`.

### Requirement: Request Response Handling

The framework SHALL support request-response handlers separately from broadcast event handlers.

#### Scenario: Request has no response handler

- **GIVEN** no request handler exists for a request type
- **WHEN** `RequestAsync<TRequest,TReply>` is called
- **THEN** EventBus logs a warning
- **AND** returns the supplied default reply.

#### Scenario: Request has multiple response handlers

- **GIVEN** more than one response handler exists for a request type
- **WHEN** `RequestAsync<TRequest,TReply>` is called
- **THEN** EventBus logs a warning naming the selected and ignored handlers
- **AND** uses the first registered handler deterministically.

### Requirement: Handler Discovery

The framework SHALL discover module handlers by attribute after module initialization and unregister them after module shutdown.

#### Scenario: Broadcast handler signatures

- **GIVEN** a method marked `[EventHandler]`
- **WHEN** ModuleRunner scans module handlers
- **THEN** only `void Handle(EventType evt)` and `UniTask HandleAsync(EventType evt)` signatures are accepted.

#### Scenario: Request handler signatures

- **GIVEN** a method marked `[RequestHandler]`
- **WHEN** ModuleRunner scans module handlers
- **THEN** only `TReply Handle(TRequest req)` and `UniTask<TReply> HandleAsync(TRequest req)` signatures are accepted.

### Requirement: AI Friendly Logging

The framework SHALL emit structured logs that are grep-friendly and can identify both the logger call site and the origin of delegated failures.

#### Scenario: Direct framework log

- **WHEN** framework code calls `FrameworkLogger.Error/Warn/Info/Debug`
- **THEN** the log starts with `[Module|LEVEL]`
- **AND** ends with `Location=file:line`.

#### Scenario: Handler failure log

- **GIVEN** EventBus catches an exception thrown by a handler
- **WHEN** it logs the failure
- **THEN** the message includes `Handler=Type.Method`
- **AND** includes an `Origin=` field derived from the exception stack trace when available.

### Requirement: Template Files Are Non Compiling References

The framework SHALL provide AI reference templates without adding invalid example code to Unity compilation.

#### Scenario: Module template exists

- **WHEN** `ModuleTemplate` is added
- **THEN** it is stored as a non-compiling reference file such as `.cs.txt`
- **AND** Unity does not compile it as runtime source.

### Requirement: GameApp Lifecycle

The framework SHALL provide a `GameApp` MonoBehaviour that owns EventBus and ModuleRunner as instance fields.

#### Scenario: Startup exception is visible

- **WHEN** `GameApp.Start` fails during module startup
- **THEN** the exception is logged through FrameworkLogger
- **AND** the startup failure is not silently swallowed.

#### Scenario: Destroy stops initialized modules once

- **WHEN** the GameObject is destroyed
- **THEN** `GameApp` calls `StopAsync`
- **AND** repeated stop calls do not shutdown the same module twice.
