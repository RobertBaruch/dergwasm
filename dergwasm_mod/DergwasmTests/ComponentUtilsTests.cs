using System;
using System.Reflection;
using Derg;
using Elements.Core;
using FrooxEngine;
using Xunit;

namespace DergwasmTests
{
    public class TestComponent : Component
    {
        public Sync<int> IntField;
        public SyncRef<TestComponent> ComponentRefField;

        public int IntProperty
        {
            get { return IntField.Value; }
            set { IntField.Value = value; }
        }

        public TestComponent()
        {
            IntField = new Sync<int>();
            ComponentRefField = new SyncRef<TestComponent>();

            // This nonsense is required because Component's ReferenceID has a private setter
            // in a base class.
            PropertyInfo propertyInfo = GetType().GetProperty("ReferenceID");
            var setterMethod = propertyInfo.GetSetMethod(true);
            if (setterMethod == null)
                setterMethod = propertyInfo
                    .DeclaringType
                    .GetProperty("ReferenceID")
                    .GetSetMethod(true);
            setterMethod.Invoke(this, new object[] { new RefID(100) });
        }

        public void InternalSetIntField(int value)
        {
            FieldInfo fieldInfo = typeof(Sync<int>).GetField(
                "_value",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            fieldInfo.SetValue(IntField, value);
        }
    }

    public class ComponentUtilsTests
    {
        [Fact]
        public void GetSyncValueTest()
        {
            var testComponent = new TestComponent();
            testComponent.InternalSetIntField(1);

            Assert.Equal(1, ComponentUtils.GetFieldValue(testComponent, "IntField"));
        }

        [Fact]
        public void GetSyncRefTest()
        {
            var testComponent = new TestComponent();
            FieldInfo fieldInfo = typeof(SyncRef<TestComponent>).GetField(
                "_target",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            fieldInfo.SetValue(testComponent.ComponentRefField, testComponent);

            Assert.Same(
                testComponent,
                ComponentUtils.GetFieldValue(testComponent, "ComponentRefField")
            );
        }

        [Fact]
        public void GetPropertyTest()
        {
            var testComponent = new TestComponent();
            testComponent.InternalSetIntField(1);

            Assert.Equal(1, ComponentUtils.GetFieldValue(testComponent, "IntProperty"));
        }

        [Fact]
        public void SetSyncValueTest()
        {
            ResonitePatches.Apply();

            var testComponent = new TestComponent();
            testComponent.InternalSetIntField(1);

            ComponentUtils.SetFieldValue(testComponent, "IntField", 12);

            Assert.Equal(12, ComponentUtils.GetFieldValue(testComponent, "IntField"));
        }

        [Fact]
        public void SetSyncRefTest()
        {
            var testComponent = new TestComponent();

            ComponentUtils.SetFieldValue(testComponent, "ComponentRefField", testComponent);

            Assert.Same(
                testComponent,
                ComponentUtils.GetFieldValue(testComponent, "ComponentRefField")
            );
        }

        [Fact]
        public void SetPropertyTest()
        {
            ResonitePatches.Apply();

            var testComponent = new TestComponent();
            testComponent.InternalSetIntField(1);

            ComponentUtils.SetFieldValue(testComponent, "IntProperty", 12);

            Assert.Equal(12, ComponentUtils.GetFieldValue(testComponent, "IntProperty"));
        }
    }
}
