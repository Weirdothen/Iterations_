# Unity C# Style Guide

**Keywords used in this guide**

* **MUST / MUST NOT**: no exceptions. Code reviews block on these.
* **SHOULD**: the default. Deviate only with a good reason.
* **MAY**: optional, use your judgment.

---

## 1. Project Structure and Files

### Folders

Organize by feature. Everything that belongs to one feature (scripts, prefabs, models, audio, UI) lives in that feature's folder. Anything used by several features lives in `Core`.

```
Assets/
├── _Project/
│   ├── Core/                     <-- Shared systems used by everyone
│   │   ├── Audio/                <-- Global UI clicks, ambient tracks
│   │   ├── Scripts/              <-- Base classes, Extensions, Managers
│   │   │   └── Editor/           <-- General editor tools
│   │   └── Shaders/              <-- Shared global shaders
│   ├── Player/
│   │   ├── Models/
│   │   ├── Prefabs/              <-- Player prefab goes here
│   │   ├── Scripts/              <-- PlayerController, PlayerHealth
│   │   └── PlayerSFX.wav
│   ├── Combat/
│   │   ├── Animations/
│   │   ├── Prefabs/              <-- Projectiles, FX prefabs
│   │   └── Scripts/              <-- Weapon, DamageDealer
│   │       └── Editor/           <-- Editor scripts used only by Combat
│   ├── Inventory/
│   │   ├── UI/                   <-- Inventory canvas and item slots
│   │   ├── Scripts/              <-- InventoryManager, ItemData (ScriptableObject)
│   │   └── Inventory_Click.wav
│   └── Scenes/                   <-- Main Menu, Level1, Level2
└── ThirdParty/                   <-- Kept clean and isolated
```

* **(MUST)** Assets used by one feature stay in that feature's folder. Assets shared by several features go in `Core`.
* **(MUST)** Third-party packages and plugins live only in `Assets/ThirdParty/`. Do not edit them.
* **(SHOULD)** Editor scripts that belong to one feature go in `<Feature>/Scripts/Editor/`. General editor tools go in `Core/Scripts/Editor/`.

### Namespaces

* **(MUST)** Every script is wrapped in `CompanyName.ProjectName.Feature`, where `Feature` is the name of the feature folder directly under `_Project`.
  * `_Project/Combat/Scripts/Weapon.cs` uses `namespace KajoGames.MyGame.Combat`.
  * `_Project/Core/Scripts/Extensions/VectorExtensions.cs` uses `namespace KajoGames.MyGame.Core`.
* `_Project`, `Scripts` and any subfolders do **not** add namespace segments.
* Editor scripts use the same namespace as their feature. Do **not** add a `.Editor` segment, because it clashes with `UnityEditor.Editor`.
* **(MUST NOT)** A namespace has the same name as a type inside it. A `Player` namespace collides with a `Player` class. If a feature folder would cause this, name the folder differently (for example `PlayerSystems`).

### Files

* **(MUST)** The file name matches the primary type exactly (`PlayerHealth.cs`). For a generic class, use the name without type parameters (`Pool.cs` for `Pool<T>`).
* **(MUST)** One main type per file.
  * A small enum, struct or EventArgs used by only one class SHOULD be nested inside that class.
  * Types shared between classes get their own file.
  * Interfaces always get their own file.
* **(MAY)** We do not use assembly definitions for now.

---

## 2. Naming Conventions

This table covers names only. Behavior rules are in the sections that follow.

| Category | Convention | Example |
| --- | --- | --- |
| Namespaces | PascalCase | `KajoGames.MyGame.Combat` |
| Classes, structs, enums | PascalCase | `PlayerController`, `WeaponType` |
| Interfaces | `I` + PascalCase | `IDamageable` |
| ScriptableObject classes | PascalCase + `SO` suffix | `VoidEventChannelSO`, `BoolEventChannelSO` |
| Event channel fields | `_camelCase` + event name in event tense + `Channel` suffix | `_playerDiedChannel`, `_levelLoadingChannel` |
| Public methods and properties | PascalCase | `Initialize()`, `IsAlive` |
| Private and protected methods | PascalCase, starting with a verb | `CalculateDamage()`, `ApplyKnockback()` |
| Private and protected fields | `_camelCase` | `_currentHealth` |
| Serialized fields | `[SerializeField] private` + `_camelCase` | `[SerializeField] private float _jumpForce;` |
| Constants and `static readonly` values | SCREAMING_SNAKE_CASE | `MAX_HEALTH`, `DAMAGE_FLASH_COLOR` |
| Parameters and local variables | camelCase | `damageAmount`, `targetPosition` |
| Events | PascalCase, **no** `On` prefix. Present participle (`-ing`) if raised *before* the change, past tense (`-ed`) if raised *after* | `HealthChanging`, `HealthChanged`, `Died` |
| Event handler methods | `On` + event name | `OnHealthChanging`, `OnHealthChanged` |

**Booleans (MUST):** name them as a question or predicate.

| Kind | Form | Example |
| --- | --- | --- |
| Local or parameter | `camelCase` | `isAlive`, `hasKey` |
| Private field | `_camelCase` | `_isAlive` |
| Property | `PascalCase` | `IsAlive`, `HasKey` |
| Method returning bool | Starts with a check verb | `HasValidTarget()`, `IsInRange()` |

**Try pattern (MAY):** Methods that follow the .NET `Try` pattern are the only exception to the boolean-method rule: `bool TryGetTarget(out Enemy target)`.

**Why the event and handler names differ:** A class can publish `HealthChanged` and also contain `OnHealthChanged` without a name collision. The `On` prefix also tells readers "this is a handler".

---

## 3. Class Member Ordering

Members MUST appear in this order:

1. Class-scoped enums and structs
2. Constants and `static readonly` values
3. Static fields
4. Serialized fields (`[SerializeField]`)
5. Private and protected fields (protected before private)
6. Public properties and events
7. Constructors (only in non-MonoBehaviour classes)
8. Unity lifecycle methods, in this order:
   `Awake`, `OnEnable`, `Start`, `FixedUpdate`, `Update`, `LateUpdate`, `OnDisable`, `OnDestroy`
9. Other Unity messages: physics callbacks (`OnTriggerEnter`, `OnCollisionEnter`, ...), then `OnGUI`
10. Public methods (interface implementations belong here)
11. Protected methods
12. Private methods (event handlers and coroutines are private methods)
13. **Editor Callbacks region**, in this order: `Reset`, `OnValidate`, `OnDrawGizmos`, `OnDrawGizmosSelected`
14. Nested classes (for example serializable group classes)

Notes:

* The Editor Callbacks region is the only place `#region` is allowed in finished code (see Section 4).
* Wrap the region's contents in `#if UNITY_EDITOR` so they are stripped from builds.
* Public fields are **not allowed** (see Section 5).

---

## 4. Formatting

* **Brace style (MUST):** Allman. Every opening brace goes on its own line.
* **Braces are always required (MUST):** One-line `if`, `else`, `for`, `foreach` and `while` without braces are not allowed.

```csharp
// Bad
if (damageAmount <= 0) return;

// Good
if (damageAmount <= 0)
{
    return;
}
```

* **Indentation (MUST):** 4 spaces. No tabs.
* **Spacing (MUST):**
  * One space after control-flow keywords: `if (`, `while (`, `for (`, `foreach (`.
  * One space around binary operators: `a + b`, `x == y`.
  * One space after commas: `Foo(a, b)`.
  * No space before the parentheses of a method call: `Foo(a)`.
  * No space inside parentheses: `(a + b)`, not `( a + b )`.
  * No space after a cast: `(float)value`.
  * No spaces inside generics: `List<int>`, `Dictionary<string, int>`.
  * No space around unary operators or `++`/`--`: `-x`, `!isAlive`, `i++`.

### Blank lines

A **member** is a field, property, method or nested type.

* **(MUST)** Put exactly **one** blank line between members that have a body: methods, multi-line properties, nested types.
* **(MAY)** Simple fields, or one-line properties of the same kind, can be stacked with no blank lines between them. Use a blank line to separate one group from the next.
* **(MUST NOT)** Use two or more blank lines in a row.
* **(MUST NOT)** Leave a blank line right after an opening `{` or right before a closing `}`.

```csharp
// Good
private int _currentHealth;
private bool _isInitialized;

public bool IsAlive => _currentHealth > 0;
public float HealthPercentage => (float)_currentHealth / _maxHealth;

public void Heal(int healAmount)
{
    _currentHealth += healAmount;
}

public void Hurt(int damageAmount)
{
    _currentHealth -= damageAmount;
}

// Bad: no blank line between methods, and two blank lines elsewhere
public void Heal(int healAmount)
{
    _currentHealth += healAmount;
}
public void Hurt(int damageAmount)
{

    _currentHealth -= damageAmount;
}


private void Die()
{
}
```

### Other formatting rules

* **Line length (SHOULD):** 120 characters at most.
* **File encoding (MUST):** UTF-8, LF line endings, one newline at the end of the file.
* **`#region` (MUST NOT):** Only one region is allowed in finished code: `Editor Callbacks` (Section 3). Anywhere else, `#region` is tolerated only in unrefactored or unfinished scripts and MUST be removed once the script is cleaned up.
* **Preprocessor directives:** `#if` / `#endif` start at column 0, and the code inside is indented normally. `#region` / `#endregion` are indented with the code around them.

---

## 5. Fields, Properties and the Inspector

### Fields and properties

* **No public fields (MUST NOT):** Use a property, or a `[SerializeField] private` field when the Inspector needs it. To read a serialized field from outside, add a property:

```csharp
[SerializeField] private float _jumpForce;

public float JumpForce => _jumpForce;
```

* **Expression-bodied properties (SHOULD):** For single-line read-only properties.
* **Standard properties:** Use `{ get; set; }` or `{ get; private set; }` when you need a setter or custom logic.
* **Field initializers (SHOULD):** Encouraged, but do not write redundant default values (`= 0`, `= null`, `= false`).

### Inspector attributes

* **Placement (MUST):**
  * `[SerializeField]` stays on the same line as the field.
  * `[Header]`, `[Space]`, `[Tooltip]` and `[Range]` each go on their **own line above** the field, in that order.

```csharp
[Header("Movement")]
[Tooltip("Upward force applied when the player jumps.")]
[Range(1f, 20f)]
[SerializeField] private float _jumpForce = 8f;
```

* **Tooltips (SHOULD):** If a serialized field needs explaining, use `[Tooltip("...")]` instead of a code comment. Self-explanatory fields (`_rigidbody`, `_speed`) do not need one.
* **Grouping:**
  * Use `[Header]` to label sections of fields.
  * Use a **serializable group class** when 3 or more related fields belong together, or when the same group repeats in several components.
  * Group types are `[Serializable] class` (not struct) with private serialized fields and public read-only properties. They are nested classes, so they go at the end of the file (Section 3).

```csharp
[Serializable]
public class CombatConfig
{
    [SerializeField] private float _attackRange = 2f;
    [SerializeField] private float _attackCooldown = 0.5f;

    public float AttackRange => _attackRange;
    public float AttackCooldown => _attackCooldown;
}
```

**Why a class and not a struct:** Structs cannot have field initializers in Unity's C# version, so you could not give fields default values like `= 2f`. Structs are also copied every time they are read or passed.

---

## 6. Structs vs. Classes

* **Default to classes (SHOULD):** Gameplay components, managers and data containers are classes.
* **Use a struct only** when the type behaves like a primitive value: small, short-lived, or embedded in other objects (lightweight math types, small data bags).
* **Exception:** Inspector grouping types are classes (see Section 5), not structs.
* **Structs are copied when passed or returned.** Changing a copy does not change the original:

```csharp
// Does not compile (error CS1612): position returns a copy, so you cannot change x directly.
transform.position.x = 10f;

// Compiles but does nothing: you changed the local copy and never assigned it back.
Vector3 position = transform.position;
position.x = 10f;

// Correct: change the copy, then assign it back.
Vector3 position = transform.position;
position.x = 10f;
transform.position = position;
```

---

## 7. Methods, Lambdas, `ref`/`out` and Collections

* **Lambdas (SHOULD):** Keep a lambda to one expression or at most 2 statements. Anything longer, or anything used in more than one place, becomes a named private method.
* **`out` parameters (MUST):** Use `out` only for extra return values that are not also inputs, and place them **after** all other parameters.
* **`ref` parameters (SHOULD NOT):** Use `ref` rarely, only when changing an existing input is truly required. Do not use `ref` as a performance trick for large structs.
* **Arrays vs. lists:**
  * Prefer `List<T>` for public members, return types, and anything whose size can change.
  * Use `T[]` only when the size is fixed at creation, or for multi-dimensional grids where performance requires it.

---

## 8. Events

* **Naming:**
  * An event raised **before** the change uses the present participle: `HealthChanging`.
  * An event raised **after** the change uses the past tense: `HealthChanged`.
  * Handlers add `On` in front: `OnHealthChanging`, `OnHealthChanged` (see Section 2).
* **Subscribe in `OnEnable`, unsubscribe in `OnDisable` (MUST).** This prevents leaks and calls on disabled objects.
* **Use named methods for subscriptions (MUST):** A lambda cannot be unsubscribed later, so it leaks.

```csharp
private void OnEnable()
{
    _health.HealthChanged += OnHealthChanged;
}

private void OnDisable()
{
    _health.HealthChanged -= OnHealthChanged;
}

private void OnHealthChanged(int newHealth)
{
    _healthBar.SetValue(newHealth);
}
```

### Event Channels (ScriptableObject)

An event channel is a ScriptableObject asset that carries an event between objects that do not reference each other (`VoidEventChannelSO`, `BoolEventChannelSO`).

* **Class names (MUST):** End with the `SO` suffix: `VoidEventChannelSO`.
* **Field names (MUST):** A normal serialized field (`_camelCase`) named after the event in event tense, with a `Channel` suffix. Use `-ed` if the channel is raised after the change, and `-ing` if it is raised before it.
* **No `On` prefix on the field (MUST NOT):** `On` is reserved for handler methods, so `_onPlayerDied` is wrong.
* **Inside the channel class (MUST):** The event is named `EventRaised` (no `On` prefix) and the raising method is `RaiseEvent()`.
* **Subscribing:** Follow the same rules as above: subscribe in `OnEnable`, unsubscribe in `OnDisable`, and use a named handler.

```csharp
// The channel
public class VoidEventChannelSO : ScriptableObject
{
    public event Action EventRaised;

    public void RaiseEvent()
    {
        EventRaised?.Invoke();
    }
}

// A listener
[SerializeField] private VoidEventChannelSO _playerDiedChannel;

private void OnEnable()
{
    _playerDiedChannel.EventRaised += OnPlayerDied;
}

private void OnDisable()
{
    _playerDiedChannel.EventRaised -= OnPlayerDied;
}

private void OnPlayerDied()
{
    ShowGameOverScreen();
}
```

---

## 9. Comments and Documentation

* **Self-documenting code (SHOULD):** Good names beat comments. Use inline comments only to explain *why* something non-obvious was done, not *what* the code does.
* **No commented-out code (MUST NOT):** Delete it. Git keeps the history.
* **Language (MUST):** Comments are written in English.

### XML documentation (`///`)

**(MAY)** Use it on:

* interfaces,
* systems that other features call,
* methods whose units, side effects or failure behavior are not clear from the name.

Skip it on self-explanatory code, simple getters and Unity lifecycle methods. Keep it short: a `<summary>`, and `<param>` / `<returns>` only when they add information.

```csharp
// Good: explains units, limits and the return value, which the name alone does not.
/// <summary>
/// Applies damage to the target after armor is taken into account.
/// </summary>
/// <param name="damageAmount">Raw damage in health points. Values of 0 or less are ignored.</param>
/// <param name="damageType">Decides which resistance applies.</param>
/// <returns>The damage actually dealt after armor, in health points.</returns>
int TakeDamage(int damageAmount, DamageType damageType);

// Bad: only repeats the name.
/// <summary>
/// Gets the health.
/// </summary>
public int Health => _health;
```

### Comment tags

**(MUST)** Include the author. Format: `// TAG(AuthorName): message`.

| Tag | Use for | Example |
| --- | --- | --- |
| `TODO` | Work deferred to later | `// TODO(Sara): Implement object pooling for projectiles.` |
| `HACK` | An inelegant workaround for a blocker or engine quirk | `// HACK(Omar): Force layout rebuild to bypass Unity's one-frame delay.` |
| `FIXME` | A known bug or broken logic | `// FIXME(Sara): Negative health can underflow the armor calculation.` |
| `NOTE` | A non-obvious decision or constraint | `// NOTE(Omar): Must survive scene loads, hence DontDestroyOnLoad.` |

---

## 10. Unity Pitfalls

* **Unity null checks (MUST):** For anything that inherits from `UnityEngine.Object` (components, GameObjects, ScriptableObjects), use `== null`, `!= null`, or `if (obj)`. Do **not** use `?.`, `??` or `is null`, because they skip Unity's "destroyed object" check and can touch objects that were already destroyed. Plain C# events are not Unity objects, so `HealthChanged?.Invoke(...)` is fine.

```csharp
// Bad
_target?.TakeDamage(10);

// Good
if (_target != null)
{
    _target.TakeDamage(10);
}
```

* **Renaming serialized fields (MUST):** Unity stores values by field name. Renaming a serialized field resets its saved value everywhere, so add `[FormerlySerializedAs]` with the old name in the same commit:

```csharp
[FormerlySerializedAs("jumpForce")]
[SerializeField] private float _jumpForce;
```

* **Initializers on serialized fields (NOTE):** The value in `= 5f` only applies to **newly added** components. Existing prefabs and scene objects keep their saved value. To change values on existing objects, change them in the Inspector.

---

## 11. Reference Example

One complete script that follows every rule above. When a rule is unclear, copy this layout.

```csharp
using System;
using UnityEngine;

namespace KajoGames.MyGame.Combat
{
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        public enum HealthState
        {
            Healthy,
            Critical
        }

        private const int DEFAULT_MAX_HEALTH = 100;
        private const float CRITICAL_THRESHOLD = 0.25f;
        private static readonly Color DAMAGE_FLASH_COLOR = Color.red;

        private static int _totalDeaths;

        [Header("Health")]
        [Tooltip("Maximum health before any bonuses are applied.")]
        [SerializeField] private int _maxHealth = DEFAULT_MAX_HEALTH;
        [SerializeField] private DamageSettings _damageSettings;

        [Header("References")]
        [SerializeField] private PlayerShield _shield;

        private int _currentHealth;

        public bool IsAlive => _currentHealth > 0;
        public float HealthPercentage => (float)_currentHealth / _maxHealth;
        public bool IsCritical => HealthPercentage <= CRITICAL_THRESHOLD;
        public HealthState State => IsCritical ? HealthState.Critical : HealthState.Healthy;

        public event Action<int> HealthChanging;
        public event Action<int> HealthChanged;
        public event Action Died;

        private void Awake()
        {
            _currentHealth = _maxHealth;
        }

        private void OnEnable()
        {
            _shield.ShieldBroken += OnShieldBroken;
        }

        private void OnDisable()
        {
            _shield.ShieldBroken -= OnShieldBroken;
        }

        public void TakeDamage(int damageAmount)
        {
            if (!IsAlive || damageAmount <= 0)
            {
                return;
            }

            HealthChanging?.Invoke(damageAmount);

            _currentHealth = Mathf.Max(0, _currentHealth - damageAmount);
            HealthChanged?.Invoke(_currentHealth);

            if (!IsAlive)
            {
                Die();
            }
        }

        /// <summary>
        /// Heals the player up to the maximum health. Does nothing if the player is dead.
        /// </summary>
        /// <param name="healAmount">Health points to restore. Must be greater than 0.</param>
        /// <param name="newHealth">The health after healing. Unchanged if no healing was applied.</param>
        /// <returns><c>true</c> if any healing was applied; otherwise <c>false</c>.</returns>
        public bool TryHeal(int healAmount, out int newHealth)
        {
            newHealth = _currentHealth;

            if (!IsAlive || healAmount <= 0)
            {
                return false;
            }

            HealthChanging?.Invoke(-healAmount);

            _currentHealth = Mathf.Min(_maxHealth, _currentHealth + healAmount);
            newHealth = _currentHealth;
            HealthChanged?.Invoke(_currentHealth);

            return true;
        }

        private void Die()
        {
            _totalDeaths++;
            Died?.Invoke();
        }

        private void OnShieldBroken()
        {
            TakeDamage(1);
        }

        #region Editor Callbacks
#if UNITY_EDITOR
        private void OnValidate()
        {
            _maxHealth = Mathf.Max(1, _maxHealth);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = DAMAGE_FLASH_COLOR;
            Gizmos.DrawWireSphere(transform.position, 1f);
        }
#endif
        #endregion

        [Serializable]
        public class DamageSettings
        {
            [SerializeField] private float _invulnerabilitySeconds = 0.5f;
            [SerializeField] private float _armorMultiplier = 1f;
            [SerializeField] private float _knockbackForce = 5f;

            public float InvulnerabilitySeconds => _invulnerabilitySeconds;
            public float ArmorMultiplier => _armorMultiplier;
            public float KnockbackForce => _knockbackForce;
        }
    }
}
```