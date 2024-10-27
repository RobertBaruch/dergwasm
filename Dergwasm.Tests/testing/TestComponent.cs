﻿using System;
using System.Collections.Generic;
using System.Reflection;
using Elements.Core;
using FrooxEngine;

namespace DergwasmTests.testing
{
    public class TestComponent : Component
    {
        FakeWorldServices worldServices;
        public Sync<int> IntField;
        public Sync<float> FloatField;
        public Sync<double> DoubleField;
        public SyncRef<TestComponent> ComponentRefField;
        public SyncRef<IField<int>> IntFieldRefField;
        public SyncType TypeField;

        public TestComponent(FakeWorldServices worldServices)
        {
            this.worldServices = worldServices;
        }

        public override ISyncMember GetSyncMember(int index)
        {
            switch (index)
            {
                case 0:
                    return persistent;
                case 1:
                    return updateOrder;
                case 2:
                    return EnabledField;
                case 3:
                    return IntField;
                case 4:
                    return FloatField;
                case 5:
                    return DoubleField;
                case 6:
                    return ComponentRefField;
                case 7:
                    return IntFieldRefField;
                case 8:
                    return TypeField;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        string[] SyncMemberNames = new string[]
        {
            "persistent",
            "updateOrder",
            "EnabledField",
            "IntField",
            "FloatField",
            "DoubleField",
            "ComponentRefField",
            "IntFieldRefField",
            "TypeField",
        };

        protected override void InitializeSyncMembers()
        {
            base.InitializeSyncMembers();
            IntField = new Sync<int>();
            FloatField = new Sync<float>();
            DoubleField = new Sync<double>();
            ComponentRefField = new SyncRef<TestComponent>();
            IntFieldRefField = new SyncRef<IField<int>>();
            TypeField = new SyncType();

            SetRefId(this);
            SetRefId(IntField);
            SetRefId(FloatField);
            SetRefId(DoubleField);
            SetRefId(ComponentRefField);
            SetRefId(IntFieldRefField);
            SetRefId(TypeField);
        }

        protected override void OnAwake()
        {
            base.OnAwake();
        }

        void SetRefId(IWorldElement obj)
        {
            // This nonsense is required because Component's ReferenceID has a private setter
            // in a base class.
            PropertyInfo propertyInfo = obj.GetType().GetProperty("ReferenceID");
            var setterMethod = propertyInfo.GetSetMethod(true);
            if (setterMethod == null)
                setterMethod = propertyInfo.DeclaringType
                    .GetProperty("ReferenceID")
                    .GetSetMethod(true);
            RefID refID = worldServices.GetNextRefID();
            setterMethod.Invoke(obj, new object[] { refID });
            worldServices.AddRefID(obj, refID);
        }

        void SetInfo()
        {
            List<FieldInfo> fieldInfos = new List<FieldInfo>();
            Dictionary<string, int> syncMemberNameToIndex = new Dictionary<string, int>();

            try
            {
                for (int i = 0; ; ++i)
                {
                    ISyncMember member = GetSyncMember(i);
                    string name = SyncMemberNames[i];
                    FieldInfo fieldInfo = GetType().GetField(name);
                    fieldInfos.Add(fieldInfo);
                    syncMemberNameToIndex[name] = i;
                }
            }
            catch (Exception) { }

            WorkerInitInfo initInfo = new WorkerInitInfo
            {
                syncMemberFields = fieldInfos.ToArray(),
                syncMemberNames = SyncMemberNames,
                syncMemberNameToIndex = syncMemberNameToIndex,
            };

            GetType()
                .GetField("InitInfo", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(this, initInfo);
        }

        public void Initialize()
        {
            InitializeSyncMembers();
            SetInfo();
            OnAwake();
        }
    }
}
