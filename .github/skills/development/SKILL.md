---
name: development
description: Guidelines and best practices for development with .NET.
---

## UI Components / Pages

- All UI components are Razor components organized under `src\{appName}\Components\`.
- The various components are organized by feature area, with a general pattern of having sub-folders for `Pages`, `Shared`, and `Layout` components:
- The various UI components will inject services to perform any necessary business logic or data fetching, but should not contain any complex logic themselves. They should be thin and focused on rendering the UI and handling user interactions.
- To the greatest extent possible, try to use built-in the defined themes, styles, typography, color palettes, and components from MudBlazor when implementing the UI in Blazor. This will help maintain a consistent look and feel across the app and also speed up development. Try to avoid custom CSS and styles unless absolutely necessary.
- The @code block in the Razor components should include the following \#region, \#endregion blocks (more blocks can be added as needed):
  - `#region Parameters` for any [Parameter] properties.
  - `#region Private Fields, Properties` for any private fields & properties.
  - `#region Event Handlers` for any event handler methods (for example, button click handlers).
  - `#region Lifecycle Methods` for any Blazor lifecycle methods (for example, OnInitializedAsync).
  - `#region Private Helper Methods` for any other private helper methods.
  - `#region CSS Style Properties` for any CSS 'style' attribute properties (for example, `private string _buttonColor = "red";`).
  - `#region Text, Label Properties ` for any text/label properties (for example, `private string _buttonText = "Click Me";`). This include ARIA labels and accessibility text. Prefer const strings over properties here, if possible.

## Controllers

- All API controllers (and trigger-activated Azure functions) are organized under `src\{appName}\Controllers\` or `src\{appName}.Api\Controllers\`.
- Controllers will inject services to perform any necessary business logic or data fetching, but should not contain any complex logic themselves. They should be thin and focused on handling HTTP requests and responses, and delegating to services for the actual work.
- Controllers should only accept and return DTOs/REST models. Not view/storage/persistence models or database entities.
- To the greatest extent possible, try to use the controller base classes defined in the Nucleus nuget package. A benefit of this is that it automatically converts any validation exceptions (thrown by FluentValidation) or any custom exceptions (thrown by service layer) into appropriate ActionResult / HTTP responses with error details.

## Service Layer

- Services are centralized under: `src\{AppName}\Services\` or `src\{AppName}.Core\Services\`.
- Define three sub-folders:
  - `Interfaces` for service interfaces.
    - All methods, properties and events on service interfaces should be documented with XML comments to describe their purpose, expected inputs and outputs, and any exceptions they might throw.
  - `Implementations` for concrete service implementations. 
    - To the greatest extent possible, try to use the service base classes defined in the Nucleus nuget package (see details below).
    - Else if necessary, create new service base classes.
    - Every public method on services that accept model(s) as arguments must validate the input before any processing occurs (see below).
  - `Validators` for FluentValidation validators of inputs: models, DTOs, etc.
    - Define all your FluentValidation validator classes here.
- Service layer should only accept and return DTOs/REST models and ViewModels. Not storage/persistence models or database entities.
  - Service layer can use AutoMapper to map between DTOs/ViewModels and storage models/entities, but the service method signatures should not directly reference storage models/entities.
- For all get/read/fetch/query methods, the service layer should take an optional parameter `bool throwIfNotExists = true`.
  - If the entity does not exist and `throwIfNotExists` is true, the service layer should throw an appropriate custom exception.
  - Else if the entity does not exist and `throwIfNotExists` is false, the service layer should return null or an empty collection.
- For all update/delete/write methods, it would be the service layer's responsibility to explicitly check for the existence of said entity (via repository layer) before attempting to update/delete/write it.
  - If the entity does not exist, the service layer should throw an appropriate custom exception.
- Miscellaneous:
  - For all the app's HTTP calls to 3rd parties, we should define services that are injected via DI as typed clients or named clients.

## Repository Layer

- Repositories are centralized under: `src\{AppName}\Repositories\` or `src\{AppName}.Core\Repositories\`.
- Define two sub-folders:
  - `Interfaces` for repository interfaces.
    - To the greatest extent possible, try to use the generic repository interfaces defined in the Nucleus nuget package. Else create a new generic repository interface, operating on a `TEntity` type (basically a storage/entity/persistence model).
    - All methods, properties and events on repository interfaces should be documented with XML comments to describe their purpose, expected inputs and outputs, and any exceptions they might throw.
  - `Implementations` for concrete repository implementations.
    - To the greatest extent possible, try to use the repository base classes defined in the Nucleus nuget package (see details below).
    - When using EFCore, the DBContext class should be defined in the `Implementations` folder, and repository implementations can depend on it for data access.
- Repository layer should only accept, return and operate upon storage/persistence models or database entities. Not DTOs or ViewModels.
- The repository layer can only throw `System.ArgumentException` (and its derived types) for invalid inputs.
- The repository layer should not "intentionally" throw any exceptions if an entity is not found.
  - For get/read/fetch/query methods, it should return null or an empty collection.
  - For update/delete/write methods, it should return false. Additionally the caller (typically the service layer) is responsible for checking the existence of the entity before performing write operations on it.

## Models

- Models are centralized under: `src\{AppName}\Models\` or `src\{AppName}.Core\Models\`.
- For UI: Create a `ViewModels` sub-folder:
  - The view model types should include the following \#region, \#endregion blocks (more blocks can be added as needed):
    - `#region Display Properties` for properties that are directly displayed as-is on the UI.
    - `#region Computed Properties` for properties that are not directly stored, but are computed based on other properties and displayed on the UI.
    - `#region Hidden Properties` for properties that are not directly displayed on the UI, but are necessary for internal logic or computations.
  - Input models go into a `InputModels` sub-folder.
  - Result models go into a `ResultModels` sub-folder.
    - Result models include computed display properties (for example, currency-formatted strings) to keep display formatting out of core math logic.
- For API: Create the following sub-folders:
  - `Dto` for REST API request/response models, DTOs, etc.
  - `Dao` for persistence models (for example, Cosmos DB entities).
  - `Events` for event stream models.
- Define a `MapperProfile` class for AutoMapper under `src\{AppName}\Misc\AutoMapper\` to centralize mapping configuration between various model types.
- To the greatest extent possible, use `required` property modifiers for non-nullable properties.

## Custom Exceptions

- Custom exception types are centralized under `src\{AppName}\Exceptions\` or `src\{AppName}.Core\Exceptions\`.
- To the greatest extent possible, the custom exceptions should inherit from base exception classes defined in the Nucleus nuget package. This allows the custom exception to define its own conversion to an ActionResult or HTTP response.

## Miscellaneous Helpers

- Any other helper classes, extension classes, utilities, or miscellaneous code can be centralized under `src\{AppName}\Misc\` or `src\{AppName}.Core\Misc\`.
- Here is the general pattern for organizing code in the `Misc` folder:
  - `ExtensionMethods` sub-folder for any extension classes/methods.
  - `Utilities` sub-folder for any other helper utilities that don't fit into the above categories.

## Global Constants / Routing / Config Keys

- Constants are centralized under: `src\{AppName}\Constants\` or `src\{AppName}.Core\Constants\`.
- Existing pattern:
  - `RouteConstants` for navigation routes
  - `ApiConstants` for well-known API endpoint paths
  - `UrlConstants` for external URLs
  - `HttpClientNameConstants` for named HTTP client keys
  - `ConfigKeys` for configuration keys
- These classes will be declared as `public static {className}` with `public const string` fields.
- Any Enums can also be declared as `public enum {enumName}`.
- Centralize magic strings and enums in these classes rather than in individual components or services.

## Other Important Notes

### Dependency Injection Pattern ⚠️

DI is centralized via startup extension methods:
- `src\{appName}\Misc\ExtensionMethods\WebAssemblyHostBuilderExtensions.cs`

Registration pattern:
- AutoMapper profile registration (`MapperProfile`)
- MudBlazor services registration
- FluentValidation validators registered by assembly scanning
- Calculator services registered as `AddSingleton<ICalculator<...>, ...Calculator>()`
- Repositories registered as `AddTransient<...>()`

Keep new registrations in `ConfigureServices()` to preserve a single composition root.

### Configuration

- To the greatest extent possible, use appsettings.json for configuration. 
  - Bind configuration sections to strongly-typed configuration classes (for example, `CosmosDbConfig`) that are injected via DI where needed.
  - Individual configuration keys should be stored in the `ConfigKeys` class under the `src\{AppName}\Constants` or `src\{AppName}.Core\Constants` folder.
- For Local development: Use user secrets. For Azure function apps, use local.settings.json.

### Logging and Telemetry

- Use the framework-injected `ILogger<T>` for logging in all services, repositories, controllers, and other classes.
- By default, the logging configuration should be set up as follows:
  - Local development: Log to console with `LogLevel.Debug` or `LogLevel.Trace` for maximum verbosity.
  - Everything else: Log to Azure AppInsights with `LogLevel.Information`. The AppInsights instrumentation key should be stored in configuration.

### NuGet Packages

- Add the `MithunShanbhag.Nucleus` package (latest pre-release version). 
  - Generally, this has all the necessary base abstractions (for repositories, event streams, services, etc.), helper utilities, and shared dependencies.
  - Source code: [mithunshanbhag/Nucleus](https://github.com/mithunshanbhag/nucleus)

### Formatting and Quality Workflow ⚠️

Before completing changes, run formatting and verification:

1. `dotnet format`
   - Ensures code aligns with repository `.editorconfig` conventions.
2. `dotnet build --nologo`

### Other Conventions

1. Prefer primary constructors for classes with dependencies.
2. Have a `GlobalUsings.cs` file in each project for common namespaces. Declare all possible global usings here to minimize the need for `using` statements in individual files.
3. Similarly, prefer the `_Imports.razor` file for Razor components to centralize common `@using` directives.
4. Only one .cs file per class/interface/struct/type, and the file name should match the type name.
