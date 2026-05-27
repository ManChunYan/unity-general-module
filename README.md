# General Module

Unity runtime module utilities with lifecycle management and binary save-data packing helpers.

## Installation

Add the package from a Git URL in Unity Package Manager:

```text
https://github.com/<owner>/<repo>.git?path=Packages/com.general.module
```

Or copy `Packages/com.general.module` into your project's `Packages` folder.

## Samples

Import `Basic Module` from Unity Package Manager's Samples section. It contains a minimal `MainBase` scene component and an `IDataPack` module that saves main data and sub-data.

## Namespaces

```csharp
using General;
using General.Module;
```

## Quick Start

Create a main behaviour by inheriting `MainBase`, then initialize modules from `Awake` or `Start`.

```csharp
using General;
using UnityEngine;

public class GameMain : MainBase
{
    protected override void Awake()
    {
        base.Awake();
        InitModule();
    }
}
```

Create a module by inheriting `ModuleBase`.

```csharp
using General.Module;
using UnityEngine;

[Module(ModuleOpCode.None)]
public class ExampleModule : ModuleBase
{
    protected override void OnInit()
    {
        Debug.Log("Example module initialized.");
    }

    protected override void OnRun()
    {
        // Called from MainBase.Update.
    }
}
```

Modules with `ModuleAttribute` are discovered automatically during `InitModule`. You can also add modules manually:

```csharp
var module = AddModule<ExampleModule>();
```

## ModuleOpCode

`ModuleOpCode.None` is provided by default. Add project-specific values when this package is maintained in project source, or fork the package for shared opcode definitions.

```csharp
namespace General.Module
{
    public enum ModuleOpCode : ushort
    {
        None = 0,
        // 1-9999: ModuleBase
        // 10000+: DataModule
    }
}
```

## Save Data

Modules can implement `IDataPack` to participate in `MainBase.Save` and `MainBase.Load`.

```csharp
using System.Collections.Generic;
using General.Module;

public class PlayerDataModule : ModuleBase, IDataPack
{
    private int _level = 1;

    public int SubDataCount() => 0;
    public IEnumerable<uint> GetSubDataIDList() => System.Array.Empty<uint>();

    public void SaveMainData(DataPack pack, string fileName)
    {
        pack.Write(_level);
    }

    public void SetMainDataDirty(DataPack pack, string fileName)
    {
        _level = pack.ReadInt32();
    }

    public void SaveSubData(uint id, DataPack pack, string fileName) { }
    public void SetSubDataDirty(uint id, DataPack pack, string fileName) { }
}
```

Save and load return `SaveLoadResult`:

```csharp
await Save(MainBase.SavePath, "slot1");
var result = Load(MainBase.SavePath, "slot1");
```

| Code | Meaning |
| --- | --- |
| `SaveLoadResult.Success` | Success or empty save data |
| `SaveLoadResult.FileNotFound` | Save file not found |
| `SaveLoadResult.InvalidData` | Invalid/corrupt save data |
| `SaveLoadResult.UnsupportedFormatVersion` | Save file format is newer than this package supports |

## Editor Tool

The Unity editor menu `Tools/SaveData/Clear` deletes `.sav` files from `Application.persistentDataPath` after a confirmation dialog.

## License

This package is released under the MIT License. See `LICENSE`.
