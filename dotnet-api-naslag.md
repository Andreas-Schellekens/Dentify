# .NET API Naslag — Layered Architecture (cursus-opbouw)

> Algemeen stappenplan voor een nieuwe API met dezelfde opbouw als de cursus (CityInfo / MotoGP).
> Vervang `<App>` door je projectnaam (bv. `Shop`) en `Item` / `Category` door je eigen entities.
>
> **Stack:** .NET 10 · EF Core · SQL Server (LocalDB) · AutoMapper v16 · Swagger (Swashbuckle) · Auth0 (JWT) · optioneel Serilog

---

## Deel 0 — Architectuur in één oogopslag

```
<App>.sln
├── <App>.API                 → Presentation
│   ├── Controllers/
│   ├── MappingProfiles/      → ItemProfile : Profile
│   ├── Strategies/           → interfaces + implementaties (DI-strategy, bv. IEmailService)
│   ├── Program.cs
│   └── appsettings.json / .Development.json / .Production.json
├── <App>.Contracts           → DTO's (= het "contract" met de client)
│   └── Models/
├── <App>.Domain              → Core business
│   └── Entities/
├── <App>.Infrastructure      → Data layer
│   ├── DbContexts/           → <App>Context
│   ├── Services/             → IItemRepository + ItemRepository (+ enums zoals ItemInclude)
│   └── Migrations/
└── <App>.Shared              → enkel bij paging: PaginationMetadata
```

### Project-referenties ("de rode pijlen")

| Project | Verwijst naar |
|---|---|
| API | Contracts, Infrastructure (+ Shared bij paging) |
| Infrastructure | Domain (+ Shared bij paging) |
| Contracts | niets |
| Domain | niets |

> API heeft **geen** directe referentie naar Domain. Entities ziet de API transitief via Infrastructure.

### Vaste keuzes (cursus)

| Onderwerp | Keuze |
|---|---|
| Nullable | `<Nullable>annotations</Nullable>` in **alle** `.csproj` |
| Repository-interfaces | In **Infrastructure/Services/**, samen met de implementatie (niet in Contracts) |
| AutoMapper profiles | In **API/MappingProfiles/**, klasse per resource (`ItemProfile : Profile`) |
| Config entities | **Data annotations** op entities. Fluent API (`OnModelCreating`) enkel voor many-to-many + seeding (`HasData`) |
| Database | SQL Server, `(localdb)\MSSQLLocalDB` |
| Swagger | Swashbuckle (`AddSwaggerGen` / `UseSwaggerUI`), niet `AddOpenApi` |
| Controllers | `[ApiController]`, attribute routing, `ActionResult<T>`, async |
| Mapping | In de controller, niet in de repository |

### NuGet packages

| Package | Project |
|---|---|
| `Swashbuckle.AspNetCore` | API |
| `AutoMapper` (v16, **license key nodig**) | API |
| `Microsoft.EntityFrameworkCore.Design` | **API én Infrastructure** |
| `Microsoft.EntityFrameworkCore` | Infrastructure |
| `Microsoft.EntityFrameworkCore.SqlServer` | Infrastructure |
| `Microsoft.EntityFrameworkCore.Tools` | Infrastructure |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | API (bij auth) |
| `Bogus` | API (enkel voor een seeder) |

---

## Deel 1 — Solution opzetten

1. VS → **New Project → ASP.NET Core Web API**: naam `<App>.API`, solution `<App>`, .NET 10, OpenAPI/Swagger aan, controllers aan.
2. Solution → **Add → New Project → Class Library** (×3): `<App>.Domain`, `<App>.Contracts`, `<App>.Infrastructure`.
3. Verwijder alle `Class1.cs`.
4. Dubbelklik elke `.csproj` → `<Nullable>annotations</Nullable>`.
5. Project-referenties instellen (tabel hierboven).
6. Template opkuisen: `WeatherForecast` controller + klasse weg.
7. `Properties/launchSettings.json` → https-profiel: `"launchUrl": "swagger"`, `"launchBrowser": true`.
8. NuGet-packages installeren (tabel hierboven).

---

## Deel 2 — Entities (Domain/Entities)

Eén klasse per tabel. Patroon:

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace <App>.Domain.Entities
{
    public class Item
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        // Foreign key + navigation (many-kant)
        [ForeignKey("CategoryId")]
        public int CategoryId { get; set; }
        public Category Category { get; set; }

        // Constructor wijst de required properties effectief toe!
        public Item(string name)
        {
            Name = name;
        }
    }
}
```

**Relaties:**
- **One-to-many:** de "one"-kant heeft `ICollection<Item> Items { get; set; } = new List<Item>();`, de "many"-kant heeft FK + navigation object. Wordt automatisch herkend by convention.
- **Many-to-many:** eigen join-entity (`ItemTag`) met eigen `Id`, FK's + navigation objecten naar beide kanten. Beide kanten hebben `ICollection<ItemTag>`.
- Constructor-parameters **altijd toewijzen** aan de properties (klassieke fout: lege constructor body).

---

## Deel 3 — DTO's (Contracts/Models)

Naming strikt aanhouden:

| Type | Naam | Inhoud |
|---|---|---|
| Lijst | `ItemListDto` | enkel wat de lijst nodig heeft |
| Detail | `ItemDetailDto` | uitgebreider, evt. berekende velden |
| Aanmaken | `ItemForCreationDto` | validatie met `[Required(ErrorMessage = "{0} is required.")]`, `[MaxLength]`, ... |
| Volledig updaten | `ItemForUpdateDto` | zoals creation |
| Gedeeltelijk updaten | `ItemForPatchDto` | enkel de velden die patchbaar zijn |

Regels:
- DTO's bevatten **geen** annotations voor DB, wel validatie op creation/update/patch.
- FK's in creation-DTO: gewoon `int` (niet `int?`), anders krijg je `cannot convert from int? to int` in de repo-calls.
- Verschillende naam dan de entity? Dan `ForMember` in het profile (zie Deel 7).

---

## Deel 4 — DbContext (Infrastructure/DbContexts)

```csharp
using Microsoft.EntityFrameworkCore;
using <App>.Domain.Entities;

namespace <App>.Infrastructure.DbContexts
{
    public class <App>Context : DbContext
    {
        public DbSet<Item> Items { get; set; }
        public DbSet<Category> Categories { get; set; }

        public <App>Context(DbContextOptions<<App>Context> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Many-to-many via join-entity
            modelBuilder.Entity<Item>()
                .HasMany(i => i.ItemTags)
                .WithOne(it => it.Item)
                .HasForeignKey(it => it.ItemId);

            modelBuilder.Entity<Tag>()
                .HasMany(t => t.ItemTags)
                .WithOne(it => it.Tag)
                .HasForeignKey(it => it.TagId);

            // Seeding: IDs altijd expliciet meegeven
            modelBuilder.Entity<Category>().HasData(
                new Category("Books") { Id = 1 },
                new Category("Games") { Id = 2 }
            );
            modelBuilder.Entity<Item>().HasData(
                new Item("Dune") { Id = 1, CategoryId = 1 }
            );
        }
    }
}
```

- Eén `DbSet` per entity (de naam wordt de tabelnaam).
- Constructor met `DbContextOptions<...>` doorgeven aan `base`.
- Seed-volgorde: eerst "one"-kant (Category), dan "many"-kant (Item), dan join-entities.
- Geen `IDesignTimeDbContextFactory` — zit niet in de cursus. Zie Deel 6 voor de juiste instellingen.

---

## Deel 5 — Program.cs (volledige template, juiste volgorde)

**Regel:** alles met `builder.Services...` staat **vóór** `builder.Build()`. Alles na `Build()` is pipeline/middleware.

```csharp
using Microsoft.EntityFrameworkCore;
using <App>.Infrastructure.DbContexts;
using <App>.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------- SERVICES (vóór Build) ----------
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<<App>Context>(options =>
    options.UseSqlServer(
        builder.Configuration["ConnectionStrings:<App>DBConnectionString"]));

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// Repositories: altijd Scoped
builder.Services.AddScoped<IItemRepository, ItemRepository>();

// Strategy-services: Transient (bv. per environment)
// if (builder.Environment.IsDevelopment())
//     builder.Services.AddTransient<IEmailService, ConsoleEmailService>();
// else
//     builder.Services.AddTransient<IEmailService, MailtrapEmailService>();

// CORS (indien er een frontend is)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(b =>
        b.WithOrigins("http://localhost:5173")   // of AllowAnyOrigin() in dev
         .AllowAnyHeader()
         .AllowAnyMethod()
         .WithExposedHeaders("X-Pagination"));   // enkel bij paging
});

// Auth (zie Deel 12)
// builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//     .AddJwtBearer(options => { options.Authority = "..."; options.Audience = "..."; });

var app = builder.Build();

// ---------- PIPELINE (na Build) ----------
if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("EnableSwagger"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors();
app.UseAuthentication();   // vóór UseAuthorization
app.UseAuthorization();
app.MapControllers();

app.Run();
```

---

## Deel 6 — Configuratie + migrations

### appsettings

| Bestand | Inhoud |
|---|---|
| `appsettings.json` (root) | Connection string + gedeelde waarden (wordt **geërfd** door Development en Production) |
| `appsettings.Development.json` | AutoMapper license key, dev-email config, logging |
| `appsettings.Production.json` | `"EnableSwagger": true`, productie-email (Mailtrap), ... |

```json
{
  "ConnectionStrings": {
    "<App>DBConnectionString": "Server=(localdb)\\MSSQLLocalDB;Initial Catalog=<App>;Integrated Security=True;"
  },
  "AutoMapper": {
    "LicenseKey": "<jouw key van automapper.io>"
  }
}
```

> Connection string ophalen: **View → SQL Server Object Explorer** → rechtermuisknop op `(localdb)\MSSQLLocalDB` → Properties → Connection string kopiëren → `Initial Catalog=<App>;` toevoegen.
> De key in `Program.cs` en in appsettings moeten **letter voor letter** gelijk zijn, anders: "ConnectionString property has not been initialized".

### Migrations — Package Manager Console

**Vóór elk commando controleren:**
- **Default project** (dropdown in PMC) = `<App>.Infrastructure`
- **Startup project** (Solution Explorer → Set as Startup) = `<App>.API`
- `Microsoft.EntityFrameworkCore.Design` staat in **beide** projecten

```
Add-Migration InitialMigration
Update-Database
```

Daarna per wijziging een nieuwe migration met een beschrijvende naam:
```
Add-Migration AddItemDescription
Update-Database

Add-Migration SeedDatabase        (na HasData toevoegen)
Update-Database
```

Controleren: **SQL Server Object Explorer** → tabel → rechtermuisknop → **View Data**.

---

## Deel 7 — AutoMapper (API/MappingProfiles)

- NuGet `AutoMapper` v16 in API, **license key** in `appsettings.Development.json`.
- Registratie: `builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());`
- Profiles moeten in het **API-project** staan, anders worden ze niet gevonden.

```csharp
using AutoMapper;
using <App>.Contracts.Models;
using <App>.Domain.Entities;

namespace <App>.API.MappingProfiles
{
    public class ItemProfile : Profile
    {
        public ItemProfile()
        {
            // Zelfde veldnamen → geen ForMember nodig
            CreateMap<Item, ItemListDto>()
                .ForMember(dest => dest.CategoryName,
                    opt => opt.MapFrom(src => src.Category.Name))
                .ForMember(dest => dest.ShortDate,
                    opt => opt.MapFrom(src => src.Date.ToString("dd/MM")));

            CreateMap<Item, ItemDetailDto>();

            // Many-to-many "uitpakken"
            CreateMap<Tag, TagListDto>();
            CreateMap<Item, ItemWithTagsDto>()
                .ForMember(dest => dest.Tags,
                    opt => opt.MapFrom(src => src.ItemTags.Select(it => it.Tag)));

            // Terug richting entity
            CreateMap<ItemForCreationDto, Item>();
            CreateMap<ItemForPatchDto, Item>();
        }
    }
}
```

Valkuilen:
- Elke combinatie (bron → doel) die je in een controller mapt **moet** als `CreateMap` bestaan, anders: *"Missing type map configuration or unsupported mapping"*.
- Mapt een collectie-property (`ICollection<RiderDetailDto>`)? Dan moet de map voor het element-type ook bestaan, **en** moet de collectie ge-`Include`d zijn.
- DTO-property zonder bron in de entity (bv. `Duration`)? `.ForMember(d => d.Duration, o => o.Ignore())` en manueel invullen in de controller.
- Profiles ondersteunen **geen DI** → berekeningen met een geïnjecteerde service doe je in de controller na `_mapper.Map()`.
- Bestaande entity updaten: `_mapper.Map(dto, entity)` (twee argumenten).

---

## Deel 8 — Repository pattern (Infrastructure/Services)

Interface + implementatie **in dezelfde folder**. Altijd async, namen eindigen op `Async`.

```csharp
// Services/IItemRepository.cs
using <App>.Domain.Entities;

namespace <App>.Infrastructure.Services
{
    public interface IItemRepository
    {
        Task<IEnumerable<Item>> GetItemsAsync();
        Task<Item?> GetItemAsync(int itemId, ItemInclude include = ItemInclude.None);
        Task<bool> ItemExistsAsync(int itemId);
        Task AddItemAsync(Item item);
        Task<bool> SaveChangesAsync();
    }
}
```

```csharp
// Services/ItemRepository.cs
using Microsoft.EntityFrameworkCore;
using <App>.Domain.Entities;
using <App>.Infrastructure.DbContexts;

namespace <App>.Infrastructure.Services
{
    public class ItemRepository : IItemRepository
    {
        private <App>Context _context;

        public ItemRepository(<App>Context context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Item>> GetItemsAsync()
        {
            return await _context.Items
                .Include(i => i.Category)
                .OrderBy(i => i.Name)
                .ToListAsync();
        }

        public async Task<Item?> GetItemAsync(int itemId, ItemInclude include = ItemInclude.None)
        {
            var query = _context.Items.AsQueryable();

            query = include switch
            {
                ItemInclude.Category => query.Include(i => i.Category),
                ItemInclude.Tags => query.Include(i => i.ItemTags).ThenInclude(it => it.Tag),
                _ => query
            };

            return await query.FirstOrDefaultAsync(i => i.Id == itemId);
        }

        public async Task<bool> ItemExistsAsync(int itemId)
            => await _context.Items.AnyAsync(i => i.Id == itemId);

        public async Task AddItemAsync(Item item)
            => await _context.Items.AddAsync(item);

        public async Task<bool> SaveChangesAsync()
            => await _context.SaveChangesAsync() >= 0;
    }
}
```

```csharp
// Services/ItemInclude.cs
public enum ItemInclude { None, Category, Tags }
```

- Eén repository per entity. Controllers injecteren de **interface**, nooit de implementatie of de DbContext.
- Registratie: `AddScoped<IItemRepository, ItemRepository>()` (per request, zoals de DbContext).
- Heeft de response gerelateerde data nodig na een POST? Expliciet laden na `SaveChangesAsync`:
  ```csharp
  public async Task LoadCategoryForItemAsync(Item item)
      => await _context.Entry(item).Reference(i => i.Category).LoadAsync();
  ```

---

## Deel 9 — Controllers (API/Controllers)

### DI-patroon (altijd identiek)

```csharp
[ApiController]
[Route("api/[controller]")]
public class ItemsController : ControllerBase
{
    private IItemRepository _itemRepository;
    private IMapper _mapper;

    public ItemsController(IItemRepository itemRepository, IMapper mapper)
    {
        _itemRepository = itemRepository;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ItemListDto>>> GetItems()
    {
        var items = await _itemRepository.GetItemsAsync();
        return Ok(_mapper.Map<IEnumerable<ItemListDto>>(items));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ItemDetailDto>> GetItem(int id)
    {
        var item = await _itemRepository.GetItemAsync(id);
        if (item == null) return NotFound();
        return Ok(_mapper.Map<ItemDetailDto>(item));
    }

    [HttpPost]
    public async Task<ActionResult<ItemDetailDto>> CreateItem(ItemForCreationDto dto)
    {
        // Bestaan van FK's controleren
        if (!await _categoryRepository.CategoryExistsAsync(dto.CategoryId))
            return BadRequest($"Category with Id {dto.CategoryId} does not exist.");

        var item = _mapper.Map<Item>(dto);
        await _itemRepository.AddItemAsync(item);
        await _itemRepository.SaveChangesAsync();

        var toReturn = _mapper.Map<ItemDetailDto>(item);
        return CreatedAtAction(nameof(GetItem), new { id = toReturn.Id }, toReturn);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateItem(int id, ItemForUpdateDto dto)
    {
        var item = await _itemRepository.GetItemAsync(id);
        if (item == null) return NotFound();

        _mapper.Map(dto, item);
        await _itemRepository.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult> PatchItem(int id, ItemForPatchDto dto)
    {
        var item = await _itemRepository.GetItemAsync(id);
        if (item == null) return NotFound();

        _mapper.Map(dto, item);
        await _itemRepository.SaveChangesAsync();
        return NoContent();
    }
}
```

### Routes (child-resources)

- Controller-niveau: `[Route("api/[controller]")]` → prefix op controllernaam.
- Child-resource in dezelfde controller: `[HttpGet("{categoryId}/items")]`, `[HttpGet("{categoryId}/items/{itemId}")]`.
- **Twee actions met dezelfde route in verschillende controllers = `AmbiguousMatchException`.** Eén route, één plek.

### Status codes

| Code | Wanneer |
|---|---|
| 200 `Ok()` | GET gelukt |
| 201 `CreatedAtAction` | POST gelukt (verwijst naar de GET-by-id) |
| 204 `NoContent()` | PUT / PATCH / DELETE gelukt |
| 400 `BadRequest()` | ongeldige input (komt automatisch via `[ApiController]` + DTO-validatie) |
| 401 / 403 | niet ingelogd / niet toegestaan |
| 404 `NotFound()` | resource bestaat niet |
| 409 | conflict |
| 500 | serverfout |

---

## Deel 10 — Filtering, searching & paging

**Shared-project:** nieuwe Class Library `<App>.Shared`, referentie vanuit **API én Infrastructure**.

```csharp
namespace <App>.Shared
{
    public class PaginationMetadata
    {
        public int TotalItemCount { get; set; }
        public int TotalPageCount { get; set; }
        public int PageSize { get; set; }
        public int CurrentPage { get; set; }

        public PaginationMetadata(int totalItemCount, int pageSize, int currentPage)
        {
            TotalItemCount = totalItemCount;
            PageSize = pageSize;
            CurrentPage = currentPage;
            TotalPageCount = (int)Math.Ceiling(totalItemCount / (double)pageSize);
        }
    }
}
```

**Repository (Tuple als return):**

```csharp
Task<(IEnumerable<Item>, PaginationMetadata)> GetItemsAsync(
    string? searchQuery, int? categoryId, bool onlyOpen, int pageNumber, int pageSize);
```

```csharp
var collection = _context.Items.Include(i => i.Category).AsQueryable();   // deferred execution

if (!string.IsNullOrWhiteSpace(searchQuery))
    collection = collection.Where(i => i.Name.Contains(searchQuery));

if (categoryId.HasValue && categoryId > 0)
    collection = collection.Where(i => i.CategoryId == categoryId);

var totalCount = await collection.CountAsync();
var paginationMetadata = new PaginationMetadata(totalCount, pageSize, pageNumber);

var items = await collection
    .OrderBy(i => i.Name)
    .Skip(pageSize * (pageNumber - 1))
    .Take(pageSize)
    .ToListAsync();

return (items, paginationMetadata);
```

**Controller:**

```csharp
[HttpGet]
public async Task<ActionResult<IEnumerable<ItemListDto>>> GetItems(
    string? searchQuery, int? categoryId, bool onlyOpen = false,
    int pageNumber = 1, int pageSize = 10)
{
    if (pageSize > 50) pageSize = 50;   // max pageSize begrenzen

    var (items, paginationMetadata) = await _itemRepository.GetItemsAsync(
        searchQuery, categoryId, onlyOpen, pageNumber, pageSize);

    Response.Headers.Add("X-Pagination", JsonSerializer.Serialize(paginationMetadata));
    return Ok(_mapper.Map<IEnumerable<ItemListDto>>(items));
}
```

- `using <App>.Shared;` in repo én controller, plus `using System.Text.Json;`.
- CORS moet `.WithExposedHeaders("X-Pagination")` hebben, anders kan de frontend de header niet lezen.
- Filter-bool op de endpoint van een andere entity (bv. `?onlyWithItems=true`) → `collection.Where(c => c.Items.Any())`.

**Test-data seeden (Bogus):** NuGet `Bogus` in API, `Seeders/` folder + `SeedController` met POST-endpoint dat 1000 records genereert.

---

## Deel 11 — DI & Strategy Pattern

- **IoC** = principe, **DI** = pattern (constructor injection).
- Lifetimes: **Transient** (nieuw elke keer) · **Scoped** (per request: DbContext, repositories) · **Singleton** (één voor de hele app).
- Meerdere registraties van dezelfde interface → de **laatst** geregistreerde wint.

**Strategy:** één interface, meerdere implementaties; de controller kent enkel de interface.

```csharp
// API/Strategies/IPriceCalculator.cs
public interface IPriceCalculator { decimal Calculate(Item item); }

// Twee implementaties: StandardPriceCalculator, DiscountPriceCalculator

// Program.cs — wissel van gedrag door één regel te veranderen:
builder.Services.AddTransient<IPriceCalculator, StandardPriceCalculator>();
```

Berekende DTO-velden: manueel invullen in de controller na het mappen:
```csharp
var dto = _mapper.Map<ItemDetailDto>(item);
dto.Price = _priceCalculator.Calculate(item);
```

---

## Deel 12 — Authentication (Auth0 + JWT)

**Auth0 dashboard:**
1. **Single Page Application** aanmaken (= frontend) → `domain` + `clientId` noteren.
2. **API** aanmaken (= je .NET API) → identifier = API-url **zonder trailing slash** (bv. `https://localhost:7191`) = je `audience`.
3. Na eerste login-poging: **Allowed Callback URLs** en **Allowed Logout URLs** invullen (`http://localhost:5173`).

**API:**
```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://<tenant>.<regio>.auth0.com";   // met https://
        options.Audience = "https://localhost:<poort>";
    });

// pipeline:
app.UseAuthentication();   // vóór UseAuthorization
app.UseAuthorization();
```
Beveilig endpoints met `[Authorize]` (denk aan **alle** gevoelige endpoints, ook PATCH/PUT/DELETE).

**Frontend (React):**
- `npm install @auth0/auth0-react`
- `Auth0Provider` (domain, clientId, `authorizationParams: { redirect_uri: window.location.origin, audience, prompt: "login" }`) als wrapper rond de app.
- `useAuth0()` → `loginWithRedirect`, `logout`, `isAuthenticated`, `user`, `error`, `getAccessTokenSilently`.
- Token ophalen vóór de call en meesturen:
  ```javascript
  const token = await getAccessTokenSilently();
  apiClient.get('/items', { headers: { Authorization: `Bearer ${token}` } });
  ```

**User-based restriction:** Auth0 → Actions → Triggers → *post-login* → eigen action met een `allowedEmails`-lijst en `api.access.deny(...)`. Secret met de clientId van je SPA gebruiken zodat andere apps niet geraakt worden. Deployen en op de trigger slepen.

---

## Deel 13 — Logging, environments & email

**Environments**
- Bepaald via `ASPNETCORE_ENVIRONMENT` in `launchSettings.json`.
- Services registreren per environment: `builder.Environment.IsDevelopment()` (vóór Build!). Na Build gebruik je `app.Environment`.

**Seeder beschermen in productie**
```csharp
// Optie 1 — runtime: controller blijft zichtbaar, geeft 403
private IWebHostEnvironment _env;
public SeedController(IWebHostEnvironment env) { _env = env; }
...
if (!_env.IsDevelopment()) return Forbid();

// Optie 2 — compile-time: controller bestaat niet meer in release
#if DEBUG
[ApiController]
[Route("api/seed")]
public class SeedController : ControllerBase { ... }
#endif
```

**Swagger in productie:** `"EnableSwagger": true` in `appsettings.Production.json` + de conditie in Program.cs (zie Deel 5).

**Email via Strategy (dev vs prod)**
```csharp
public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
}
// ConsoleEmailService  → schrijft naar console (Development)
// MailtrapEmailService → SMTP via Mailtrap (Production), config uit appsettings
```
Config-keys (`From`, `Host`, `Port`, `Username`, `Password`) in `appsettings.Development.json` / `appsettings.Production.json`. Inject `IEmailService` in de controller en roep `SendEmailAsync` aan na het opslaan. Heb je gerelateerde data nodig in de mail (bv. de race/categorie-naam)? Zorg dat die ge-`Include`d of geladen is.

**ILogger**
- By default beschikbaar → `ILogger<ItemsController>` injecteren in de constructor.
- Levels: Trace < Debug < Information < Warning < Error < Critical.
- Minimum level instellen in appsettings onder `Logging:LogLevel`.
- **Serilog** (logging naar bestand): NuGet `Serilog.AspNetCore` + `Serilog.Sinks.File`:
  ```csharp
  Log.Logger = new LoggerConfiguration()
      .MinimumLevel.Debug()
      .WriteTo.Console()
      .WriteTo.File("logs/<app>.txt", rollingInterval: RollingInterval.Day)
      .CreateLogger();
  builder.Host.UseSerilog();
  ```

---

## Deel 14 — Typische errors en de oplossing

| Foutmelding | Oorzaak → oplossing |
|---|---|
| *Your startup project doesn't reference Microsoft.EntityFrameworkCore.Design* | `Design`-package ook in het **API**-project installeren |
| *No DbContext was found in assembly …API* | PMC **Default project** op `Infrastructure` zetten, Startup op API |
| *Unable to resolve service for type DbContextOptions…* | Meestal DbContext niet (juist) geregistreerd, of verkeerd Default/Startup project. Eerst Program.cs en PMC-instellingen checken |
| *The service collection cannot be modified because it is read-only* | `builder.Services.Add…` staat **na** `builder.Build()` → verplaatsen |
| *The ConnectionString property has not been initialized* | Key-naam in appsettings ≠ key in Program.cs, of connection string staat in het verkeerde bestand |
| *AutoMapperMappingException: Missing type map configuration* | Ontbrekende `CreateMap<Bron, Doel>()`, of profile staat niet in het API-project |
| *AmbiguousMatchException: multiple endpoints* | Twee actions met dezelfde route → één weghalen/verplaatsen |
| *cannot convert from int? to int* | DTO-property is `int?` → `int` maken (required) of `.Value` gebruiken |
| *PaginationMetadata could not be found* | Project-referentie naar `Shared` ontbreekt, of `using <App>.Shared;` mist |
| CORS-errors in de browser | `UseCors()` vóór `UseAuthorization()`, juiste origin, en `WithExposedHeaders("X-Pagination")` bij paging |
| Swagger start niet op | `launchUrl: "swagger"` in launchSettings, `UseSwagger`/`UseSwaggerUI` in Development |
| Geseedde data verschijnt niet | Migration na `HasData` vergeten + `Update-Database` |
| `Ticket.Race` / navigation is `null` | `.Include()` / `.ThenInclude()` vergeten, of na POST expliciet `LoadAsync()` nodig |

---

## Deel 15 — Checklist: nieuw project van nul

**Setup**
- [ ] Solution + 4 projecten, referenties volgens de tabel
- [ ] `<Nullable>annotations</Nullable>` in alle `.csproj`
- [ ] NuGet-packages (Design in API én Infrastructure)
- [ ] `launchSettings.json` → swagger als startpagina

**Data**
- [ ] Entities met annotations + constructors die toewijzen
- [ ] DbContext met DbSets, many-to-many en `HasData` in `OnModelCreating`
- [ ] Connection string in `appsettings.json` (root)
- [ ] `AddDbContext` vóór `Build()`
- [ ] Default = Infrastructure, Startup = API → `Add-Migration` + `Update-Database`

**Logica**
- [ ] DTO's (`List`, `Detail`, `ForCreation`, `ForUpdate`, `ForPatch`) met juiste naming
- [ ] AutoMapper: license key, `AddAutoMapper(...)`, profiles in API/MappingProfiles
- [ ] Repositories in Infrastructure/Services (interface + implementatie, async, `Async`-suffix)
- [ ] `AddScoped<IXRepository, XRepository>()` per repository
- [ ] Controllers: interface + `IMapper` injecteren, juiste status codes, `CreatedAtAction` bij POST

**Uitbreidingen**
- [ ] Filtering/searching/paging (+ Shared-project, `X-Pagination`, CORS expose)
- [ ] Strategy-services (`AddTransient`)
- [ ] CORS + `UseCors()` vóór `UseAuthorization()`
- [ ] Auth: Authority met `https://`, Audience zonder slash, `UseAuthentication()` vóór `UseAuthorization()`, `[Authorize]` op alle gevoelige endpoints
- [ ] Seeder afschermen in productie
- [ ] Dev/Prod appsettings + environment-afhankelijke registraties

**Testen**
- [ ] Elk endpoint via Swagger testen (200 / 201 / 204 / 400 / 404)
- [ ] Beveiligde endpoints testen zónder token (moet 401 geven)
