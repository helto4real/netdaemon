using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using JoySoftware.HomeAssistant.Model;
using NetDaemon.Infrastructure.Extensions;
using NetDaemon.Mapping;
using Xunit;

namespace NetDaemon.Daemon.Tests.Daemon
{
    public class ExtensionMethodUnitTests
    {
        [Fact]
        public void JsonElementToDynamicValueWhenStringShouldReturnString()
        {
            // ARRANGE
            var doc = JsonDocument.Parse("{\"str\": \"string\"}");
            var prop = doc.RootElement.GetProperty("str");

            // ACT
            var obj = prop.ConvertToDynamicValue();

            // ASSERT
            Assert.IsType<string>(obj);
            Assert.Equal("string", obj);
        }

        [Fact]
        public void JsonElementToDynamicValueWhenTrueShouldReturnBoolTrue()
        {
            // ARRANGE
            var doc = JsonDocument.Parse("{\"bool\": true}");
            var prop = doc.RootElement.GetProperty("bool");

            // ACT
            var obj = prop.ConvertToDynamicValue();

            // ASSERT
            Assert.IsType<bool>(obj);
            Assert.Equal(true, obj);
        }

        [Fact]
        public void JsonElementToDynamicValueWhenFalseShouldReturnBoolFalse()
        {
            // ARRANGE
            var doc = JsonDocument.Parse("{\"bool\": false}");
            var prop = doc.RootElement.GetProperty("bool");

            // ACT
            var obj = prop.ConvertToDynamicValue();

            // ASSERT
            Assert.IsType<bool>(obj);
            Assert.Equal(false, obj);
        }

        [Fact]
        public void JsonElementToDynamicValueWhenIntegerShouldReturnInteger()
        {
            // ARRANGE
            var doc = JsonDocument.Parse("{\"int\": 10}");
            var prop = doc.RootElement.GetProperty("int");
            const long expectedValue = 10;
            // ACT
            var obj = prop.ConvertToDynamicValue();

            // ASSERT
            Assert.IsType<long>(obj);
            Assert.Equal(expectedValue, obj);
        }

        [Fact]
        public void JsonElementToDynamicValueWhenDoubleShouldReturnDouble()
        {
            // ARRANGE
            var doc = JsonDocument.Parse("{\"int\": 10.5}");
            var prop = doc.RootElement.GetProperty("int");
            const double expectedValue = 10.5;
            // ACT
            var obj = prop.ConvertToDynamicValue();

            // ASSERT
            Assert.IsType<double>(obj);
            Assert.Equal(expectedValue, obj);
        }

        [Fact]
        public void JsonElementToDynamicValueWhenArrayShouldReturnArray()
        {
            // ARRANGE
            var doc = JsonDocument.Parse("{\"array\": [\"a\", \"b\", \"c\"]}");
            var prop = doc.RootElement.GetProperty("array");

            // ACT
            var obj = prop.ConvertToDynamicValue();
            var arr = obj as IEnumerable<object?>;

            // ASSERT
            Assert.NotNull(arr);
            Assert.Equal(3, arr?.Count());
        }

        [Fact]
        public void JsonElementToDynamicValueWhenObjectShouldReturnObject()
        {
            // ARRANGE
            var doc = JsonDocument.Parse("{\"str\": \"string\"}");

            // ACT
            var obj = doc.RootElement.ConvertToDynamicValue() as IDictionary<string, object?>;

            // ASSERT
            Assert.Equal("string", obj?["str"]);
        }

        [Fact]
        public void ParseDataTypeIntShouldReturnInt()
        {
            // ARRANGE
            const long expectedValue = 10;
            // ACT
            var longValue = StringParser.ParseDataType("10");
            // ASSERT
            Assert.IsType<long>(longValue);
            Assert.Equal(expectedValue, longValue);
        }

        [Fact]
        public void ParseDataTypeDoubleShouldReturnInt()
        {
            // ARRANGE
            const double expectedValue = 10.5;
            // ACT
            var doubleValue = StringParser.ParseDataType("10.5");
            // ASSERT
            Assert.IsType<double>(doubleValue);
            Assert.Equal(expectedValue, doubleValue);
        }

        [Fact]
        public void HassStateShouldConvertCorrectEntityState()
        {
            // ARRANGE

            var doc = JsonDocument.Parse("{\"str\": \"attr3value\"}");
            var prop = doc.RootElement.GetProperty("str");

            var hassState = new HassState
            {
                EntityId = "light.fake",
                Attributes = new Dictionary<string, object>
                {
                    ["attr1"] = "attr1value",
                    ["attr2"] = "attr2value",
                    ["attr3"] = prop
                },
                LastChanged = new DateTime(2000, 1, 1, 1, 1, 1),
                LastUpdated = new DateTime(2000, 1, 1, 1, 1, 2),
                Context = new HassContext
                {
                    Id = "idguid",
                    ParentId = "parentidguid",
                    UserId = "useridguid"
                }
            };

            // ACT
            var entityState = hassState.Map();

            // ASSERT
            Assert.Equal("light.fake", entityState.EntityId);
            Assert.Equal(new DateTime(2000, 1, 1, 1, 1, 1), entityState.LastChanged);
            Assert.Equal(new DateTime(2000, 1, 1, 1, 1, 2), entityState.LastUpdated);
            Assert.NotNull(entityState.Attribute);
            Assert.Equal("attr1value", entityState?.Attribute?.attr1);
            Assert.Equal("attr2value", entityState?.Attribute?.attr2);
            Assert.Equal("attr3value", entityState?.Attribute?.attr3);
            Assert.NotNull(entityState?.Context);
            Assert.Equal("idguid", hassState.Context.Id);
            Assert.Equal("parentidguid", hassState.Context.ParentId);
            Assert.Equal("useridguid", hassState.Context.UserId);
        }
    }
}