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
        }
    }

    public class ComponentUtilsTests
    {
        public World world;

        public ComponentUtilsTests()
        {
            //World world = World.JoinSession(null, new Uri[] { });
            //Assert.NotNull(world.ConnectorManager);
        }

        //[Fact]
        //public void GetSyncValueTest()
        //{
        //    var testComponent = new ValueField<int>();
        //    var intField = new Sync<int>();

        //    FieldInfo fieldInfo = testComponent.GetType().GetField("Value");
        //    fieldInfo.SetValue(testComponent, intField);

        //    fieldInfo = typeof(Sync<int>).GetField(
        //        "_value",
        //        BindingFlags.Instance | BindingFlags.NonPublic
        //    );
        //    fieldInfo.SetValue(intField, 1);

        //    Assert.Equal(1, ComponentUtils.GetFieldValue(testComponent, "Value"));
        //}

        [Fact]
        public void GetSyncValueTest()
        {
            var testComponent = new TestComponent();
            FieldInfo fieldInfo = typeof(Sync<int>).GetField(
                "_value",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            fieldInfo.SetValue(testComponent.IntField, 1);

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
            FieldInfo fieldInfo = typeof(Sync<int>).GetField(
                "_value",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            fieldInfo.SetValue(testComponent.IntField, 1);

            Assert.Equal(1, ComponentUtils.GetFieldValue(testComponent, "IntProperty"));
        }

        [Fact]
        public void SetSyncValueTest()
        {
            var testComponent = new TestComponent();
            FieldInfo fieldInfo = typeof(Sync<int>).GetField(
                "_value",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            fieldInfo.SetValue(testComponent.IntField, 1);

            ComponentUtils.SetFieldValue(testComponent, "IntField", 12);

            Assert.Equal(12, ComponentUtils.GetFieldValue(testComponent, "IntField"));
        }
    }
}
