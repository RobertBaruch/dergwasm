using System;
using System.Reflection;
using Derg;
using FrooxEngine;
using Xunit;

namespace DergwasmTests
{
    public class TestComponent : Component
    {
        public Sync<int> IntField;

        public TestComponent(World world)
        {
            IntField = new Sync<int> { Value = 1 };
            IntField.Initialize(world, null);
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

        [Fact]
        public void GetSyncValueTest()
        {
            var testComponent = new ValueField<int>();
            var intField = new Sync<int>();

            FieldInfo fieldInfo = testComponent.GetType().GetField("Value");
            fieldInfo.SetValue(testComponent, intField);

            fieldInfo = typeof(Sync<int>).GetField(
                "_value",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            fieldInfo.SetValue(intField, 1);

            Assert.Equal(1, ComponentUtils.GetFieldValue(testComponent, "Value"));
        }

        [Fact]
        public void SetSyncValueTest()
        {
            var testComponent = new ValueField<int>();
            var intField = new Sync<int>();

            FieldInfo fieldInfo = testComponent.GetType().GetField("Value");
            fieldInfo.SetValue(testComponent, intField);

            fieldInfo = typeof(Sync<int>).GetField(
                "_value",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            fieldInfo.SetValue(intField, 1);

            ComponentUtils.SetFieldValue(testComponent, "Value", 12);

            Assert.Equal(12, ComponentUtils.GetFieldValue(testComponent, "Value"));
        }
    }
}
