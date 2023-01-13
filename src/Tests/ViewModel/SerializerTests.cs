using Microsoft.VisualStudio.TestTools.UnitTesting;
using DotVVM.Framework.ViewModel.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using DotVVM.Framework.ViewModel;
using DotVVM.Framework.Compilation.Parser;
using DotVVM.Framework.Configuration;
using DotVVM.Framework.Testing;
using System.Text;
using DotVVM.Framework.Controls;

namespace DotVVM.Framework.Tests.ViewModel
{
    [TestClass]
    public class SerializerTests
    {
        static ViewModelJsonConverter CreateConverter(bool isPostback, JObject encryptedValues = null)
        {
            var config = DotvvmTestHelper.DefaultConfig;
            return new ViewModelJsonConverter(
                isPostback,
                config.ServiceProvider.GetRequiredService<IViewModelSerializationMapper>(),
                config.ServiceProvider,
                encryptedValues
            );
        }

        static string Serialize<T>(T viewModel, out JObject encryptedValues, bool isPostback = false)
        {
            encryptedValues = new JObject();
            var settings = DefaultSerializerSettingsProvider.Instance.Settings;
            var serializer = JsonSerializer.Create(settings);
            serializer.Converters.Add(CreateConverter(isPostback, encryptedValues));

            var output = new StringWriter();
            serializer.Serialize(output, viewModel);
            return output.ToString();
        }

        static T Deserialize<T>(string json, JObject encryptedValues = null)
        {
            var settings = DefaultSerializerSettingsProvider.Instance.Settings;
            var serializer = JsonSerializer.Create(settings);
            serializer.Converters.Add(CreateConverter(true, encryptedValues));

            return serializer.Deserialize<T>(new JsonTextReader(new StringReader(json)));
        }

        static T PopulateViewModel<T>(string json, T existingValue, JObject encryptedValues = null)
        {
            var settings = DefaultSerializerSettingsProvider.Instance.Settings;
            var serializer = JsonSerializer.Create(settings);
            var dotvvmConverter = CreateConverter(true, encryptedValues);
            serializer.Converters.Add(dotvvmConverter);
            return (T)dotvvmConverter.Populate(new JsonTextReader(new StringReader(json)), serializer, existingValue);
        }

        internal static (T vm, JObject json) SerializeAndDeserialize<T>(T viewModel, bool isPostback = false)
        {
            var json = Serialize<T>(viewModel, out var encryptedValues, isPostback);
            var viewModel2 = Deserialize<T>(json, encryptedValues);
            return (viewModel2, JObject.Parse(json));
        }

        [TestMethod]
        public void Support_NestedProtectedData()
        {
            var obj = new TestViewModelWithNestedProtectedData() {
                Root = new DataNode() {
                    Text = "Root",
                    EncryptedData = new DataNode() {
                        Text = "Encrypted1",
                        EncryptedData = new DataNode() {
                            Text = "Encrypted2",
                            EncryptedData = new DataNode() {
                                Text = "Encrypted3"
                            }
                        }
                    },
                    SignedData = new DataNode() {
                        Text = "Signed1",
                        SignedData = new DataNode() {
                            Text = "Signed2",
                            SignedData = new DataNode() {
                                Text = "Signed3"
                            }
                        }
                    }
                }
            };

            var serialized = Serialize(obj, out var encryptedValues, false);
            var deserialized = Deserialize<TestViewModelWithNestedProtectedData>(serialized, encryptedValues);
            Assert.AreEqual(serialized, Serialize(deserialized, out var _, false));
        }

        [TestMethod]
        public void Support_CollectionWithNestedProtectedData()
        {
            var obj = new TestViewModelWithCollectionOfNestedProtectedData() {
                Collection = new List<DataNode>() {
                    null,
                    new DataNode() { Text = "Element1", SignedData = new DataNode() { Text = "InnerSigned1" } },
                    null,
                    new DataNode() { Text = "Element2", SignedData = new DataNode() { Text = "InnerSigned2" } }
                }
            };

            var serialized = Serialize(obj, out var encryptedValues, false);
            var deserialized = Deserialize<TestViewModelWithCollectionOfNestedProtectedData>(serialized, encryptedValues);
            Assert.AreEqual(serialized, Serialize(deserialized, out var _, false));
        }

        [TestMethod]
        public void Support_CollectionOfCollectionsWithNestedProtectedData()
        {
            var obj = new TestViewModelWithCollectionOfNestedProtectedData() {
                Matrix = new List<List<DataNode>>() {
                    new List<DataNode>() {
                        new DataNode() { Text = "Element11", SignedData = new DataNode() { Text = "Signed11" } },
                        new DataNode() { Text = "Element12", SignedData = new DataNode() { Text = "Signed12" } },
                        new DataNode() { Text = "Element13", SignedData = new DataNode() { Text = "Signed13" } },
                    },
                    new List<DataNode>() {
                        new DataNode() { Text = "Element21", EncryptedData = new DataNode() { Text = "Encrypted21" } },
                        new DataNode() { Text = "Element22", EncryptedData = new DataNode() { Text = "Encrypted22" } },
                        new DataNode() { Text = "Element23", EncryptedData = new DataNode() { Text = "Encrypted23" } },
                    },
                    new List<DataNode>() {
                        new DataNode() { Text = "Element31", EncryptedData = new DataNode() { Text = "Encrypted31" } },
                        new DataNode() { Text = "Element32", EncryptedData = new DataNode() { Text = "Encrypted32" } },
                        new DataNode() { Text = "Element33", EncryptedData = new DataNode() { Text = "Encrypted33" } },
                    },
                }
            };

            var serialized = Serialize(obj, out var encryptedValues, false);
            var deserialized = Deserialize<TestViewModelWithCollectionOfNestedProtectedData>(serialized, encryptedValues);
            Assert.AreEqual(serialized, Serialize(deserialized, out var _, false));
        }

        [TestMethod]
        public void Support_NestedMixedProtectedData()
        {
            var obj = new TestViewModelWithNestedProtectedData() {
                Root = new DataNode() {
                    Text = "Root",
                    SignedData = new DataNode() {
                        Text = "Signed",
                        EncryptedData = new DataNode() {
                            Text = "Encrypted",
                        }
                    }
                }
            };

            var serialized = Serialize(obj, out var encryptedValues, false);
            var deserialized = Deserialize<TestViewModelWithNestedProtectedData>(serialized, encryptedValues);
            Assert.AreEqual(serialized, Serialize(deserialized, out var _, false));
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow(new byte[] { })]
        [DataRow(new byte[] { 1 })]
        [DataRow(new byte[] { 1, 2, 3 })]
        public void CustomJsonConverters_ByteArray(byte[] array)
        {
            using var stream = new MemoryStream();
            // Serialize array
            using (var writer = new JsonTextWriter(new StreamWriter(stream, Encoding.UTF8, 4096, leaveOpen: true)))
            {
                new DotvvmByteArrayConverter().WriteJson(writer, array, new JsonSerializer());
                writer.Flush();
            }

            // Deserialize array
            stream.Position = 0;
            byte[] deserialized;
            using (var reader = new JsonTextReader(new StreamReader(stream, Encoding.UTF8)))
            {
                while (reader.TokenType == JsonToken.None)
                    reader.Read();

                deserialized = (byte[])new DotvvmByteArrayConverter().ReadJson(reader, typeof(byte[]), null, new JsonSerializer());
            }

            CollectionAssert.AreEqual(array, deserialized);
        }

        [TestMethod]
        public void ViewModelWithByteArray()
        {
            var obj = new TestViewModelWithByteArray() {
                Bytes = new byte[] { 1, 2, 3 }
            };
            var (obj2, json) = SerializeAndDeserialize(obj);

            CollectionAssert.AreEqual(obj.Bytes, obj2.Bytes);
            Assert.AreEqual(1, (int)json["Bytes"][0]);
            Assert.AreEqual(2, (int)json["Bytes"][1]);
            Assert.AreEqual(3, (int)json["Bytes"][2]);

        }

        [TestMethod]
        public void SupportTuples()
        {
            var obj = new TestViewModelWithTuples() {
                P1 = new Tuple<int, int, int, int>(9, 8, 7, 6),
                P2 = (5, 6, 7, 8),
                P3 = {
                    new KeyValuePair<int, int>(3, 4),
                    new KeyValuePair<int, int>(5, 6)
                },
                P4 = (
                    6,
                    new TestViewModelWithBind {
                        P1 = "X",
                        P2 = "Y",
                        ServerToClient = "Z",
                        ClientToServer = "Z"
                    }
                )
            };
            var obj2 = SerializeAndDeserialize(obj, isPostback: true).vm;

            Assert.AreEqual(obj.P1, obj2.P1);
            Assert.AreEqual(obj.P2, obj2.P2);
            Assert.IsTrue(obj.P3.SequenceEqual(obj2.P3));
            Assert.AreEqual(obj.P4.a, obj2.P4.a);
            Assert.AreEqual(obj.P4.b.P1, obj2.P4.b.P1);
            Assert.AreEqual(obj.P4.b.P2, obj2.P4.b.P2);
            Assert.AreEqual("default", obj2.P4.b.ServerToClient);
            Assert.AreEqual("default", obj2.P4.b.ClientToServer);
        }

        [TestMethod]
        public void DoesNotCloneSettableRecord()
        {
            var obj = new TestViewModelWithRecords() {
                Primitive = 10
            };
            var json = Serialize(obj, out var ev, false);
            var obj2 = new TestViewModelWithRecords() { Primitive = 100 };
            var obj3 = PopulateViewModel(json, obj2, ev);
            Assert.AreEqual(10, obj3.Primitive);
            Assert.IsTrue(ReferenceEquals(obj2, obj3), "The deserialized object TestViewModelWithRecords is not referenced equal to the existingValue");
            Assert.AreEqual(10, obj2.Primitive);
        }

        [TestMethod]
        public void ClonesInitOnlyClass()
        {
            var obj = new TestInitOnlyClass() { X = 10, Y = "A" };
            var json = Serialize(obj, out var ev, false);
            var obj2 = new TestInitOnlyClass() { X = 20, Y = "B" };
            var obj3 = PopulateViewModel(json, obj2, ev);
            Assert.AreEqual(10, obj3.X);
            Assert.AreEqual("A", obj3.Y);
            Assert.AreEqual(20, obj2.X, "The deserializer didn't clone TestInitOnlyClass and used the init-only setter at runtime.");
            Assert.IsFalse(ReferenceEquals(obj2, obj3), "The deserializer didn't clone TestInitOnlyClass");
            Assert.AreEqual("B", obj2.Y, "The deserializer used TestInitOnlyClass.Y setter at runtime, but then returned another instance.");
        }
        [TestMethod]
        public void ClonesInitOnlyRecord()
        {
            var obj = new TestInitOnlyRecord() { X = 10, Y = "A" };
            var json = Serialize(obj, out var ev, false);
            var obj2 = new TestInitOnlyRecord() { X = 20, Y = "B" };
            var obj3 = PopulateViewModel(json, obj2, ev);
            Assert.AreEqual(10, obj3.X);
            Assert.AreEqual("A", obj3.Y);
            Assert.AreEqual(20, obj2.X, "The deserializer didn't clone TestInitOnlyRecord and used the init-only setter at runtime.");
            Assert.IsFalse(ReferenceEquals(obj2, obj3), "The deserializer didn't clone TestInitOnlyRecord");
            Assert.AreEqual("B", obj2.Y, "The deserializer used TestInitOnlyRecord.Y setter at runtime, but then returned another instance.");
        }


        [TestMethod]
        public void SupportBasicRecord()
        {
            var obj = new TestViewModelWithRecords() {
                Primitive = 10
            };
            var (obj2, json) = SerializeAndDeserialize(obj);

            Assert.AreEqual(obj.Primitive, obj2.Primitive);
            Assert.AreEqual(obj.A, obj2.A);
            Assert.AreEqual(obj.Primitive, (int)json["Primitive"]);
        }
        [TestMethod]
        public void SupportConstructorRecord()
        {
            var obj = new TestViewModelWithRecords() {
                A = new (1, "ahoj")
            };
            var (obj2, json) = SerializeAndDeserialize(obj);

            Assert.AreEqual(obj.A, obj2.A);
            Assert.AreEqual(obj.A.X, obj2.A.X);
            Assert.AreEqual(1, (int)json["A"]["X"]);
            Assert.AreEqual("ahoj", (string)json["A"]["Y"]);
        }
        [TestMethod]
        public void SupportConstructorRecordWithProperty()
        {
            var obj = new TestViewModelWithRecords() {
                B = new (1, "ahoj") { Z = "zz" }
            };
            var (obj2, json) = SerializeAndDeserialize(obj);

            Assert.AreEqual(obj.B, obj2.B);
            Assert.AreEqual(1, obj2.B.X);
            Assert.AreEqual("zz", obj2.B.Z);
            Assert.AreEqual("zz", (string)json["B"]["Z"]);
            Assert.AreEqual("ahoj", (string)json["B"]["Y"]);
        }
        [TestMethod]
        public void SupportStructRecord()
        {
            var obj = new TestViewModelWithRecords() {
                C = new (1, "ahoj")
            };
            var (obj2, json) = SerializeAndDeserialize(obj);

            Assert.AreEqual(obj.C, obj2.C);
            Assert.AreEqual(1, obj2.C.X);
            Assert.AreEqual(1, (int)json["C"]["X"]);
            Assert.AreEqual("ahoj", (string)json["C"]["Y"]);
        }
        [TestMethod]
        public void SupportMutableStruct()
        {
            var obj = new TestViewModelWithRecords() {
                D = new() { X = 1, Y = "ahoj" }
            };
            var (obj2, json) = SerializeAndDeserialize(obj);

            Assert.AreEqual(1, (int)json["D"]["X"]);
            Assert.AreEqual("ahoj", (string)json["D"]["Y"]);
            Assert.AreEqual(obj.D.Y, obj2.D.Y);
            Assert.AreEqual(obj.D.X, obj2.D.X);
            Assert.AreEqual(1, obj2.D.X);
        }

        [TestMethod]
        public void SupportRecordWithGridDataSet()
        {
            var obj = new TestViewModelWithRecords() {
                E = new(new GridViewDataSet<string>() { Items = new List<string> { "a", "b", "c" } })
            };
            var (obj2, json) = SerializeAndDeserialize(obj);

            Assert.AreEqual(0, (int)json["E"]["Dataset"]["PagingOptions"]["PageIndex"]);
            CollectionAssert.AreEqual(obj.E.Dataset.Items.ToArray(), obj2.E.Dataset.Items.ToArray());
            Assert.AreEqual(obj.E.Dataset.PagingOptions.PageIndex, obj2.E.Dataset.PagingOptions.PageIndex);
        }

        [TestMethod]
        public void SupportViewModelWithGridDataSet()
        {
            var obj = new TestViewModelWithDataset() {
                NoInit = new GridViewDataSet<string>() { Items = new List<string> { "a", "b", "c" } },
                Preinitialized = { Items = new List<string> { "d", "e", "f" }, PagingOptions = { PageSize = 1 } }
            };
            var (obj2, json) = SerializeAndDeserialize(obj);

            Assert.AreEqual(1, (int)json["Preinitialized"]["PagingOptions"]["PageSize"]);
            CollectionAssert.AreEqual(obj.NoInit.Items.ToArray(), obj2.NoInit.Items.ToArray());
            CollectionAssert.AreEqual(obj.Preinitialized.Items.ToArray(), obj2.Preinitialized.Items.ToArray());
            Assert.AreEqual(obj.Preinitialized.PagingOptions.PageSize, obj2.Preinitialized.PagingOptions.PageSize);
            Assert.AreEqual("AAA", obj.Preinitialized.SortingOptions.SortExpression);
            Assert.AreEqual("AAA", obj2.Preinitialized.SortingOptions.SortExpression);
        }

        [TestMethod]
        public void SupportConstructorInjection()
        {
            var service = DotvvmTestHelper.DefaultConfig.ServiceProvider.GetRequiredService<DotvvmTestHelper.ITestSingletonService>();
            var obj = new ViewModelWithService("test", service);
            var (obj2, json) = SerializeAndDeserialize(obj);

            Assert.AreEqual(obj.Property1, obj2.Property1);
            Assert.AreEqual(obj.GetService(), obj2.GetService());
            Assert.AreEqual(obj.Property1, (string)json["Property1"]);
            Assert.IsNull(json.Property("Service"));
        }

        [TestMethod]
        public void SupportsSignedDictionary()
        {
            var obj = new TestViewModelWithSignedDictionary() {
                SignedDictionary = {
                    ["a"] = "x",
                    ["b"] = "y"
                }
            };
            var (obj2, json) = SerializeAndDeserialize(obj);

            CollectionAssert.Contains(obj2.SignedDictionary, new KeyValuePair<string, string>("a", "x"));
            CollectionAssert.Contains(obj2.SignedDictionary, new KeyValuePair<string, string>("b", "y"));
            Assert.AreEqual(obj.SignedDictionary.Count, obj2.SignedDictionary.Count);
            Assert.IsNotNull(json.Property("SignedDictionary"));
        }

        [TestMethod]
        public void DoesNotTouchIrrelevantGetters()
        {
            var obj = new ParentClassWithBrokenGetters() {
                NestedVM = {
                    SomeNestedVM = new TestViewModelWithRecords {
                        Primitive = 100
                    }
                }
            };
            var (obj2, json) = SerializeAndDeserialize(obj);

            Assert.AreEqual(obj.NestedVM.SomeNestedVM.Primitive, obj2.NestedVM.SomeNestedVM.Primitive);
            Assert.AreEqual(obj.NestedVM.BrokenGetter, obj2.NestedVM.BrokenGetter);
        }
        public class ViewModelWithService
        {
            public string Property1 { get; }
            private DotvvmTestHelper.ITestSingletonService Service { get; }
            public DotvvmTestHelper.ITestSingletonService GetService() => Service;

            [JsonConstructor]
            public ViewModelWithService(string property1, DotvvmTestHelper.ITestSingletonService service)
            {
                Property1 = property1;
                Service = service;
            }
        }

        [TestMethod]
        public void FailsReasonablyOnUnmatchedConstructorProperty1()
        {
            var obj = new ViewModelWithUnmatchedConstuctorProperty1("test");
            var x = Assert.ThrowsException<Exception>(() => SerializeAndDeserialize(obj));
            Assert.AreEqual("Can not deserialize DotVVM.Framework.Tests.ViewModel.SerializerTests.ViewModelWithUnmatchedConstuctorProperty1, constructor parameter x is not mapped to any property.", x.Message);
        }

        public class ViewModelWithUnmatchedConstuctorProperty1
        {
            [JsonConstructor]
            public ViewModelWithUnmatchedConstuctorProperty1(string x) { }
        }

        [TestMethod]
        public void FailsReasonablyOnUnmatchedConstructorProperty2()
        {
            var obj = new ViewModelWithUnmatchedConstuctorProperty2(null!);
            var x = Assert.ThrowsException<Exception>(() => SerializeAndDeserialize(obj));
            Assert.AreEqual("Can not deserialize DotVVM.Framework.Tests.ViewModel.SerializerTests.ViewModelWithUnmatchedConstuctorProperty2, constructor parameter x is not mapped to any property and service TestViewModelWithByteArray was not found in ServiceProvider.", x.Message);
        }

        public class ViewModelWithUnmatchedConstuctorProperty2
        {
            [JsonConstructor]
            public ViewModelWithUnmatchedConstuctorProperty2(TestViewModelWithByteArray x) { }
        }

    }

    public class DataNode
    {
        [Protect(ProtectMode.EncryptData)]
        public DataNode EncryptedData { get; set; }

        [Protect(ProtectMode.SignData)]
        public DataNode SignedData { get; set; }

        [Protect(ProtectMode.None)]
        public string Text { get; set; }
    }

    public class TestViewModelWithNestedProtectedData
    {
        public DataNode Root { get; set; }
    }

    public class TestViewModelWithByteArray
    {
        public byte[] Bytes { get; set; }
    }

    public class TestViewModelWithCollectionOfNestedProtectedData
    {
        [Protect(ProtectMode.SignData)]
        public List<DataNode> Collection { get; set; }

        [Protect(ProtectMode.SignData)]
        public List<List<DataNode>> Matrix { get; set; }
    }

    public class TestViewModelWithDataset
    {
        public GridViewDataSet<string> Preinitialized { get; set; } = new GridViewDataSet<string> { SortingOptions = { SortExpression = "AAA" } };
        public GridViewDataSet<string> NoInit { get; set; }
    }

    public class TestViewModelWithTuples
    {
        public Tuple<int, int, int, int> P1 { get; set; }
        public (int a, int b, int c, int d) P2 { get; set; } = (1, 2, 3, 4);
        public List<KeyValuePair<int, int>> P3 { get; set; } = new List<KeyValuePair<int, int>>();
        public (int a, TestViewModelWithBind b) P4 { get; set; } = (1, new TestViewModelWithBind());
    }

    public class TestViewModelWithBind
    {
        [Bind(Name = "property ONE")]
        public string P1 { get; set; } = "value 1";
        [JsonProperty("property TWO")]
        public string P2 { get; set; } = "value 2";
        [Bind(Direction.ClientToServer)]
        public string ClientToServer { get; set; } = "default";
        [Bind(Direction.ServerToClient)]
        public string ServerToClient { get; set; } = "default";
    }

    public record TestViewModelWithRecords
    {
        public ImmutableRecord A { get; set; }
        public RecordWithAdditionalField B { get; set; }
        public StructRecord C { get; set; }
        public MutableStruct D { get; set; }
        public WithDataset E { get; set; }

        public int Primitive { get; set; }

        public record ImmutableRecord(int X, string Y);

        public record RecordWithAdditionalField(int X, string Y)
        {
            public string Z { get; set; }
        }


        public record struct StructRecord(int X, string Y);

        public struct MutableStruct
        {
            public int X { get; set; }
            public string Y { get; set; }
        }

        public record WithDataset(GridViewDataSet<string> Dataset);
    }

    public class TestInitOnlyClass
    {
        public int X { get; init; }
        public string Y { get; set; }
    }
    public class TestInitOnlyRecord
    {
        public int X { get; init; }
        public string Y { get; set; }
    }

    public class TestViewModelWithSignedDictionary
    {
        [Protect(ProtectMode.SignData)]
        public Dictionary<string, string> SignedDictionary { get; set; } = new();
    }

    // we had a bug that the deserializer touched all properties before deserializing, some of these could crash on NRE because they were computing something
    public class ClassWithBrokenGetters
    {
        public TestViewModelWithRecords SomeNestedVM { get; set; } = null;

        public bool BrokenGetter => SomeNestedVM.Primitive > 10;
    }

    public class ParentClassWithBrokenGetters
    {
        public ClassWithBrokenGetters NestedVM { get; set; } = new ClassWithBrokenGetters();
    }
}
