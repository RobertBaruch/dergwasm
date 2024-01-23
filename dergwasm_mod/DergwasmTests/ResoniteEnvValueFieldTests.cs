using System.Reflection;
using Derg;
using Elements.Core;
using FrooxEngine;
using Xunit;

namespace DergwasmTests
{
    public class ResoniteEnvValueFieldTests : TestMachine
    {
        TestWorldServices worldServices;
        ResoniteEnv env;
        TestEmscriptenEnv emscriptenEnv;
        Frame frame;

        public ResoniteEnvValueFieldTests()
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
        public void GetValueUnsetStringDefaultIsNullTest()
        {
            ValueField<string> valueField = new ValueField<string>();
            Initialize(valueField);
            SetRefId(valueField, 100);

            int dataPtr = env.value_field__get_value(frame, 100, 0);

            Assert.NotEqual(0, dataPtr);

            object value = SimpleSerialization.Deserialize(this, env, dataPtr);
            Assert.Null(value);
        }

        [Fact]
        public void GetValueUnsetIntIsDefaultedTest()
        {
            ValueField<int> valueField = new ValueField<int>();
            Initialize(valueField);
            SetRefId(valueField, 100);

            int dataPtr = env.value_field__get_value(frame, 100, 0);

            Assert.NotEqual(0, dataPtr);

            object value = SimpleSerialization.Deserialize(this, env, dataPtr);
            Assert.NotNull(value);
            Assert.IsType<int>(value);
            Assert.Equal(0, (int)value);
        }

        [Fact]
        public void GetValueIntTest()
        {
            ValueField<int> valueField = new ValueField<int>();
            Initialize(valueField);
            SetRefId(valueField, 100);
            valueField.Value.Value = 1;

            int dataPtr = env.value_field__get_value(frame, 100, 0);

            Assert.NotEqual(0, dataPtr);

            object value = SimpleSerialization.Deserialize(this, env, dataPtr);
            Assert.IsType<int>(value);
            Assert.Equal(1, (int)value);
        }

        [Fact]
        public void GetValueWithLengthTest()
        {
            ValueField<int> valueField = new ValueField<int>();
            Initialize(valueField);
            SetRefId(valueField, 100);
            valueField.Value.Value = 1;
            int lenPtr = emscriptenEnv.Malloc(frame, sizeof(int));

            env.value_field__get_value(frame, 100, lenPtr);

            Assert.Equal(8, env.machine.HeapGet<int>(lenPtr));
        }

        [Fact]
        public void SetValueTest()
        {
            ValueField<int> valueField = new ValueField<int>();
            Initialize(valueField);
            SetRefId(valueField, 100);
            valueField.Value.Value = 1;
            int dataPtr = SimpleSerialization.Serialize(env.machine, env, frame, 12, out int _);

            Assert.Equal(0, env.value_field__set_value(frame, 100, dataPtr));
            Assert.Equal(12, valueField.Value.Value);
        }

        [Fact]
        public void SetValueNullSetsDefaultOnNonnullableTypeTest()
        {
            ValueField<int> valueField = new ValueField<int>();
            Initialize(valueField);
            SetRefId(valueField, 100);
            valueField.Value.Value = 1;
            int dataPtr = SimpleSerialization.Serialize(env.machine, env, frame, null, out int _);

            Assert.Equal(0, env.value_field__set_value(frame, 100, dataPtr));
            Assert.Equal(0, valueField.Value.Value);
        }

        [Fact]
        public void SetValueNullSucceedsOnNullableTypeTest()
        {
            ValueField<string> valueField = new ValueField<string>();
            Initialize(valueField);
            SetRefId(valueField, 100);
            valueField.Value.Value = "12";
            int dataPtr = SimpleSerialization.Serialize(env.machine, env, frame, null, out int _);

            Assert.Equal(0, env.value_field__set_value(frame, 100, dataPtr));
            Assert.Null(valueField.Value.Value);
        }

        [Fact]
        public void SetValueSetsNullOnBadSerialization()
        {
            ValueField<int> valueField = new ValueField<int>();
            Initialize(valueField);
            SetRefId(valueField, 100);
            valueField.Value.Value = 1;
            int dataPtr = emscriptenEnv.Malloc(frame, 4);
            env.machine.HeapSet<int>(dataPtr, SimpleSerialization.SimpleType.Unknown);

            Assert.Equal(0, env.value_field__set_value(frame, 100, dataPtr));
            Assert.Equal(0, valueField.Value.Value);
        }

        [Fact]
        public void SetValueFailsOnBadType()
        {
            ValueField<int> valueField = new ValueField<int>();
            Initialize(valueField);
            SetRefId(valueField, 100);
            valueField.Value.Value = 1;
            int dataPtr = SimpleSerialization.Serialize(env.machine, env, frame, 12u, out int _);

            Assert.Equal(-1, env.value_field__set_value(frame, 100, dataPtr));
            Assert.Equal(1, valueField.Value.Value);
        }

        [Fact]
        public void SetValueFailsOnNonexistentRefID()
        {
            int dataPtr = SimpleSerialization.Serialize(env.machine, env, frame, 12, out int _);

            Assert.Equal(-1, env.value_field__set_value(frame, 200, dataPtr));
        }

        [Fact]
        public void SetValueFailsOnRefIDNotValueField()
        {
            EmptyComponent component = new EmptyComponent(100);
            int dataPtr = SimpleSerialization.Serialize(env.machine, env, frame, 12, out int _);

            Assert.Equal(-1, env.value_field__set_value(frame, 100, dataPtr));
        }
    }
}
