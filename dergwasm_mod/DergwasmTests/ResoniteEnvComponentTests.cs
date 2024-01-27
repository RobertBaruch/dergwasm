using System;
using System.Collections.Generic;
using System.Reflection;
using Derg;
using Elements.Core;
using FrooxEngine;
using Xunit;

namespace DergwasmTests
{
    public class ResoniteEnvComponentTests : TestMachine
    {
        FakeWorldServices worldServices;
        ResoniteEnv env;
        TestEmscriptenEnv emscriptenEnv;
        Frame frame;
        TestComponent testComponent;

        public class TestComponent : Component
        {
            FakeWorldServices worldServices;
            public Sync<int> IntField;
            public Sync<float> FloatField;
            public Sync<double> DoubleField;

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
            };

            protected override void InitializeSyncMembers()
            {
                base.InitializeSyncMembers();
                IntField = new Sync<int>();
                FloatField = new Sync<float>();
                DoubleField = new Sync<double>();

                SetRefId(this);
                SetRefId(IntField);
                SetRefId(FloatField);
                SetRefId(DoubleField);
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
                    setterMethod = propertyInfo
                        .DeclaringType
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

        public ResoniteEnvComponentTests()
        {
            ResonitePatches.Apply();
            worldServices = new FakeWorldServices();
            emscriptenEnv = new TestEmscriptenEnv();
            env = new ResoniteEnv(this, worldServices, emscriptenEnv);
            SimpleSerialization.Initialize(env);
            frame = emscriptenEnv.EmptyFrame(null);

            testComponent = new TestComponent(worldServices);
            testComponent.Initialize();
        }

        [Fact]
        public void GetMemberTest()
        {
            int namePtr = emscriptenEnv.AllocateUTF8StringInMem(frame, "IntField");
            int outTypePtr = namePtr + 100;
            int outRefIdPtr = outTypePtr + sizeof(int);

            Assert.Equal(
                0,
                env.component__get_member(
                    frame,
                    (ulong)testComponent.ReferenceID,
                    namePtr,
                    outTypePtr,
                    outRefIdPtr
                )
            );
            Assert.Equal(
                ResoniteEnv.ResoniteType.ValueInt,
                (ResoniteEnv.ResoniteType)HeapGet<int>(outTypePtr)
            );
            Assert.Equal(testComponent.IntField.ReferenceID, HeapGet<ulong>(outRefIdPtr));
        }

        [Fact]
        public void GetMemberFailsOnNonexistentRefID()
        {
            int namePtr = emscriptenEnv.AllocateUTF8StringInMem(frame, "IntField");
            int outTypePtr = namePtr + 100;
            int outRefIdPtr = outTypePtr + sizeof(int);

            Assert.Equal(
                -1,
                env.component__get_member(
                    frame,
                    0xFFFFFFFFFFFFFFFFUL,
                    namePtr,
                    outTypePtr,
                    outRefIdPtr
                )
            );
        }

        [Fact]
        public void GetMemberFailsOnNoncomponentRefID()
        {
            int namePtr = emscriptenEnv.AllocateUTF8StringInMem(frame, "IntField");
            int outTypePtr = namePtr + 100;
            int outRefIdPtr = outTypePtr + sizeof(int);

            Assert.Equal(
                -1,
                env.component__get_member(
                    frame,
                    (ulong)testComponent.IntField.ReferenceID,
                    namePtr,
                    outTypePtr,
                    outRefIdPtr
                )
            );
        }

        [Fact]
        public void GetMemberFailsOnNullNamePtr()
        {
            int namePtr = 0;
            int outTypePtr = namePtr + 100;
            int outRefIdPtr = outTypePtr + sizeof(int);

            Assert.Equal(
                -1,
                env.component__get_member(
                    frame,
                    (ulong)testComponent.ReferenceID,
                    namePtr,
                    outTypePtr,
                    outRefIdPtr
                )
            );
        }

        [Fact]
        public void GetMemberFailsOnNullTypePtr()
        {
            int namePtr = emscriptenEnv.AllocateUTF8StringInMem(frame, "IntField");
            int outTypePtr = 0;
            int outRefIdPtr = outTypePtr + sizeof(int);

            Assert.Equal(
                -1,
                env.component__get_member(
                    frame,
                    (ulong)testComponent.ReferenceID,
                    namePtr,
                    outTypePtr,
                    outRefIdPtr
                )
            );
        }

        [Fact]
        public void GetMemberFailsOnNullRefIdPtr()
        {
            int namePtr = emscriptenEnv.AllocateUTF8StringInMem(frame, "IntField");
            int outTypePtr = namePtr + 100;
            int outRefIdPtr = 0;

            Assert.Equal(
                -1,
                env.component__get_member(
                    frame,
                    (ulong)testComponent.ReferenceID,
                    namePtr,
                    outTypePtr,
                    outRefIdPtr
                )
            );
        }

        [Fact]
        public void GetMemberFailsOnNonexistentField()
        {
            int namePtr = emscriptenEnv.AllocateUTF8StringInMem(frame, "CatFace");
            int outTypePtr = namePtr + 100;
            int outRefIdPtr = outTypePtr + sizeof(int);

            Assert.Equal(
                -1,
                env.component__get_member(
                    frame,
                    (ulong)testComponent.IntField.ReferenceID,
                    namePtr,
                    outTypePtr,
                    outRefIdPtr
                )
            );
        }
    }
}
