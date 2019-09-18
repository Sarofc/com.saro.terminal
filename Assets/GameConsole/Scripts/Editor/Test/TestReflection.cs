#if UNIT_TEST

using NUnit.Framework;
using UnityEngine.TestTools;
namespace Saro.Console.Test
{
    public class TestReflection
    {

        TestCommand testCommand;

        [SetUp]
        public void Setup()
        {
            // setup
            testCommand = new TestCommand();
        }

        // ---------------------------------------------------------------------
        // static method
        // ---------------------------------------------------------------------
        [Test]
        public void Test_Reflection_Get_Static_Command()
        {
            Assert.IsTrue(ConsoleCommand.CommandLookup.ContainsKey(TestCommand.MethodPrefix_Static), "can't find TestStatic");
            Assert.IsTrue(ConsoleCommand.CommandLookup.ContainsKey(TestCommand.MethodPrefix_Static + TestCommand.IntParam), "type int error");
            Assert.IsTrue(ConsoleCommand.CommandLookup.ContainsKey(TestCommand.MethodPrefix_Static + TestCommand.FloatParam), "type float error");
            Assert.IsTrue(ConsoleCommand.CommandLookup.ContainsKey(TestCommand.MethodPrefix_Static + TestCommand.StringParam), "type string error");
            Assert.IsTrue(ConsoleCommand.CommandLookup.ContainsKey(TestCommand.MethodPrefix_Static + TestCommand.V2Param), "type vector2 error");
            Assert.IsTrue(ConsoleCommand.CommandLookup.ContainsKey(TestCommand.MethodPrefix_Static + TestCommand.V3Param), "type vector3 error");
        }

        // ---------------------------------------------------------------------
        // normal method
        // ---------------------------------------------------------------------
        [Test]
        public void Test_Reflection_Get_Normal_Command()
        {
            Assert.IsTrue(ConsoleCommand.CommandLookup.ContainsKey(TestCommand.MethodPrefix_Normal), "can't find TestStatic");
            Assert.IsTrue(ConsoleCommand.CommandLookup.ContainsKey(TestCommand.MethodPrefix_Normal + TestCommand.IntParam), "type int error");
            Assert.IsTrue(ConsoleCommand.CommandLookup.ContainsKey(TestCommand.MethodPrefix_Normal + TestCommand.FloatParam), "type float error");
            Assert.IsTrue(ConsoleCommand.CommandLookup.ContainsKey(TestCommand.MethodPrefix_Normal + TestCommand.StringParam), "type string error");
            Assert.IsTrue(ConsoleCommand.CommandLookup.ContainsKey(TestCommand.MethodPrefix_Normal + TestCommand.V2Param), "type vector2 error");
            Assert.IsTrue(ConsoleCommand.CommandLookup.ContainsKey(TestCommand.MethodPrefix_Normal + TestCommand.V3Param), "type vector3 error");
        }
    }
}

#endif