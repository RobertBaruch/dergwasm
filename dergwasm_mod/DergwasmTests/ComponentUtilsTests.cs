using System;
using System.Reflection;
using Derg;
using Elements.Core;
using FrooxEngine;
using Xunit;

namespace DergwasmTests
{
    public class ComponentUtilsTests
    {
        public class TestComponent : Component
        {
            public Sync<int> IntField;
            public SyncRef<TestComponent> ComponentRefField;
            public SyncRef<IField<int>> IntFieldRefField;
            public SyncType TypeField;

            public int IntProperty
            {
                get { return IntField.Value; }
                set { IntField.Value = value; }
            }

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

            public TestComponent()
            {
                IntField = new Sync<int>();
                ComponentRefField = new SyncRef<TestComponent>();
                IntFieldRefField = new SyncRef<IField<int>>();
                TypeField = new SyncType();

                SetRefId(this, 100);
                SetRefId(IntField, 101);
                SetRefId(ComponentRefField, 102);
                SetRefId(IntFieldRefField, 103);
                SetRefId(TypeField, 104);
            }
        }

        public ComponentUtilsTests()
        {
            ResonitePatches.Apply();
        }

        [Fact]
        public void GetSyncValueTest()
        {
            var testComponent = new TestComponent();
            testComponent.IntField.Value = 1;
            object value;
            Assert.True(ComponentUtils.GetFieldValue(testComponent, "IntField", out value));
            Assert.Equal(1, value);
        }

        [Fact]
        public void GetSyncRefTest()
        {
            var testComponent = new TestComponent();
            testComponent.ComponentRefField.Target = testComponent;
            object value;

            Assert.True(
                ComponentUtils.GetFieldValue(testComponent, "ComponentRefField", out value)
            );
            Assert.Same(testComponent, value);
        }

        [Fact]
        public void GetSyncTypeTest()
        {
            var testComponent = new TestComponent();
            testComponent.TypeField.Value = typeof(int4);
            object value;
            Assert.True(ComponentUtils.GetFieldValue(testComponent, "TypeField", out value));
            Assert.Equal(typeof(int4), value);
        }

        [Fact]
        public void GetPropertyTest()
        {
            var testComponent = new TestComponent();
            testComponent.IntField.Value = 1;
            object value;

            Assert.True(ComponentUtils.GetFieldValue(testComponent, "IntProperty", out value));
            Assert.Equal(1, value);
        }

        [Fact]
        public void GetNullIntFieldRefFieldTest()
        {
            var testComponent = new TestComponent();
            object value;

            Assert.True(ComponentUtils.GetFieldValue(testComponent, "IntFieldRefField", out value));
            Assert.Null(value);
        }

        [Fact]
        public void SetSyncValueTest()
        {
            var testComponent = new TestComponent();
            testComponent.IntField.Value = 1;

            Assert.True(ComponentUtils.SetFieldValue(testComponent, "IntField", 12));

            object value;
            Assert.True(ComponentUtils.GetFieldValue(testComponent, "IntField", out value));
            Assert.Equal(12, value);
        }

        [Fact]
        public void SetSyncRefTest()
        {
            var testComponent = new TestComponent();

            Assert.True(
                ComponentUtils.SetFieldValue(testComponent, "ComponentRefField", testComponent)
            );

            object value;
            Assert.True(
                ComponentUtils.GetFieldValue(testComponent, "ComponentRefField", out value)
            );
            Assert.Same(testComponent, value);
        }

        [Fact]
        public void SetSyncTypeTest()
        {
            var testComponent = new TestComponent();
            testComponent.TypeField.Value = typeof(int);

            Assert.True(ComponentUtils.SetFieldValue(testComponent, "TypeField", typeof(int3)));

            object value;
            Assert.True(ComponentUtils.GetFieldValue(testComponent, "TypeField", out value));
            Assert.Equal(typeof(int3), value);
        }

        [Fact]
        public void SetPropertyTest()
        {
            var testComponent = new TestComponent();
            testComponent.IntField.Value = 1;

            Assert.True(ComponentUtils.SetFieldValue(testComponent, "IntProperty", 12));

            object value;
            Assert.True(ComponentUtils.GetFieldValue(testComponent, "IntProperty", out value));
            Assert.Equal(12, value);
        }

        [Fact]
        public void SetPropertyWithWrongTypeTest()
        {
            var testComponent = new TestComponent();
            testComponent.IntField.Value = 1;

            Assert.False(ComponentUtils.SetFieldValue(testComponent, "IntProperty", "12"));
        }

        [Fact]
        public void SetNonexistentFieldTest()
        {
            var testComponent = new TestComponent();

            Assert.False(ComponentUtils.SetFieldValue(testComponent, "NonexistentField", 12));
        }

        [Fact]
        public void SetIntFieldRefFieldTest()
        {
            var testComponent = new TestComponent();

            Assert.True(
                ComponentUtils.SetFieldValue(
                    testComponent,
                    "IntFieldRefField",
                    testComponent.IntField
                )
            );
        }

        [Fact]
        public void GetIntFieldRefFieldTest()
        {
            var testComponent = new TestComponent();
            testComponent.IntField.Value = 1;
            ComponentUtils.SetFieldValue(testComponent, "IntFieldRefField", testComponent.IntField);
            object field;
            ComponentUtils.GetFieldValue(testComponent, "IntFieldRefField", out field);

            Assert.NotNull(field);
            Assert.IsType<Sync<int>>(field);

            object value;
            Assert.True(ComponentUtils.GetFieldValue(field, "Value", out value));
            Assert.Equal(1, value);
        }
    }
}
