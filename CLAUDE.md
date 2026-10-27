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

One git repo, two solutions in two folders. Solutions use the `.slnx` format. The project structure below is in place. Do not create, rename or delete projects, and do not edit `.slnx` files, unless asked.

```
Dentify/                              (repo root, this file lives here)
├── CLAUDE.md
├── dotnet-api-naslag.md
├── Backend/
│   └── Dentify.Backend/
│       ├── Dentify.Backend.slnx
│       ├── Dentify.Api/              → Presentation (controllers, mapping profiles, strategies, Program.cs)
│       ├── Dentify.Contracts/        → DTOs (Models/), also used by the MAUI app
│       ├── Dentify.Domain/           → Entities
│       └── Dentify.Infrastructure/   → DbContext, repositories, migrations
└── Maui/
    └── Dentify.Maui/
        ├── Dentify.Maui.slnx
        └── Dentify.Maui/             → .NET MAUI app
```

Project references:

| Project | References |
|---|---|
| Dentify.Api | Contracts, Infrastructure |
| Dentify.Infrastructure | Domain |
| Dentify.Maui | `Dentify.Contracts` via a relative path: `..\..\..\Backend\Dentify.Backend\Dentify.Contracts\Dentify.Contracts.csproj` |
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

The full reference is in @dotnet-api-naslag.md. Follow it. Key points:

- .NET 10, EF Core, SQL Server LocalDB, AutoMapper, Swagger (Swashbuckle), `<Nullable>annotations</Nullable>` in every `.csproj`.
- Entities in `Domain/Entities`, configured with **data annotations**. Fluent API only for many-to-many and seeding (`HasData`). Constructors must assign all required properties.
- DTO naming: `XListDto`, `XDetailDto`, `XForCreationDto`, `XForUpdateDto`, `XForPatchDto`. Validation attributes on creation/update/patch DTOs only. FK properties in DTOs are `int`, not `int?`.
- `DentifyContext` in `Infrastructure/DbContexts`. Repository interface and implementation live together in `Infrastructure/Services`, one repository per entity, all methods async with the `Async` suffix, registered `AddScoped`.
- Controllers: `[ApiController]`, attribute routing (`api/[controller]`), `ActionResult<T>`, inject the repository **interface** and `IMapper` (never the DbContext). Mapping happens in the controller. POST returns `CreatedAtAction`, PUT/PATCH/DELETE return `NoContent`.
- AutoMapper profiles in `Api/MappingProfiles`, one `XProfile : Profile` per resource. Every mapped pair needs a `CreateMap`.
- In `Program.cs` all `builder.Services...` calls go **before** `builder.Build()`. Order in the pipeline: `UseCors` → `UseAuthentication` → `UseAuthorization` → `MapControllers`.
- Connection string key: `ConnectionStrings:DentifyDBConnectionString` in `appsettings.json`.
- Migrations: default project `Dentify.Infrastructure`, startup project `Dentify.Api`. From the CLI, run inside `Backend/Dentify.Backend/`:
  `dotnet ef migrations add <Name> --project Dentify.Infrastructure --startup-project Dentify.Api` and then `dotnet ef database update` with the same flags. Use a descriptive migration name. Ask before running `database update`.

## Azure integration

- All Azure calls happen in the **API**, never in the MAUI app. The app only uploads photos and receives DTOs.
- Use the Strategy pattern in `Api/Strategies/`, one interface per Azure service:
  - `IPlateReaderService` (Azure AI Vision OCR)
  - `IDamageDetectionService` (Custom Vision prediction)
  - `IReportService` (Azure OpenAI summary)
- Every interface has a real implementation and a **fake** implementation that returns fixed data. Register one or the other in `Program.cs` (fake in Development by default) so the app can be built without Azure.
- Switch: `Azure:UseFakeServices` is `true` in `appsettings.Development.json`. To use the real services locally, set it to `false` in user-secrets. A real implementation is only registered when its own settings are present, otherwise that service falls back to its fake.
- Configuration keys (user-secrets): `Azure:Vision:Endpoint`, `Azure:Vision:Key`, `Azure:CustomVision:PredictionEndpoint`, `Azure:CustomVision:PredictionKey`, `Azure:CustomVision:ProjectId`, `Azure:CustomVision:PublishedName`, `Azure:CustomVision:ProjectType` (`ObjectDetection` or `Classification`), `Azure:CustomVision:MinimumProbability` (default 0.5), `Azure:OpenAI:Endpoint`, `Azure:OpenAI:Key`, `Azure:OpenAI:DeploymentName`.
- Implemented: `AzurePlateReaderService` (package `Azure.AI.Vision.ImageAnalysis`, OCR `VisualFeatures.Read`, filters Belgian plate formats). `CustomVisionDamageDetectionService` (Prediction REST API v3.1 via a typed `HttpClient`, no package; classification projects give location "Whole photo"). OpenAI still uses its fake.
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
| Inspection | Id, VehicleId (FK), Type (Departure/Return), Date, InspectorName, PhotoUrl (cover photo = first upload), DamageDetected, AiSummary |
| InspectionPhoto | Id, InspectionId (FK), Url, UploadedAt |
| Damage | Id, InspectionId (FK), Description, Severity (Minor/Moderate/Severe), Location, Confidence |

One `Vehicle` has many `Inspection`s. One `Inspection` has many `InspectionPhoto`s and many `Damage`s.

Photos are stored by the API in `Dentify.Api/uploads/` (git-ignored) and served at `/uploads/...`. Photo URLs in DTOs are **relative**; the MAUI app prefixes them with the API base URL, because the Ngrok URL changes.

## API flow for the app

1. `POST /api/plates/scan` (multipart, field `photo`) → `PlateScanResultDto` with the recognized plate and the matching vehicle, if any.
2. `POST /api/inspections` (JSON `InspectionForCreationDto`) → creates the inspection.
3. `POST /api/inspections/{id}/photos` (multipart, field `photos`, 1 to 8 photos, .jpg/.png, max 20 MB each) → stores the photos, runs damage detection, writes `AiSummary` and returns the `InspectionDetailDto`.
4. Read endpoints: `GET /api/vehicles?searchQuery=`, `GET /api/vehicles/{id}`, `GET /api/vehicles/{id}/inspections`, `GET /api/inspections/{id}`, `GET /api/inspections/{id}/damages`.
5. Write endpoints besides the flow: `POST /api/vehicles`, `PUT /api/vehicles/{id}` (409 on a duplicate plate), `PATCH /api/inspections/{id}` (`InspectorName`, `AiSummary`).

Details the app must handle:
- Enums travel as strings: inspection `Type` is `"Departure"`/`"Return"`, damage `Severity` is `"Minor"`/`"Moderate"`/`"Severe"`.
- `PlateScanResultDto.RecognizedPlate` is null when no plate was read; `Vehicle` is null (`IsMatch` false) when the plate is not in the database. Offer to pick the vehicle from the list in both cases.
- Photo upload can take several seconds per photo (Azure calls): show a busy indicator. It returns **502** with a readable message when an Azure service fails, **400** for invalid files, **404** for an unknown inspection.
- The API shrinks every photo to at most 2048 px (JPEG, EXIF rotation applied) before storing it and calling Azure (`SkiaImageResizeService`, package `SkiaSharp`). Still resize in the app before uploading (longest side about 2048 px): phone originals are 10+ MB and uploading them over Ngrok is slow. Check the current `MediaPicker` docs for built-in size options before writing resize code.
- Damage `Location` is an area of the photo ("Top left", "Center", ...), `Confidence` is 0..1.

## Team split and status

- **Andreas**: backend and Azure (plate OCR, Custom Vision, Azure OpenAI report).
- **Finn**: the MAUI app (`Maui/`). Work on a feature branch, e.g. `feature/maui-app`, and merge into `main` via a PR.

Status:
- API complete (all endpoints above, Swagger, seed data). Plate OCR and Custom Vision damage detection work against real Azure resources; the report still uses `FakeReportService` (Azure OpenAI is pending).
- Custom Vision: object detection project `Dentify-Detection` (resource `asbigfive`, Germany West Central), published as `Iteration1`, mAP 63.5%. Strong on broken glass and flat tires, over-predicts dents, rarely finds scratches. Training in Germany West Central often hangs; see `tools/customvision/README.md`.
- MAUI app: not started yet (template only). `CommunityToolkit.Mvvm` and the reference to `Dentify.Contracts` are already in the project.

## Running the API locally (new PC)

Easiest: `powershell -ExecutionPolicy Bypass -File tools\setup-dev.ps1` (database only, fake Azure services) or add `-UseAzure` (also stores the Azure settings; asks for the keys with hidden input). The prompt for the MAUI agent is in `docs/maui-agent-prompt.md`.

Manually, from the repo root, once:

```bash
dotnet tool install --global dotnet-ef
dotnet ef database update --project Backend/Dentify.Backend/Dentify.Infrastructure --startup-project Backend/Dentify.Backend/Dentify.Api
```

Without any user-secrets the API runs with the **fake** Azure services, which is enough to build the whole app. To use the real ones, ask Andreas for the values (never via git) and set them, see "Azure integration" for the keys:

```bash
dotnet user-secrets set "Azure:UseFakeServices" "false" --project Backend/Dentify.Backend/Dentify.Api
```

Start: `dotnet run --project Backend/Dentify.Backend/Dentify.Api --launch-profile http` (http://localhost:5152, Swagger at `/swagger`). A phone or Android emulator reaches the API through Ngrok: `ngrok http 5152`, then put the `https://...ngrok-free.app` URL in the app configuration. Ngrok's free tier shows a browser warning page; send the header `ngrok-skip-browser-warning: true` from the `ApiService`.

## Working agreement

- Prefer complete, runnable files over fragments. Match the existing style of the file you edit.
- Build before saying something works: `dotnet build Backend/Dentify.Backend/Dentify.Backend.slnx` for the backend. For the MAUI app, a quick check is `dotnet build Maui/Dentify.Maui/Dentify.Maui/Dentify.Maui.csproj -f net10.0-windows10.0.19041.0`; device builds are done by the developer in Visual Studio.
- Keep changes small and focused. Mention which files changed and what the developer must do manually (migrations, secrets, Azure portal steps).
- When a requirement above conflicts with a request, point it out instead of silently breaking it.
- Do not add libraries that are not in this file or in the naslag without asking.
