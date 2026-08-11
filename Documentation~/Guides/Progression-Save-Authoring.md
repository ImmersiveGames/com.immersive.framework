# Progression Save — Authoring

## Built-in JSON

1. Open the active **Game Application** asset.
2. In **Progression Save**, enable **Enabled**.
3. Assign a **Default Progression Save Profile**, or click **Create Progression Save Profile**.
4. Open the Profile.
5. Set **Backend > Selection** to **Built-in JSON**.
6. Return to the Game Application and click **Validate**.

Expected status:

```text
Ready — Built-in JSON
```

The storage path is derived by the built-in backend. Normal authoring does not require
editing file paths.

## Custom / third-party backend

A package or game integration derives from:

```csharp
ProgressionSaveStoreProviderAsset
```

The provider contains vendor-specific authored configuration and implements
`TryCreateStore(...)`.

In the Profile:

```text
Backend
  Selection = Custom Provider
  Provider = <your provider asset>
```

If the selected provider cannot create a valid `IProgressionSaveStore`, composition
fails explicitly. The Framework does not switch to JSON.

## Runtime ownership

The default Framework bootstrap resolves the Profile once and gives the resulting
`ProgressionSaveRuntime` application lifetime through `FrameworkRuntimeHost`.

There is no global `ProgressionSaveManager`, singleton or service locator.

The final gameplay injection/binding convenience surface is intentionally deferred to
FIRSTGAME usability proof.
