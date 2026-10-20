# Dentify

School project (AI.NET, Challenge 1, team of two). A cross-platform **.NET MAUI** app that uses **Azure AI** to detect vehicle damage at handover, backed by our own **.NET Web API**.

## Language

- All code, comments, identifiers, commit messages and docs are in **English**.
- Chat replies to the developer can be in informal Belgian Dutch.

## What the app does

1. The user photographs a license plate. The API reads it with Azure AI Vision (OCR) and matches a `Vehicle`.
2. The user takes photos of the vehicle at departure or return. This creates an `Inspection`.
3. The API sends the photos to a trained Azure Custom Vision model and stores the detected `Damage` records.
4. Azure OpenAI writes a short damage report (`AiSummary`), optionally comparing with the previous inspection of the same vehicle.

## Repository layout

One git repo, two solutions in two folders. The developer creates the empty solutions and projects in Visual Studio. Claude Code writes the code inside them. Do not create, rename or delete projects, and do not edit `.sln` files, unless asked.

```
Dentify/                              (repo root, this file lives here)
├── CLAUDE.md
├── docs/dotnet-api-naslag.md
├── backend/
│   ├── Dentify.Backend.sln
│   ├── Dentify.Api/                  → Presentation (controllers, mapping profiles, strategies, Program.cs)
│   ├── Dentify.Contracts/            → DTOs, also used by the MAUI app
│   ├── Dentify.Domain/               → Entities
│   └── Dentify.Infrastructure/       → DbContext, repositories, migrations
└── maui/
    ├── Dentify.Maui.sln
    └── Dentify.Maui/                 → .NET MAUI app
```

Project references:

| Project | References |
|---|---|
| Dentify.Api | Contracts, Infrastructure |
| Dentify.Infrastructure | Domain |
| Dentify.Maui | `Dentify.Contracts` via a relative path: `..\..\backend\Dentify.Contracts\Dentify.Contracts.csproj` |
| Dentify.Contracts, Dentify.Domain | nothing |

Dentify.Api has **no** direct reference to Domain. Contracts must stay free of any package or project dependency so the MAUI app can reference it. If the cross-solution reference gives trouble, copy the DTOs into the MAUI project instead and say so.

## Course requirements (graded, never violate)

1. MAUI with **MVVM Community Toolkit**. **Code-behind files stay empty** (only `InitializeComponent()` and the constructor that sets `BindingContext` if needed). No logic in code-behind.
2. At least one trained Azure AI model integrated (Custom Vision), plus OCR and Azure OpenAI.
3. Attractive, user-friendly UI.
4. .NET API that reads **and writes** data. Minimum 2 tables in a one-to-many relationship, both used in the app. Layered architecture, repository pattern, everything async.
5. API and database hosted locally, exposed with **Ngrok**.
6. Multiple pages, a **NavigationService**, and at least **2 messages** passing info between pages.
7. Original idea, not a copy of the SafariSnap lesson app.
8. Follow the naming conventions and coding guidelines from the lessons.

## API conventions

The full reference is in @docs/dotnet-api-naslag.md. Follow it. Key points:

- .NET 10, EF Core, SQL Server LocalDB, AutoMapper, Swagger (Swashbuckle), `<Nullable>annotations</Nullable>` in every `.csproj`.
- Entities in `Domain/Entities`, configured with **data annotations**. Fluent API only for many-to-many and seeding (`HasData`). Constructors must assign all required properties.
- DTO naming: `XListDto`, `XDetailDto`, `XForCreationDto`, `XForUpdateDto`, `XForPatchDto`. Validation attributes on creation/update/patch DTOs only. FK properties in DTOs are `int`, not `int?`.
- `DentifyContext` in `Infrastructure/DbContexts`. Repository interface and implementation live together in `Infrastructure/Services`, one repository per entity, all methods async with the `Async` suffix, registered `AddScoped`.
- Controllers: `[ApiController]`, attribute routing (`api/[controller]`), `ActionResult<T>`, inject the repository **interface** and `IMapper` (never the DbContext). Mapping happens in the controller. POST returns `CreatedAtAction`, PUT/PATCH/DELETE return `NoContent`.
- AutoMapper profiles in `Api/MappingProfiles`, one `XProfile : Profile` per resource. Every mapped pair needs a `CreateMap`.
- In `Program.cs` all `builder.Services...` calls go **before** `builder.Build()`. Order in the pipeline: `UseCors` → `UseAuthentication` → `UseAuthorization` → `MapControllers`.
- Connection string key: `ConnectionStrings:DentifyDBConnectionString` in `appsettings.json`.
- Migrations: default project `Dentify.Infrastructure`, startup project `Dentify.Api`. From the CLI, run inside `backend/`:
  `dotnet ef migrations add <Name> --project Dentify.Infrastructure --startup-project Dentify.Api` and then `dotnet ef database update` with the same flags. Use a descriptive migration name. Ask before running `database update`.

## Azure integration

- All Azure calls happen in the **API**, never in the MAUI app. The app only uploads photos and receives DTOs.
- Use the Strategy pattern in `Api/Strategies/`, one interface per Azure service:
  - `IPlateReaderService` (Azure AI Vision OCR)
  - `IDamageDetectionService` (Custom Vision prediction)
  - `IReportService` (Azure OpenAI summary)
- Every interface has a real implementation and a **fake** implementation that returns fixed data. Register one or the other in `Program.cs` (fake in Development by default) so the app can be built without Azure.
- Endpoints, keys and the published Custom Vision iteration name come from configuration. **Never hardcode or commit secrets.** Use `dotnet user-secrets` for the API and keep `appsettings*.json` free of real keys (the AutoMapper license key is the only exception and is already needed locally).
- Check the current Azure SDK docs before choosing package names or API shapes. Do not guess.

## MAUI conventions

- Folders: `Views/`, `ViewModels/`, `Models/` (only if not covered by Contracts), `Services/`, `Messages/`, `Converters/`, `Resources/`.
- Pages end in `Page` (`VehicleListPage`), view models end in `ViewModel` (`VehicleListViewModel`), messages end in `Message` (`VehicleSelectedMessage`). One view model per page.
- Use `[ObservableProperty]` and `[RelayCommand]` from `CommunityToolkit.Mvvm`. Bind everything in XAML. No `Clicked` handlers.
- `INavigationService` + implementation wraps Shell navigation. View models never touch `Shell` directly.
- Messages via `WeakReferenceMessenger`. Required messages: `VehicleSelectedMessage` (list → detail / new inspection) and `InspectionSavedMessage` (refresh the inspection list). Optional: `PlateScannedMessage`.
- One `ApiService` (typed `HttpClient`) is the only class that talks to the API. Base URL comes from configuration, because the Ngrok URL changes. All calls async. Show loading and error states.
- Photos with `MediaPicker.Default.CapturePhotoAsync()`; request camera permissions properly.
- Pages: `VehicleListPage` → `VehicleDetailPage` → `NewInspectionPage` → `InspectionResultPage`.

## Domain model

| Entity | Fields |
|---|---|
| Vehicle | Id, LicensePlate, Brand, Model, Type |
| Inspection | Id, VehicleId (FK), Type (Departure/Return), Date, InspectorName, PhotoUrl, DamageDetected, AiSummary |
| Damage (optional) | Id, InspectionId (FK), Description, Severity, Location, Confidence |

One `Vehicle` has many `Inspection`s. One `Inspection` has many `Damage`s.

## Working agreement

- Prefer complete, runnable files over fragments. Match the existing style of the file you edit.
- Build before saying something works: `dotnet build backend/Dentify.Backend.sln` for the backend. MAUI builds are done by the developer in Visual Studio unless asked.
- Keep changes small and focused. Mention which files changed and what the developer must do manually (migrations, secrets, Azure portal steps).
- When a requirement above conflicts with a request, point it out instead of silently breaking it.
- Do not add libraries that are not in this file or in the naslag without asking.
