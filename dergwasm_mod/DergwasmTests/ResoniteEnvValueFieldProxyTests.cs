﻿using System.Reflection;
using Derg;
using Elements.Core;
using FrooxEngine;
using Xunit;

namespace DergwasmTests
{
    public class ResoniteEnvValueFieldProxyTests : TestMachine
    {
        TestWorldServices worldServices;
        ResoniteEnv env;
        TestEmscriptenEnv emscriptenEnv;
        Frame frame;

        public ResoniteEnvValueFieldProxyTests()
        {
            ResonitePatches.Apply();
            worldServices = new TestWorldServices();
            emscriptenEnv = new TestEmscriptenEnv();
            env = new ResoniteEnv(this, worldServices, emscriptenEnv);
            frame = emscriptenEnv.EmptyFrame(null);
        }

        public class EmptyComponent : Component
        {
            void SetRefId(object obj, ulong i)
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
                setterMethod.Invoke(obj, new object[] { new RefID(i) });
            }

            public EmptyComponent(ulong refID)
            {
                SetRefId(this, refID);
            }
        }

        void SetRefId(IWorldElement obj, ulong i)
        {
            // This nonsense is required because a WorldElement's ReferenceID has a private setter
            // in a base class.
            PropertyInfo propertyInfo = obj.GetType().GetProperty("ReferenceID");
            var setterMethod = propertyInfo.GetSetMethod(true);
            if (setterMethod == null)
                setterMethod = propertyInfo
                    .DeclaringType
                    .GetProperty("ReferenceID")
                    .GetSetMethod(true);
            setterMethod.Invoke(obj, new object[] { new RefID(i) });
            worldServices.AddRefID(obj, i);
        }

        void Initialize(Component component)
        {
            component
                .GetType()
                .GetMethod("InitializeSyncMembers", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(component, new object[] { });
            component
                .GetType()
                .GetMethod("OnAwake", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(component, new object[] { });
        }

        [Fact]
        public void GetSourceFailsOnBadRefIDTest()
        {
            Assert.Equal(0UL, env.value_field_proxy__get_source(frame, 101));
        }

        [Fact]
        public void GetSourceFailsOnIncorrectComponentTest()
        {
            Sync<int> valueField = new Sync<int>();
            SetRefId(valueField, 100);

            Assert.Equal(0UL, env.value_field_proxy__get_source(frame, 100));
        }

        [Fact]
        public void GetSourceNullTest()
        {
            ValueFieldProxy<int> valueFieldProxy = new ValueFieldProxy<int>();
            Initialize(valueFieldProxy);
            SetRefId(valueFieldProxy, 101);

            Assert.Equal(0UL, env.value_field_proxy__get_source(frame, 101));
        }

        [Fact]
        public void GetSourceTest()
        {
            Sync<int> valueField = new Sync<int>();
            SetRefId(valueField, 100);
            valueField.Value = 1;

            ValueFieldProxy<int> valueFieldProxy = new ValueFieldProxy<int>();
            Initialize(valueFieldProxy);
            SetRefId(valueFieldProxy, 101);
            valueFieldProxy.Source.Target = valueField;

            Assert.Equal(100UL, env.value_field_proxy__get_source(frame, 101));
        }

        [Fact]
        public void GetValueFailsOnBadRefIDTest()
        {
            Assert.Equal(0, env.value_field_proxy__get_value(frame, 101, 0));
        }

        [Fact]
        public void GetValueFailsOnIncorrectComponentTest()
        {
            Sync<int> valueField = new Sync<int>();
            SetRefId(valueField, 100);

            Assert.Equal(0, env.value_field_proxy__get_value(frame, 100, 0));
        }

        [Fact]
        public void GetValueTest()
        {
            Sync<int> valueField = new Sync<int>();
            SetRefId(valueField, 100);
            valueField.Value = 1;

            ValueFieldProxy<int> valueFieldProxy = new ValueFieldProxy<int>();
            Initialize(valueFieldProxy);
            SetRefId(valueFieldProxy, 101);
            valueFieldProxy.Source.Target = valueField;

            int lenPtr = emscriptenEnv.Malloc(frame, sizeof(int));
            int dataPtr = env.value_field_proxy__get_value(frame, 101, lenPtr);

            Assert.Equal(8, env.machine.HeapGet<int>(lenPtr));
            object value = SimpleSerialization.Deserialize(this, env, dataPtr);
            Assert.NotNull(value);
            Assert.IsType<int>(value);
            Assert.Equal(1, (int)value);
        }

        [Fact]
        public void SetSourceFailsOnBadRefIDTest()
        {
            Assert.Equal(-1, env.value_field_proxy__set_source(frame, 101, 100));
        }

        [Fact]
        public void SetSourceFailsOnIncorrectComponentTest()
        {
            Sync<int> valueField = new Sync<int>();
            SetRefId(valueField, 100);

            Assert.Equal(-1, env.value_field_proxy__set_source(frame, 100, 100));
        }

        [Fact]
        public void SetSourceTest()
        {
            Sync<int> valueField = new Sync<int>();
            SetRefId(valueField, 100);
            valueField.Value = 1;

            ValueFieldProxy<int> valueFieldProxy = new ValueFieldProxy<int>();
            Initialize(valueFieldProxy);
            SetRefId(valueFieldProxy, 101);

            Assert.Equal(0, env.value_field_proxy__set_source(frame, 101, 100));
            Assert.Equal(100UL, env.value_field_proxy__get_source(frame, 101));
        }

        [Fact]
        public void SetSourceFailsOnIncorrectTypeTest()
        {
            Sync<uint> valueField = new Sync<uint>();
            SetRefId(valueField, 100);
            valueField.Value = 1;

            ValueFieldProxy<int> valueFieldProxy = new ValueFieldProxy<int>();
            Initialize(valueFieldProxy);
            SetRefId(valueFieldProxy, 101);

            Assert.Equal(-1, env.value_field_proxy__set_source(frame, 101, 100));
        }

        [Fact]
        public void SetValueFailsOnBadRefIDTest()
        {
            int dataPtr = SimpleSerialization.Serialize(env.machine, env, frame, 12, out int _);
            Assert.Equal(-1, env.value_field_proxy__set_value(frame, 101, dataPtr));
        }

        [Fact]
        public void SetValueFailsOnIncorrectComponentTest()
        {
            Sync<int> valueField = new Sync<int>();
            SetRefId(valueField, 100);
            int dataPtr = SimpleSerialization.Serialize(env.machine, env, frame, 12, out int _);

            Assert.Equal(-1, env.value_field_proxy__set_value(frame, 100, dataPtr));
        }

        [Fact]
        public void SetValueTest()
        {
            Sync<int> valueField = new Sync<int>();
            SetRefId(valueField, 100);
            valueField.Value = 1;

            ValueFieldProxy<int> valueFieldProxy = new ValueFieldProxy<int>();
            Initialize(valueFieldProxy);
            SetRefId(valueFieldProxy, 101);
            valueFieldProxy.Source.Target = valueField;
            int dataPtr = SimpleSerialization.Serialize(env.machine, env, frame, 12, out int _);

            Assert.Equal(0, env.value_field_proxy__set_value(frame, 101, dataPtr));
            Assert.Equal(12, valueFieldProxy.Source.Target.Value);
        }

        [Fact]
        public void SetValueFailsOnIncorrectTypeTest()
        {
            Sync<int> valueField = new Sync<int>();
            SetRefId(valueField, 100);
            valueField.Value = 1;

            ValueFieldProxy<int> valueFieldProxy = new ValueFieldProxy<int>();
            Initialize(valueFieldProxy);
            SetRefId(valueFieldProxy, 101);
            valueFieldProxy.Source.Target = valueField;
            int dataPtr = SimpleSerialization.Serialize(env.machine, env, frame, 12U, out int _);

            Assert.Equal(-1, env.value_field_proxy__set_value(frame, 101, dataPtr));
            Assert.Equal(1, valueFieldProxy.Source.Target.Value);
        }
    }
}