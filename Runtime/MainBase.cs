using General.Module;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace General.Module
{
    public enum SaveLoadResult
    {
        Success = 0,
        FileNotFound = 1,
        InvalidData = 2,
        UnsupportedFormatVersion = 3
    }
}

namespace General
{
    public class MainBase : MonoBehaviour
    {
        protected ModuleCore _moduleCore;

        public static string SavePath => Application.persistentDataPath + Path.DirectorySeparatorChar;

        public static string FileFormat => ".sav";

        private const int SaveFileMagic = 0x444F4D47; // "GMOD" in little-endian.
        private const int SaveFileFormatVersion = 1;

        protected virtual void Awake()
        {
            EnsureModuleCore();
        }

        protected void InitModule()
        {
            EnsureModuleCore().Init();
            EnsureModuleCore().Began();
        }

        protected void Update()
        {
            Run();
        }

        protected void Run()
        {
            EnsureModuleCore().Run();
        }

        public T AddModule<T>(ModuleOpCode opcode) where T : ModuleBase
        {
            return EnsureModuleCore().AddModule<T>(opcode);
        }

        public T AddModule<T>() where T : ModuleBase
        {
            return EnsureModuleCore().AddModule<T>();
        }

        public T GetModule<T>() where T : ModuleBase
        {
            foreach (var module in EnsureModuleCore().GetModules())
            {
                if (module is T typedModule)
                {
                    return typedModule;
                }
            }
            return null;
        }

        public ModuleBase GetModule(ModuleOpCode opcode)
        {
            foreach (var module in EnsureModuleCore().GetModules())
            {
                if (module.Opcode == opcode)
                    return module;
            }
            return null;
        }

        public bool RemoveModule<T>(T module) where T : ModuleBase
        {
            return EnsureModuleCore().RemoveModule(module);
        }

        protected void Release()
        {
            EnsureModuleCore().Release();
        }

        public void DestroyObj(GameObject obj)
        {
            obj.SetActive(false);
            Destroy(obj);
        }

        public async Task<SaveLoadResult> Save(string path, string fileName)
        {
            var dataPack = new DataPack();
            var modules = new List<ModuleSaveData>();

            var currentPath = Path.Combine(path, fileName + FileFormat);

            dataPack.Write(SaveFileMagic);
            dataPack.Write(SaveFileFormatVersion);

            foreach (var module in EnsureModuleCore().GetModules())
            {
                if (module is IDataPack dataModule)
                {
                    var modulePack = new DataPack();
                    dataModule.SaveMainData(modulePack, fileName);
                    modules.Add(new ModuleSaveData(module.Opcode, modulePack.GetBuffer));
                }
            }

            dataPack.Write(modules.Count);

            foreach (var module in modules)
            {
                dataPack.Write((int)module.Opcode);
                dataPack.Write(module.Data.Length);
                dataPack.Write(module.Data);

                if (GetModule(module.Opcode) is IDataPack dataModule)
                {
                    var subDataIds = dataModule.GetSubDataIDList();
                    var subData = new List<SubModuleSaveData>();

                    if (subDataIds != null)
                    {
                        foreach (var id in subDataIds)
                        {
                            var subDataPack = new DataPack();
                            dataModule.SaveSubData(id, subDataPack, fileName);
                            subData.Add(new SubModuleSaveData(id, subDataPack.GetBuffer));
                        }
                    }

                    dataPack.Write(subData.Count);

                    foreach (var data in subData)
                    {
                        dataPack.Write(data.ID);
                        dataPack.Write(data.Data.Length);
                        dataPack.Write(data.Data);
                    }
                }
                else
                {
                    dataPack.Write(0);
                }
            }

            Directory.CreateDirectory(path);

            if (File.Exists(currentPath))
            {
                File.Delete(currentPath);
            }

            return await SaveData(currentPath, dataPack.GetBuffer);
        }

        private async Task<SaveLoadResult> SaveData(string path, byte[] data)
        {
            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                await fs.WriteAsync(data, 0, data.Length);
                Debug.Log("SaveFile Finished ! Path:" + path);
            }
            return SaveLoadResult.Success;
        }

        public SaveLoadResult Load(string path, string fileName)
        {
            var finalPath = Path.Combine(path, fileName + FileFormat);
            if (File.Exists(finalPath))
            {
                var data = File.ReadAllBytes(finalPath);
                return Load(data, fileName);
            }
            else
            {
                return SaveLoadResult.FileNotFound;
            }
        }

        private SaveLoadResult Load(byte[] data, string fileName)
        {
            try
            {
                var loadPack = new DataPack(data);

                if (loadPack.Count == 0)
                    return SaveLoadResult.Success;

                var magic = loadPack.ReadInt32();
                if (magic != SaveFileMagic)
                {
                    Debug.LogError("Invalid save file format.");
                    return SaveLoadResult.InvalidData;
                }

                var formatVersion = loadPack.ReadInt32();
                if (formatVersion > SaveFileFormatVersion)
                {
                    Debug.LogError($"Unsupported save file format version: {formatVersion}");
                    return SaveLoadResult.UnsupportedFormatVersion;
                }

                var moduleCount = loadPack.ReadInt32();
                if (moduleCount < 0)
                {
                    Debug.LogError($"Invalid save module count: {moduleCount}");
                    return SaveLoadResult.InvalidData;
                }

                for (var i = 0; i < moduleCount; i++)
                {
                    var opcode = (ModuleOpCode)loadPack.ReadInt32();
                    var dataLength = loadPack.ReadInt32();
                    var moduleData = loadPack.ReadBytes(dataLength);

                    var module = GetModule(opcode);

                    if (module is IDataPack packModule)
                    {
                        packModule.SetMainDataDirty(new DataPack(moduleData), fileName);
                    }

                    var subDataCount = loadPack.ReadInt32();
                    if (subDataCount < 0)
                    {
                        Debug.LogError($"Invalid save sub-data count: {subDataCount}");
                        return SaveLoadResult.InvalidData;
                    }

                    for (var subDataIndex = 0; subDataIndex < subDataCount; subDataIndex++)
                    {
                        var id = loadPack.ReadUInt32();
                        var subDataLength = loadPack.ReadInt32();
                        var subData = loadPack.ReadBytes(subDataLength);

                        if (module is IDataPack subDataModule)
                        {
                            subDataModule.SetSubDataDirty(id, new DataPack(subData), fileName);
                        }
                    }
                }

                return SaveLoadResult.Success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Load save data failed: {ex}");
                return SaveLoadResult.InvalidData;
            }
        }

        private readonly struct ModuleSaveData
        {
            public ModuleSaveData(ModuleOpCode opcode, byte[] data)
            {
                Opcode = opcode;
                Data = data ?? Array.Empty<byte>();
            }

            public ModuleOpCode Opcode { get; }
            public byte[] Data { get; }
        }

        private readonly struct SubModuleSaveData
        {
            public SubModuleSaveData(uint id, byte[] data)
            {
                ID = id;
                Data = data ?? Array.Empty<byte>();
            }

            public uint ID { get; }
            public byte[] Data { get; }
        }

        private ModuleCore EnsureModuleCore()
        {
            return _moduleCore ??= new ModuleCore(this);
        }
    }
}
