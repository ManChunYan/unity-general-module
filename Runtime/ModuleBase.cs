using System;
using UnityEngine;

namespace General.Module
{
    public class ModuleBase : IComparable<ModuleBase>
    {
        protected MainBase _mainBase = null;

        public ModuleOpCode Opcode { get; protected set; }

        public bool IsInitialize { get; private set; }

        private bool IsRun;

        /// <summary>
        /// Sets the module opcode. Call this only during module setup.
        /// </summary>
        public void SetOpcode(ModuleOpCode opcode)
        {
            Opcode = opcode;
        }

        public T GetModule<T>() where T : ModuleBase
        {
            return _mainBase.GetModule<T>();
        }

        public T AddModule<T>() where T : ModuleBase
        {
            return _mainBase.AddModule<T>();
        }

        internal void Initialize(MainBase main)
        {
            _mainBase = main;
            IsInitialize = true;
            OnInit();
        }

        internal void Begin()
        {
            OnBegin();
            IsRun = true;
        }

        internal void Run()
        {
            if (IsRun)
                OnRun();
        }

        internal void Release()
        {
            OnRelease();
            IsRun = false;
        }

        internal void Log(object obj, LogType type = LogType.Log) { }

        protected virtual void OnInit() { }
        protected virtual void OnBegin() { }
        protected virtual void OnRun() { }
        protected virtual void OnRelease() { }

        public int CompareTo(ModuleBase othermodule)
        {
            if ((uint)othermodule.Opcode > (uint)Opcode)
                return -1;
            else if ((uint)othermodule.Opcode == (uint)Opcode)
                return 0;
            return 1;
        }
    }
}
