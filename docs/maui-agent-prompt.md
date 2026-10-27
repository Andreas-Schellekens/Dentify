# Prompt for the MAUI agent

Copy everything below the line into Claude Code, started in the repo root on branch `feature/maui-app`.

---

You are building the .NET MAUI app of **Dentify**, a school project (AI.NET Challenge 1, Thomas More). My teammate Andreas built the .NET API and the Azure integration; I (Finn) build the app. Reply to me in informal Belgian Dutch; all code, comments and commit messages in English.

**Read first, in this order:** `CLAUDE.md` (project rules, graded course requirements, MAUI conventions, API flow, status), then `ASSIGNMENT.md`. Then read the DTOs in `Backend/Dentify.Backend/Dentify.Contracts/Models/` and the controllers in `Backend/Dentify.Backend/Dentify.Api/Controllers/` so you know the exact endpoints and response shapes. Do not change anything under `Backend/` unless I ask; if the app needs an API change, tell me and I will pass it to Andreas.

**What the app does:** an employee hands over or takes back a vehicle. They scan the license plate (the API does OCR and finds the vehicle), take photos of the vehicle, and the API detects damage with a trained Custom Vision model and writes a short report. The app shows the vehicles, their inspection history and the result of each inspection with photos and damages.

**Graded requirements you must not break** (details in `CLAUDE.md`):
- MVVM Community Toolkit (`[ObservableProperty]`, `[RelayCommand]`), everything bound in XAML, **code-behind stays empty** (only `InitializeComponent()` and setting `BindingContext`), no `Clicked` handlers.
- At least 4 pages: `VehicleListPage` → `VehicleDetailPage` → `NewInspectionPage` → `InspectionResultPage` (a plate scan page or step is welcome).
- An `INavigationService` that wraps Shell navigation; view models never touch `Shell`.
- At least 2 messages with `WeakReferenceMessenger`: `VehicleSelectedMessage` and `InspectionSavedMessage` (optional `PlateScannedMessage`).
- One `ApiService` (typed `HttpClient`) is the only class that calls the API; the base URL comes from configuration because it is an Ngrok URL that changes. Send the header `ngrok-skip-browser-warning: true`. All calls async, with loading and error states.
- Photos with `MediaPicker.Default.CapturePhotoAsync()` (allow picking from the gallery too for testing), camera permissions requested properly (Android manifest and iOS plist).
- An attractive, user-friendly UI: consistent colors and styles in `Resources/Styles`, cards for vehicles and inspections, clear severity colors for damages, empty states.
- Use the DTOs from `Dentify.Contracts` (already referenced). Photo URLs in DTOs are relative: prefix them with the API base URL.
- Do not add NuGet packages without asking me first (`CommunityToolkit.Mvvm` is already installed).

**How to work:**
1. Start with a short plan: folders, pages, view models, services, messages, navigation routes, and which endpoint each page uses. Wait for my OK.
2. Build it step by step, smallest working slice first: `ApiService` + `VehicleListPage` against the running API, then detail, then the new inspection flow (plate scan → create inspection → upload photos → result), then polish.
3. After each step, build with `dotnet build Maui/Dentify.Maui/Dentify.Maui/Dentify.Maui.csproj -f net10.0-windows10.0.19041.0` and tell me what to test. I run device builds in Visual Studio.
4. Commit per working step on `feature/maui-app` when I ask; never commit secrets.

**Running the API for testing:** if the database does not exist yet, run `powershell -ExecutionPolicy Bypass -File tools\setup-dev.ps1`. Start the API with `dotnet run --project Backend/Dentify.Backend/Dentify.Api --launch-profile http` (Swagger at http://localhost:5152/swagger). Without Azure keys the API returns fake data (plate `1-ABC-123`, one minor scratch per photo), which is fine for building the app. For a phone or emulator run `ngrok http 5152` and use the https URL.

Begin by reading the files above and then give me the plan.
