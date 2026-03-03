# Unity Project Coding Guidelines

*Version 2.0 - Comprehensive Coding Standard*

## Table of Contents

1. [Architecture Overview](#1-architecture-overview)
2. [MonoBehaviour Lifecycle](#2-monobehaviour-lifecycle)
3. [App Class - Application Bootstrap](#3-app-class---application-bootstrap)
4. [Service Layer](#4-service-layer)
5. [Manager Layer](#5-manager-layer)
6. [Controller Layer (UI)](#6-controller-layer-ui)
7. [Event System](#7-event-system)
8. [Data Management and Persistence](#8-data-management-and-persistence)
9. [UI Toolkit Development](#9-ui-toolkit-development)
10. [Naming Conventions](#10-naming-conventions)
11. [Code Organization](#11-code-organization)
12. [Error Handling](#12-error-handling)
13. [Performance and Best Practices](#13-performance-and-best-practices)
14. [Testing](#14-testing)
15. [Folder Structure](#15-folder-structure)

---

## 1. Architecture Overview

This project follows a **Service-Manager-Controller** architecture with strict separation of concerns.

```
┌─────────────────────────────────────────────────────┐
│                     App (Bootstrap)                  │
│         Initializes Services, then Managers          │
├─────────────────────────────────────────────────────┤
│                                                      │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐ │
│  │  Services    │  │  Managers   │  │ Controllers │ │
│  │             │  │             │  │   (UI)      │ │
│  │ Always run  │  │ Logic +     │  │ Bound to    │ │
│  │ Core infra  │  │ Data        │  │ UI elements │ │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘ │
│         │                │                │         │
│         └────────────────┼────────────────┘         │
│                          │                           │
│                   EventService                       │
│              (Communication Bus)                     │
│                                                      │
├─────────────────────────────────────────────────────┤
│                    Data Layer                        │
│        Objects, DTOs, ScriptableObjects              │
└─────────────────────────────────────────────────────┘
```

### Layer Definitions

| Layer | Base Class | Purpose | Lifetime |
|-------|-----------|---------|----------|
| **Service** | Plain C# class (no MonoBehaviour) | Core infrastructure that must always run. Provides foundational capabilities to the entire application. | Entire application lifetime |
| **Manager** | `BaseManager<T>` | Handles business logic and data. Owns and mutates state. Communicates via events. | Scene or application lifetime |
| **Controller** | `BaseUiController<T>` | Always bound to UI. Displays values, links buttons to Manager methods. Never owns business logic. | Bound to UI Document lifetime |

### Key Rules

- **Services** never depend on Managers or Controllers.
- **Managers** may use Services but never reference Controllers.
- **Controllers** may read from Managers and use Services, but all mutations go through Managers.
- All cross-layer communication happens through the **EventService**.

---

## 2. MonoBehaviour Lifecycle

Understanding Unity's execution order is fundamental. Methods execute in this order:

```
Awake() → OnEnable() → Start() → FixedUpdate() → Update() → LateUpdate() → OnDisable() → OnDestroy()
```

### Lifecycle Method Usage

| Method | When It Runs | Use For |
|--------|-------------|---------|
| `Awake()` | Once, when the object is created (even if disabled) | Self-initialization: cache component references, register events |
| `OnEnable()` | Each time the object is enabled | Subscribe to events, reset state on re-enable |
| `Start()` | Once, before the first Update (only if enabled) | Cross-object initialization that depends on other objects' `Awake()` completing |
| `FixedUpdate()` | Fixed timestep (default 0.02s) | Physics calculations, Rigidbody manipulation |
| `Update()` | Once per frame | Input handling, game logic, non-physics movement |
| `LateUpdate()` | Once per frame, after all `Update()` calls | Camera follow, post-processing of frame data |
| `OnDisable()` | Each time the object is disabled | Unsubscribe from events, pause behavior |
| `OnDestroy()` | When the object is destroyed | Final cleanup, release resources |

### Rules

- **Never** put heavy processing in `Update()`. Prefer event-driven design.
- **Always** cache `GetComponent<T>()` results in `Awake()` or `Start()` - never call them in `Update()`.
- **Physics** code belongs in `FixedUpdate()`. Do not multiply by `Time.deltaTime` in `FixedUpdate()`.
- **Input** handling belongs in `Update()`, never in `FixedUpdate()`.
- **Subscribe** to events in `OnEnable()`, **unsubscribe** in `OnDisable()` (handled automatically by base classes).

---

## 3. App Class - Application Bootstrap

The `App` class is the single entry point for the entire application. It initializes Services first, then Managers.

```csharp
namespace PM.Core
{
    public class App : MonoBehaviour
    {
        // Static service references - available globally
        public static ConfigurationService ConfigurationService { get; private set; }
        public static EventService EventService { get; private set; }
        public static Log Log { get; private set; }
        public static LocalizationService LocalizationService { get; private set; }
        public static AdService AdService { get; private set; }

        // Manager registry - populated in Inspector
        [SerializeField] private List<MonoBehaviour> Managers;

        private void Awake()
        {
            InitializeServices();
            InitializeManagers();
        }

        private void InitializeServices()
        {
            ConfigurationService = new ConfigurationService();
            EventService = new EventService();
            Log = new Log();
            LocalizationService = new LocalizationService();
            AdService = new AdService();
        }

        private void InitializeManagers()
        {
            foreach (var manager in Managers)
            {
                if (manager is IManager initManager)
                {
                    if (!initManager.Init())
                        Log.Warning($"Could not initialize {manager.GetType().Name}");
                }
            }
        }

        #if UNITY_EDITOR
        [ContextMenu("Load All Managers")]
        private void LoadAllManagers()
        {
            var allManagers = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .Where(m => m is IManager)
                .ToList();

            foreach (var manager in allManagers.Where(manager => !Managers.Contains(manager)))
                Managers.Add(manager);
        }
        #endif
    }
}
```

### Initialization Order

1. `App.Awake()` is called by Unity.
2. Services are created (plain C# objects, no MonoBehaviour dependency).
3. Managers are initialized via `IManager.Init()` in the order defined in the Inspector list.
4. `DataManager` loads persisted data after a short delay and distributes it via Load events.

---

## 4. Service Layer

Services are core infrastructure components that **must always run**. They provide foundational capabilities that the entire application depends on.

### Characteristics

- **Plain C# classes** - they do NOT inherit from `MonoBehaviour` or `BaseManager<T>`.
- **Created and owned by App** - instantiated in `App.InitializeServices()`.
- **Accessed statically** via `App.ServiceName`.
- **Stateless or self-contained** - they do not depend on Managers or Controllers.
- **Always available** - from the moment `App.Awake()` completes until application shutdown.

### Existing Services

| Service | Purpose |
|---------|---------|
| `ConfigurationService` | Loads and manages project configuration (log level, feature flags, etc.) from INI files |
| `EventService` | Central event bus for all cross-layer communication |
| `Log` | Centralized logging (replaces `Debug.Log`) |
| `LocalizationService` | Text localization and translation |
| `AdService` | Advertisement management |

### Service Implementation Example

```csharp
namespace PM.Service
{
    public class ConfigurationService
    {
        private Dictionary<string, NameValueCollection> _sections;
        private readonly Dictionary<string, object> _cache;

        public ConfigurationService()
        {
            _sections = new Dictionary<string, NameValueCollection>();
            _cache = new Dictionary<string, object>();
        }

        // Async loading from StreamingAssets
        public async UniTask LoadFileContent(string filePath) { /* ... */ }

        // Type-safe value retrieval with caching and default fallback
        public T GetValue<T>(string section, string key, T defaultValue = default)
        {
            string cacheKey = $"{section}.{key}";
            if (_cache.TryGetValue(cacheKey, out var cached))
                return (T)cached;

            // Parse from INI sections, convert, cache, return
            // ...
        }
    }
}
```

### When to Create a New Service

Create a Service when:
- The functionality is needed by the entire application.
- It must be available before any Manager initializes.
- It has no dependency on Unity's GameObject/Component system.
- It provides infrastructure (not business logic).

Examples: analytics, remote config, crash reporting, network connectivity checking.

---

## 5. Manager Layer

Managers handle **business logic and data**. They own state, process rules, and communicate changes via events.

### Characteristics

- **Inherit from `BaseManager<T>`** - provides singleton pattern and event registration.
- **Own and mutate data** - the single source of truth for their domain.
- **Communicate via EventService** - never directly reference Controllers.
- **Expose read-only state** - public properties use `IReadOnlyList<T>` or readonly accessors.

### BaseManager&lt;T&gt; Base Class

```csharp
public abstract class BaseManager<T> : MonoBehaviour, IManager where T : BaseManager<T>
{
    public static T Instance;                                    // Singleton access
    private readonly List<EventRegistration> _eventRegistrations;

    public virtual bool Init();            // Called by App during initialization
    protected virtual void OnInit();       // Override for manager-specific setup

    // Event registration (auto subscribe/unsubscribe in OnEnable/OnDisable)
    protected void RegisterEvent<TR>(EventKeys eventKey, Action<TR> handler);
    protected void RegisterEvent(EventKeys eventKey, Action handler);
}
```

### Manager Implementation Template

```csharp
namespace PM.Manager
{
    public class ExampleManager : BaseManager<ExampleManager>
    {
        // 1. Constants
        private const int MAX_ITEMS = 100;

        // 2. Serialized fields
        [SerializeField] private float _spawnRate = 1.0f;
        [SerializeField, Range(0f, 1f)] private float _probability = 0.5f;

        // 3. Private fields
        private ExampleCollectionData _data = new ExampleCollectionData();
        private readonly List<Item> _items = new List<Item>();

        // 4. Properties (read-only public access)
        public bool IsReady { get; private set; }
        public IReadOnlyList<Item> Items => _items;

        // 5. Unity lifecycle / initialization
        private void Awake()
        {
            RegisterEvent<ExampleCollectionData>(EventKeys.LoadExample, OnLoadExample);
        }

        protected override void OnInit()
        {
            InitializeDefaultData();
        }

        // 6. Public interface methods
        public void AddItem(Item item)
        {
            _items.Add(item);
            App.EventService.Notify(EventKeys.ChangeExample, GetCurrentData());
        }

        public bool CanAddItem()
        {
            return _items.Count < MAX_ITEMS;
        }

        // 7. Private helper methods
        private void InitializeDefaultData()
        {
            _data = new ExampleCollectionData();
            IsReady = true;
        }

        private ExampleCollectionData GetCurrentData()
        {
            return _data;
        }

        // 8. Event handlers
        private void OnLoadExample(ExampleCollectionData data)
        {
            if (data == null)
            {
                App.Log.Warning("Loaded data is null, initializing defaults.");
                _data = new ExampleCollectionData();
                return;
            }

            _data = data;
        }

        // 9. Editor-only debug utilities
        #if UNITY_EDITOR
        [ContextMenu("Debug Info")]
        private void DebugInfo()
        {
            App.Log.Debug($"Items: {_items.Count}, Ready: {IsReady}");
        }
        #endif
    }
}
```

### Manager Categories

| Category | Examples | Responsibility |
|----------|----------|---------------|
| **Data** | `DataManager` | Central save/load hub |
| **Business Logic** | `BrewManager`, `CustomerManager`, `ShopManager` | Core game mechanics |
| **System** | `AudioManager`, `OptionsManager`, `NetworkManager` | System-level features |
| **Game State** | `QuestManager`, `AchievementManager`, `StatisticManager` | Progress and tracking |

---

## 6. Controller Layer (UI)

Controllers are **always bound to UI**. They display values from Managers and link user interactions (button clicks, input fields) back to Manager methods. Controllers never contain business logic.

### Characteristics

- **Inherit from `BaseUiController<T>`** - provides UIDocument integration and event registration.
- **Require a UIDocument component** - enforced by `[RequireComponent(typeof(UIDocument))]`.
- **Display only** - read data from events/Managers, write to UI elements.
- **Delegate actions** - button clicks call Manager methods, never process logic directly.
- **Use UI Toolkit exclusively** - `VisualElement`, `Label`, `Button`, `ListView`, etc. No legacy UI (Canvas/UGUI) or TextMeshPro.

### BaseUiController&lt;T&gt; Base Class

```csharp
[RequireComponent(typeof(UIDocument))]
public abstract class BaseUiController<T> : MonoBehaviour, IUiController where T : BaseUiController<T>
{
    public bool IsVisible { get; private set; }
    public UIDocument RootDocument { get; private set; }
    public VisualElement RootVisualElement { get; private set; }

    public virtual void Show(bool force);
    public virtual void Hide(bool force);
    public virtual void DelayedHide(bool force, float delay = 0.5f);

    // Localization helper - uses controller class name as section key
    public string GetText(string key);

    // Event registration (same API as BaseManager)
    protected void RegisterEvent<TR>(EventKeys eventKey, Action<TR> handler);
    protected void RegisterEvent(EventKeys eventKey, Action handler);
}
```

### Controller Implementation Template

```csharp
namespace PM.UiController
{
    public class ShopUiController : BaseUiController<ShopUiController>
    {
        // UI element references (cached)
        private Button _buyButton;
        private Label _priceLabel;
        private Label _goldLabel;
        private ListView _itemListView;
        private VisualElement _emptyState;
        private VisualTreeAsset _itemTemplate;

        // Local data for display
        private List<ShopItemData> _shopItems = new List<ShopItemData>();
        private double _currentGold;

        protected override void OnAwake()
        {
            // Register for data events
            RegisterEvent<List<ShopItemData>>(EventKeys.LoadShopItems, OnShopItemsLoaded);
            RegisterEvent<double>(EventKeys.ChangeGold, OnGoldChanged);

            InitializeUI();
            LoadTemplates();
        }

        protected override void OnShow()
        {
            RefreshUI();
        }

        private void InitializeUI()
        {
            // Query UI elements from the UIDocument
            _buyButton = RootVisualElement.Q<Button>("BuyButton");
            _priceLabel = RootVisualElement.Q<Label>("PriceLabel");
            _goldLabel = RootVisualElement.Q<Label>("GoldLabel");
            _itemListView = RootVisualElement.Q<ListView>("ItemListView");
            _emptyState = RootVisualElement.Q<VisualElement>("EmptyState");

            // Validate critical elements
            if (_buyButton == null)
            {
                App.Log.Error("BuyButton not found in ShopUiController");
                return;
            }

            if (_itemListView == null)
            {
                App.Log.Error("ItemListView not found in ShopUiController");
                return;
            }

            // Register button callbacks - delegate to Manager
            _buyButton.RegisterCallback<ClickEvent>(OnBuyButtonClicked);

            SetupListView();
        }

        private void LoadTemplates()
        {
            _itemTemplate = Resources.Load<VisualTreeAsset>("Ui/Templates/ShopItem");
            if (_itemTemplate == null)
                App.Log.Error("ShopItem template not found in Resources");
        }

        private void SetupListView()
        {
            _listView.makeItem = () =>
            {
                if (_itemTemplate != null)
                    return _itemTemplate.CloneTree();

                var fallback = new VisualElement();
                fallback.Add(new Label("Template missing"));
                return fallback;
            };

            _listView.bindItem = (element, index) =>
            {
                if (index < 0 || index >= _shopItems.Count) return;

                var item = _shopItems[index];
                var nameLabel = element.Q<Label>("ItemName");
                var priceLabel = element.Q<Label>("ItemPrice");

                if (nameLabel != null)
                    nameLabel.text = item.Name;
                if (priceLabel != null)
                    priceLabel.text = item.Price.ToString("F0");
            };

            _listView.itemsSource = _shopItems;
        }

        // Button click -> delegate to Manager (no logic here)
        private void OnBuyButtonClicked(ClickEvent evt)
        {
            ShopManager.Instance.BuySelectedItem();
        }

        // Event handlers -> update UI
        private void OnShopItemsLoaded(List<ShopItemData> items)
        {
            _shopItems = items ?? new List<ShopItemData>();
            RefreshUI();
        }

        private void OnGoldChanged(double gold)
        {
            _currentGold = gold;
            if (_goldLabel != null)
                _goldLabel.text = gold.ToString("F0");
        }

        private void RefreshUI()
        {
            if (_itemListView == null) return;

            _itemListView.itemsSource = _shopItems;
            _itemListView.RefreshItems();
            UpdateEmptyState();
        }

        private void UpdateEmptyState()
        {
            if (_emptyState == null) return;

            bool isEmpty = _shopItems == null || _shopItems.Count == 0;
            _emptyState.style.display = isEmpty ? DisplayStyle.Flex : DisplayStyle.None;
            _itemListView.style.display = isEmpty ? DisplayStyle.None : DisplayStyle.Flex;
        }

        #if UNITY_EDITOR
        [ContextMenu("Refresh UI")]
        private void DebugRefreshUI() => RefreshUI();
        #endif
    }
}
```

### Controller Rules

1. **Never** put business logic in a Controller. If a button triggers a purchase, the Controller calls `ShopManager.Instance.BuySelectedItem()`. The Manager handles validation, gold deduction, and inventory changes.
2. **Always** cache UI element references in `InitializeUI()` called from `OnAwake()`.
3. **Always** null-check queried elements and log errors for missing ones.
4. **Always** handle empty states in lists.
5. **Use `RegisterCallback<ClickEvent>`** for button interactions, not `clicked +=`.
6. **Use `ListView`** with virtualization for any list that could grow large.

---

## 7. Event System

All cross-layer communication happens through the `EventService`. This decouples Managers from Controllers and Managers from each other.

### EventService API

```csharp
public class EventService
{
    private readonly Dictionary<EventKeys, EventData> Events;

    // Typed events
    public void Subscribe<T>(EventKeys key, Action<T> action);
    public void Unsubscribe<T>(EventKeys key, Action<T> action);
    public void Notify<T>(EventKeys key, T args);

    // Parameterless events
    public void Subscribe(EventKeys key, Action action);
    public void Unsubscribe(EventKeys key, Action action);
    public void Notify(EventKeys key);
}
```

### Key Features

- **WeakReference caching**: Last notified data is cached so late subscribers receive it automatically.
- **Automatic replay**: When a Controller subscribes to `LoadGold` after the data was already loaded, it immediately receives the cached value.
- **Type safety**: Generic methods enforce correct types at compile time.
- **Automatic lifecycle**: Base classes handle subscribe/unsubscribe in `OnEnable`/`OnDisable` via reflection.

### EventKeys Organization

Events are grouped by numerical ranges:

```csharp
public enum EventKeys
{
    // 100-199: Change events (trigger data persistence)
    ChangeGold = 100,
    ChangeReputation = 101,
    ChangeItems = 102,

    // 200-299: Load events (distribute loaded data)
    LoadGold = 200,
    LoadReputation = 201,
    LoadItems = 202,

    // 300-399: User action events (player interactions)
    ResolveOrder = 301,
    BuyIngredient = 306,

    // 400-499: Room/Scene events
    ChangeRoom = 400,

    // 500-599: Ad events
    ShowAdForReward = 500,

    // 600+: System/data events
    LoadingFinish = 600
}
```

### Event Registration Pattern

```csharp
// In Awake() for Managers or OnAwake() for Controllers
RegisterEvent<DataType>(EventKeys.LoadData, OnLoadData);
RegisterEvent(EventKeys.SomeEvent, OnSomeEvent);

// Handler naming: always prefix with "On"
private void OnLoadData(DataType data) { /* ... */ }
private void OnSomeEvent() { /* ... */ }
```

### Data Flow Through Events

```
Manager changes data
    → App.EventService.Notify(EventKeys.ChangeX, data)
        → DataManager receives ChangeX → saves via PmPrefs
        → Other Managers can react to ChangeX
        → Controllers receive ChangeX → update UI
```

---

## 8. Data Management and Persistence

### DataManager - Central Persistence Hub

The `DataManager` is a specialized Manager that acts as the central save/load coordinator.

```csharp
public class DataManager : BaseManager<DataManager>
{
    private void Awake()
    {
        // Register Change events for automatic saving
        RegisterEvent<double>(EventKeys.ChangeGold, gold =>
            PmPrefs.Save(SaveKeys.Gold, gold));
        RegisterEvent<List<BrewedPotion>>(EventKeys.ChangeBrewedPotion, potions =>
            PmPrefs.Save(SaveKeys.BrewedPotions, potions));

        // Load data after a delay to ensure all Managers are initialized
        Invoke(nameof(LoadGameData), LoadingDelay);
    }

    private void LoadGameData()
    {
        App.EventService.Notify(EventKeys.LoadGold,
            PmPrefs.Load<SaveKeys, double>(SaveKeys.Gold));
        App.EventService.Notify(EventKeys.LoadBrewedPotion,
            PmPrefs.Load<SaveKeys, List<BrewedPotion>>(SaveKeys.BrewedPotions));

        App.EventService.Notify(EventKeys.LoadingFinish);
    }
}
```

### Data Object Patterns

#### Collection Data (for Managers with multiple related items)

```csharp
namespace PM.Objects.Data
{
    public class QuestCollectionData
    {
        public List<QuestData> LongtermQuests { get; set; } = new List<QuestData>();
        public List<QuestData> DailyQuests { get; set; } = new List<QuestData>();
        public QuestData ActiveRandomQuest { get; set; }
    }
}
```

#### Individual Data

```csharp
namespace PM.Objects.Data
{
    public class QuestData
    {
        public string QuestId;
        public QuestType Type;
        public int CurrentLevel;
        public DateTime LastCompletionDate;
        public bool IsActive;

        // Parameterless constructor required for serialization
        public QuestData() { }

        // Convenience constructor
        public QuestData(string questId, QuestType type)
        {
            QuestId = questId;
            Type = type;
        }
    }
}
```

### SaveKeys Enumeration

```csharp
public enum SaveKeys
{
    Gold,
    Reputation,
    Ingredients,
    BrewedPotions,
    PotionsKnowledge,
    // Add new keys as data types are introduced
}
```

### Load Handler Pattern

Every Manager that owns persisted data must implement this pattern:

```csharp
private void OnLoadData(SomeCollectionData data)
{
    if (data == null)
    {
        App.Log.Warning("Loaded data is null, initializing defaults.");
        _data = new SomeCollectionData();
        return;
    }

    _data = data;
}
```

---

## 9. UI Toolkit Development

This project uses **Unity UI Toolkit** exclusively for all UI. Do not use legacy Canvas/UGUI or TextMeshPro.

### UI Toolkit Core Concepts

| Concept | Description |
|---------|-------------|
| **UXML** | XML-based markup for UI structure (like HTML) |
| **USS** | Stylesheet language for UI styling (like CSS) |
| **VisualElement** | Base class for all UI elements (like DOM elements) |
| **UIDocument** | MonoBehaviour component that loads a UXML file into the scene |

### File Naming and Organization

| Type | Naming | Location |
|------|--------|----------|
| **UXML Document** | `PascalCase.uxml` (e.g., `ShopView.uxml`) | `Ui/Documents/` |
| **USS Stylesheet** | `kebab-case.uss` (e.g., `shop-view.uss`) | `Ui/Styles/` |
| **Controller** | `PascalCaseUiController.cs` (e.g., `ShopUiController.cs`) | `Scripts/UiController/` |
| **UXML Templates** | `PascalCase.uxml` (e.g., `ShopItem.uxml`) | `Resources/Ui/Templates/` |
| **Themes** | Stored in | `Ui/Themes/` |
| **Settings** | Stored in | `Ui/Settings/` |

### USS Best Practices

Use **BEM (Block Element Modifier)** naming convention for USS classes:

```css
/* Block */
.shop-panel { }

/* Element */
.shop-panel__header { }
.shop-panel__item-list { }
.shop-panel__buy-button { }

/* Modifier */
.shop-panel__buy-button--disabled { }
.shop-panel__item--selected { }
```

**Rules:**
- Use USS files instead of inline styles for memory efficiency.
- Keep selectors short - avoid deeply nested selectors.
- Avoid type selectors (`Button`, `Label`) in production USS. Use class selectors instead.
- Minimize use of `:hover` pseudo-class as it triggers re-styling on every pointer move.

### Querying UI Elements

Use `Q<T>()` for single elements and `Query<T>()` for collections:

```csharp
// Single element by name
var button = RootVisualElement.Q<Button>("BuyButton");

// Single element by class
var header = RootVisualElement.Q<VisualElement>(className: "shop-panel__header");

// Multiple elements
var allLabels = RootVisualElement.Query<Label>().ToList();

// Nested query
var itemName = listItem.Q<Label>("ItemName");
```

**Always** cache queried elements in private fields during initialization. Querying is relatively costly and must not be done every frame.

### Registering UI Callbacks

```csharp
// Button click
_buyButton.RegisterCallback<ClickEvent>(OnBuyClicked);

// Value change (TextField, Toggle, Slider, etc.)
_searchField.RegisterValueChangedCallback(OnSearchChanged);

// Custom event data
_element.RegisterCallback<PointerDownEvent, int>(OnPointerDown, itemIndex);
```

### Show/Hide UI Elements

```csharp
// Hide an element (keeps it in hierarchy, no layout cost)
element.style.display = DisplayStyle.None;

// Show an element
element.style.display = DisplayStyle.Flex;

// Toggle visibility (still takes layout space)
element.style.visibility = Visibility.Hidden;
element.style.visibility = Visibility.Visible;
```

**Prefer hiding over removing.** `RemoveFromHierarchy()` causes garbage collection and is costly to reverse. Only remove elements when they are truly no longer needed.

### ListView - Virtualized Lists

Use `ListView` for any list that may contain many items. It virtualizes elements and only renders visible ones.

```csharp
private void SetupListView()
{
    _listView.makeItem = () => _itemTemplate.CloneTree();

    _listView.bindItem = (element, index) =>
    {
        if (index < 0 || index >= _data.Count) return;

        var item = _data[index];
        element.Q<Label>("Name").text = item.Name;
        element.Q<Label>("Value").text = item.Value.ToString();
    };

    _listView.itemsSource = _data;
}

// After data changes:
_listView.itemsSource = _data;
_listView.RefreshItems();
```

### Localization in Controllers

Controllers access localized text through the base class helper:

```csharp
// Uses the controller's class name as the localization section
_titleLabel.text = GetText("shop_title");
_buyButton.text = GetText("buy_button");
```

---

## 10. Naming Conventions

### Classes and Types

| Type | Convention | Example |
|------|-----------|---------|
| Classes | PascalCase | `CustomerManager`, `BrewedPotion` |
| Interfaces | PascalCase with `I` prefix | `IManager`, `IUiController` |
| Abstract classes | PascalCase with `Base` prefix | `BaseManager`, `BaseUiController` |
| Enums | PascalCase | `EventKeys`, `CustomerType` |
| Enum values | PascalCase | `ChangeGold`, `LoadReputation` |

### Members

| Type | Convention | Example |
|------|-----------|---------|
| Constants | UPPER_CASE | `MAX_ITEM_COUNT` |
| Private fields | `_camelCase` | `_currentPotion`, `_items` |
| SerializeField | `_camelCase` | `[SerializeField] private float _spawnRate` |
| Properties | PascalCase | `IsReady`, `CurrentPotion` |
| Public methods | PascalCase, verb-based | `CalculatePrice()`, `StartBrew()` |
| Private methods | PascalCase | `InitializeComponents()` |
| Event handlers | `On` prefix | `OnLoadData()`, `OnGoldChanged()` |
| Boolean methods | `Is`/`Has`/`Can` prefix | `IsValid()`, `HasItems()`, `CanBrew()` |

### Namespaces

```
PM                          # Root namespace
PM.Base                     # Base classes and interfaces
PM.Core                     # App class
PM.Service                  # Service implementations
PM.Manager                  # Manager implementations
PM.UiController             # UI Controllers
PM.Objects                  # Data objects and DTOs
PM.Objects.Data             # Persistence data containers
PM.Objects.EventData        # Event-specific data objects
PM.Objects.Network          # Network communication objects
PM.Enums                    # Centralized enumerations
PM.Interfaces               # Contract definitions
PM.ScriptableObjects        # Unity ScriptableObject definitions
PM.Tests.EditMode           # Unit tests
PM.Tests.PlayMode           # Integration tests
PM.Tests.Manual             # Manual test scripts
```

---

## 11. Code Organization

### Member Ordering Within a Class

Every class follows this exact order:

```csharp
public class ExampleManager : BaseManager<ExampleManager>
{
    // 1. Constants and static fields
    private const int MAX_COUNT = 100;

    // 2. Serialized fields
    [SerializeField] private float _rate = 1.0f;

    // 3. Private fields
    private int _count;
    private readonly List<Item> _items = new List<Item>();

    // 4. Properties
    public int Count => _count;
    public bool IsReady { get; private set; }

    // 5. Unity lifecycle methods (Awake, Start, OnInit, Update, etc.)
    private void Awake() { }
    protected override void OnInit() { }

    // 6. Public interface methods
    public void DoSomething() { }

    // 7. Private helper methods
    private void HelperMethod() { }

    // 8. Event handlers
    private void OnLoadData(SomeData data) { }

    // 9. Editor-only methods
    #if UNITY_EDITOR
    [ContextMenu("Debug")]
    private void DebugInfo() { }
    #endif
}
```

### General Code Style

- **Always** use explicit access modifiers (`public`, `private`, `protected`).
- **Self-documenting code** - write clear names instead of comments. Only add comments when the logic is genuinely non-obvious.
- **Never** create `.meta` files manually. Unity generates them.
- **Use `App.Log`** for all logging. Never use `Debug.Log` directly.
- **Use `readonly`** for collections that do not change their reference.
- **Use `IReadOnlyList<T>`** for public collection properties.
- **Use expression-bodied members** for simple getters: `public int Count => _count;`

---

## 12. Error Handling

### Standard Pattern

```csharp
public bool TryPerformAction()
{
    try
    {
        // Implementation
        return true;
    }
    catch (Exception e)
    {
        App.Log.Warning($"Failed to perform action: {e.Message}");
        return false;
    }
}
```

### Data Loading Pattern

```csharp
private void OnLoadData(SomeData data)
{
    try
    {
        if (data == null)
        {
            App.Log.Warning("Loaded data is null, initializing defaults.");
            _data = new SomeData();
            return;
        }

        _data = data;
        ValidateDataIntegrity();
    }
    catch (Exception e)
    {
        App.Log.Error($"Failed to load data: {e.Message}");
        _data = new SomeData();
    }
}
```

### UI Element Validation

```csharp
private void InitializeUI()
{
    _button = RootVisualElement.Q<Button>("ActionButton");
    if (_button == null)
    {
        App.Log.Error("ActionButton not found in UI document");
        return;
    }

    _button.RegisterCallback<ClickEvent>(OnActionClicked);
}
```

### Rules

- **Always** handle null data gracefully with default initialization.
- **Always** validate queried UI elements before using them.
- **Use `App.Log.Warning`** for recoverable issues, `App.Log.Error` for critical problems.
- **Never** let exceptions propagate silently. Catch, log, and provide fallback behavior.

---

## 13. Performance and Best Practices

### Event-Driven Over Polling

Prefer event-driven architecture over `Update()` polling:

```csharp
// BAD: Checking every frame
private void Update()
{
    if (_gold != _previousGold)
    {
        UpdateGoldDisplay();
        _previousGold = _gold;
    }
}

// GOOD: React to events
private void OnGoldChanged(double gold)
{
    UpdateGoldDisplay(gold);
}
```

### Memory Management

- Use `readonly` for collection fields that do not change reference.
- Prefer `struct` for small, immutable value types.
- Use object pooling for frequently created/destroyed objects.
- WeakReference is used by EventService for data caching - this prevents memory leaks from cached event data.
- Event subscriptions are automatically managed by base classes in `OnEnable`/`OnDisable`.

### Component Caching

```csharp
// BAD: GetComponent every frame
private void Update()
{
    GetComponent<Rigidbody>().AddForce(Vector3.up);
}

// GOOD: Cache in Awake
private Rigidbody _rigidbody;

private void Awake()
{
    _rigidbody = GetComponent<Rigidbody>();
}

private void FixedUpdate()
{
    _rigidbody.AddForce(Vector3.up);
}
```

### UI Toolkit Performance

- Cache all `Q<T>()` results in fields during initialization.
- Use `ListView` for lists with virtualization instead of manually creating elements.
- Use USS files instead of inline styles to reduce per-element memory overhead.
- Hide elements with `DisplayStyle.None` instead of removing them from hierarchy.
- Avoid `:hover` pseudo-selectors on many elements as they cause frequent re-styling.

### General

- Use `TryGetComponent<T>()` to avoid null reference exceptions when the component may not exist.
- Minimize `Update()` usage. Most game state changes should be event-driven.
- Use `Invoke()` or coroutines for delayed operations instead of timers in `Update()`.

---

## 14. Testing

### Test Folder Structure

```
Scripts/Tests/
├── EditMode/       # Unit tests (no Play Mode required)
├── PlayMode/       # Integration tests (require Play Mode)
└── Manual/         # Manual test scripts with ContextMenu
```

### Test Naming Conventions

| Type | Class Name | Method Name |
|------|-----------|-------------|
| Unit/Integration | `ExampleManagerTests` (plural) | `MethodName_Scenario_ExpectedResult` |
| Manual | `ExampleManagerTest` (singular) | `TestMethodName` |

### EditMode Tests (Unit Tests)

Fast tests that run without Play Mode. Suitable for testing pure logic.

```csharp
using NUnit.Framework;
using PM.Objects.Data;

namespace PM.Tests.EditMode
{
    public class QuestDataTests
    {
        [Test]
        public void Constructor_WithParameters_SetsProperties()
        {
            var quest = new QuestData("quest_001", QuestType.Daily);

            Assert.That(quest.QuestId, Is.EqualTo("quest_001"));
            Assert.That(quest.Type, Is.EqualTo(QuestType.Daily));
        }

        [Test]
        public void DefaultConstructor_InitializesDefaults()
        {
            var quest = new QuestData();

            Assert.That(quest.QuestId, Is.Null);
            Assert.That(quest.IsActive, Is.False);
        }
    }
}
```

### PlayMode Tests (Integration Tests)

Tests that require Unity's runtime. Use `[UnityTest]` for coroutine-based tests.

```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using PM.Manager;

namespace PM.Tests.PlayMode
{
    public class BrewManagerIntegrationTests
    {
        private GameObject _testObject;
        private BrewManager _brewManager;

        [SetUp]
        public void Setup()
        {
            _testObject = new GameObject("TestBrewManager");
            _brewManager = _testObject.AddComponent<BrewManager>();
        }

        [UnityTest]
        public IEnumerator StartBrewing_ValidIngredients_CompletesSuccessfully()
        {
            _brewManager.StartBrewing(GetTestIngredients());
            yield return new WaitForSeconds(2f);

            Assert.That(_brewManager.IsBrewingComplete, Is.True);
        }

        [TearDown]
        public void Teardown()
        {
            if (_testObject != null)
                Object.DestroyImmediate(_testObject);
        }
    }
}
```

### Manual Tests

MonoBehaviour-based tests with `[ContextMenu]` for in-editor testing.

```csharp
using UnityEngine;
using PM.Manager;

namespace PM.Tests.Manual
{
    public class CustomerManagerTest : MonoBehaviour
    {
        [Header("Test Configuration")]
        public int NumberOfCustomers = 10;
        public CustomerType TestCustomerType = CustomerType.Regular;

        [Header("Test Results")]
        public List<CustomerTestResult> Results = new List<CustomerTestResult>();

        [System.Serializable]
        public class CustomerTestResult
        {
            public string CustomerName;
            public bool TestPassed;
        }

        [ContextMenu("Test Customer Generation")]
        public void TestCustomerGeneration()
        {
            Results.Clear();

            for (int i = 0; i < NumberOfCustomers; i++)
            {
                var customer = CustomerManager.Instance.GenerateCustomer(TestCustomerType);
                Results.Add(new CustomerTestResult
                {
                    CustomerName = customer.Name,
                    TestPassed = !string.IsNullOrEmpty(customer.Name)
                });
            }

            var passed = Results.Count(r => r.TestPassed);
            App.Log.Debug($"Test Results: {passed}/{Results.Count} passed");
        }
    }
}
```

### Assertion Style

Use NUnit constraint-based assertions with descriptive messages:

```csharp
Assert.That(result, Is.EqualTo(expected), "Description of what should be true");
Assert.That(collection, Is.Not.Empty, "Collection should contain items");
Assert.That(value, Is.InRange(0f, 100f), "Value should be in valid range");
```

---

## 15. Folder Structure

```
Assets/
└── _project/
	├── Resources/
	│   └── Ui/
	│       └── Templates/          # Reusable UXML templates for ListView items etc.
	├── Scripts/
	│   ├── Base/                   # BaseManager<T>, BaseUiController<T>, interfaces
	│   ├── Core/                   # App class
	│   ├── Enums/                  # EventKeys, SaveKeys, game enums
	│   ├── Interfaces/             # IManager, IUiController
	│   ├── Manager/                # All Manager implementations
	│   ├── Objects/
	│   │   ├── Data/               # Persistence data (XCollectionData, XData)
	│   │   ├── EventData/          # Event-specific DTOs
	│   │   └── Network/            # Network request/response objects
	│   ├── ScriptableObjects/      # ScriptableObject definitions
	│   ├── Service/                # Service implementations
	│   ├── Tests/
	│   │   ├── EditMode/           # Unit tests
	│   │   ├── PlayMode/           # Integration tests
	│   │   └── Manual/             # Manual ContextMenu tests
	│   └── UiController/           # All UI Controller implementations
	├── StreamingAssets/
	│   └── Config/
	│       └── Config.ini          # Application configuration
	└── Ui/
		├── Documents/              # .uxml files
		├── Styles/                 # .uss files
		├── Settings/               # UI Toolkit settings
		└── Themes/                 # UI Toolkit themes
```

---

## Quick Reference: When to Use What

| I need to... | Use |
|-------------|-----|
| Add core infrastructure (logging, config, analytics) | **Service** |
| Add game logic or manage game data | **Manager** |
| Display data or handle user input in UI | **Controller** |
| Communicate between layers | **EventService** |
| Persist data to disk | **DataManager** + Change/Load events |
| Create a list UI | **ListView** with UXML template |
| Style UI elements | **USS file** with BEM classes |
| Run code every frame | `Update()` (but prefer events) |
| Handle physics | `FixedUpdate()` |
| Initialize self-references | `Awake()` |
| Initialize cross-references | `Start()` or `OnInit()` |
