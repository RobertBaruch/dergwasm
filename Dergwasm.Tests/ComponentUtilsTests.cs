using Dergwasm.Resonite;
using DergwasmTests.testing;
using Elements.Core;
using FrooxEngine;
using Xunit;

namespace DergwasmTests
{
    public class ComponentUtilsTests
    {
        FakeWorld world;
        TestComponent testComponent;

        public ComponentUtilsTests()
        {
            ResonitePatches.Apply();

            world = new FakeWorld();
            testComponent = new TestComponent(world);
            testComponent.Initialize();
        }

        [Fact]
        public void GetSyncValueTest()
        {
            testComponent.IntField.Value = 1;
            object value;
            Assert.True(ComponentUtils.GetFieldValue(testComponent, "IntField", out value));
            Assert.Equal(1, value);
        }

        [Fact]
        public void GetSyncRefTest()
        {
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
            testComponent.TypeField.Value = typeof(int4);
            object value;
            Assert.True(ComponentUtils.GetFieldValue(testComponent, "TypeField", out value));
            Assert.Equal(typeof(int4), value);
        }

        [Fact]
        public void GetNullIntFieldRefFieldTest()
        {
            object value;

            Assert.True(ComponentUtils.GetFieldValue(testComponent, "IntFieldRefField", out value));
            Assert.Null(value);
        }

        [Fact]
        public void SetSyncValueTest()
        {
            testComponent.IntField.Value = 1;

            Assert.True(ComponentUtils.SetFieldValue(testComponent, "IntField", 12));

            object value;
            Assert.True(ComponentUtils.GetFieldValue(testComponent, "IntField", out value));
            Assert.Equal(12, value);
        }

        [Fact]
        public void SetSyncRefTest()
        {
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
            testComponent.TypeField.Value = typeof(int);

            Assert.True(ComponentUtils.SetFieldValue(testComponent, "TypeField", typeof(int3)));

            object value;
            Assert.True(ComponentUtils.GetFieldValue(testComponent, "TypeField", out value));
            Assert.Equal(typeof(int3), value);
        }

        [Fact]
        public void SetNonexistentFieldTest()
        {
            Assert.False(ComponentUtils.SetFieldValue(testComponent, "NonexistentField", 12));
        }

        [Fact]
        public void SetIntFieldRefFieldTest()
        {
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
