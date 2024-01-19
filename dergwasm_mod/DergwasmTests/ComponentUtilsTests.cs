using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BepuPhysics.Constraints;
using Derg;
using FrooxEngine;
using HarmonyLib;
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
            World world = World.JoinSession(null, new Uri[] { });
            Assert.NotNull(world.ConnectorManager);
        }

        [Fact]
        public void GetSyncValueTest()
        {
            var testComponent = new TestComponent(world);
            Assert.Equal(1, ComponentUtils.GetFieldValue(testComponent, "Value"));
        }
    }
}
