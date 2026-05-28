using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace General.Module
{

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class ModuleAttribute : Attribute
    {
        public ModuleOpCode Opcode { get; }

        public ModuleAttribute(ModuleOpCode opcode)
        {
            this.Opcode = opcode;
        }
    }

    public class ModuleCore
    {

        private readonly List<ModuleBase> _modules = new List<ModuleBase>();

        private readonly Queue<ModuleBase> _dynamicModules = new Queue<ModuleBase>();

        public int Count => _modules.Count + _dynamicModules.Count;

        private readonly MainBase _main;

        public ModuleCore(MainBase main)
        {
            _main = main;
        }

        internal void AddModule<T>(T t, ModuleOpCode opcode = ModuleOpCode.None) where T : ModuleBase
        {
            if (t == null)
            {
                Debug.LogError($"Add null module: {typeof(T).Name}");
                return;
            }

            if (Contains<T>())
            {
                Debug.LogError($"Add repeated module: {typeof(T).Name}");
                return;
            }

            if (HasSaveDataOpcodeConflict(t.GetType(), opcode))
            {
                Debug.LogError($"Add module with repeated save opcode: {typeof(T).Name}, {opcode}");
                return;
            }

            t.SetOpcode(opcode);
            t.Initialize(_main);
            t.Begin();
            _dynamicModules.Enqueue(t);
        }

        internal T AddModule<T>(ModuleOpCode opcode = ModuleOpCode.None) where T : ModuleBase
        {
            var existingModule = GetModule<T>();
            if (existingModule != null)
            {
                Debug.LogError($"Add repeated module: {typeof(T).Name}");
                return existingModule;
            }

            if (HasSaveDataOpcodeConflict(typeof(T), opcode))
            {
                Debug.LogError($"Add module with repeated save opcode: {typeof(T).Name}, {opcode}");
                return null;
            }

            var module = Activator.CreateInstance<T>();
            module.SetOpcode(opcode);
            module.Initialize(_main);
            module.Begin();
            _dynamicModules.Enqueue(module);
            return module;
        }

        internal bool RemoveModule<T>(T module) where T : ModuleBase
        {
            if (module == null)
                return false;

            if (_modules.Remove(module))
            {
                module.Release();
                return true;
            }

            var queuedModules = _dynamicModules.ToList();
            if (!queuedModules.Remove(module))
                return false;

            _dynamicModules.Clear();
            foreach (var queuedModule in queuedModules)
            {
                _dynamicModules.Enqueue(queuedModule);
            }

            module.Release();
            return true;
        }

        private void CreateFirstModule()
        {
            var opcodeModules = GetAttributeDefineModules();

            _modules.AddRange(opcodeModules);
            _modules.Sort();
        }

        public void Init(bool create = true)
        {
            if (create)
            {
                CreateFirstModule();
            }

            foreach (var module in _modules)
            {
                try
                {
                    if (module.IsInitialize == false)
                        module.Initialize(_main);
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex.ToString());
                }
            }

        }

        public void Began()
        {
            foreach (var module in GetModules())
            {
                try
                {
                    module.Begin();
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex.ToString());
                }
            }
        }


        public void Run()
        {
            InsertDynamicModule();
            foreach (var module in GetModules())
            {
                try
                {
                    module.Run();
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex.ToString());
                }
            }
        }

        public void Release()
        {
            foreach (var module in GetModules())
            {
                try
                {
                    module.Release();
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex.ToString());
                }
            }
        }

        private void InsertDynamicModule()
        {
            var inserted = false;
            while (_dynamicModules.Count > 0)
            {
                var module = _dynamicModules.Dequeue();
                _modules.Add(module);
                inserted = true;
            }

            if (inserted)
                _modules.Sort();
        }


        private bool Contains(ModuleOpCode opcode)
        {
            foreach (var obj in _modules)
            {
                if (obj.Opcode == opcode)
                {
                    return true;
                }
            }
            return false;
        }

        private bool Contains<T>() where T : ModuleBase
        {
            return GetModule<T>() != null;
        }

        private T GetModule<T>() where T : ModuleBase
        {
            foreach (var module in GetModules())
            {
                if (module is T typedModule)
                    return typedModule;
            }
            return null;
        }

        private IEnumerable<ModuleBase> GetAttributeDefineModules()
        {
            var saveOpcodes = new Dictionary<ModuleOpCode, Type>();
            foreach (var module in GetModules())
            {
                if (module is IDataPack)
                    saveOpcodes[module.Opcode] = module.GetType();
            }

            foreach (var type in GetLoadableTypes())
            {
                if (type.IsAbstract || !typeof(ModuleBase).IsAssignableFrom(type))
                    continue;

                var attr = type.GetCustomAttribute<ModuleAttribute>(true);

                if (attr == null)
                    continue;

                if (typeof(IDataPack).IsAssignableFrom(type) && saveOpcodes.TryGetValue(attr.Opcode, out var existingType))
                {
                    Debug.LogError($"Skip module with repeated save opcode: {type.Name}, {attr.Opcode}. Existing module: {existingType.Name}");
                    continue;
                }

                var obj = Activator.CreateInstance(type) as ModuleBase;

                if (obj == null)
                    continue;

                Debug.Log($"Add {attr.Opcode} Module Success");

                obj.SetOpcode(attr.Opcode);

                if (obj is IDataPack)
                    saveOpcodes[attr.Opcode] = type;

                yield return (obj);
            }

            yield break;

        }

        public IEnumerable<ModuleBase> GetModules()
        {
            foreach (var obj in _modules)
            {
                yield return obj;
            }

            if (_dynamicModules.Count != 0)
            {
                var dmodule = _dynamicModules.ToArray();
                foreach (var module in dmodule)
                {
                    yield return module;
                }
            }

            yield break;
        }

        private static IEnumerable<Type> GetLoadableTypes()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types.Where(type => type != null).ToArray();
                }

                foreach (var type in types)
                {
                    yield return type;
                }
            }
        }

        private bool HasSaveDataOpcodeConflict(Type moduleType, ModuleOpCode opcode)
        {
            if (!typeof(IDataPack).IsAssignableFrom(moduleType))
                return false;

            foreach (var module in GetModules())
            {
                if (module.Opcode == opcode && module is IDataPack)
                    return true;
            }

            return false;
        }
    }
}

