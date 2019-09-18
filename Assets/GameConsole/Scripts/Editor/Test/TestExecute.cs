#if UNIT_TEST
using NUnit.Framework;

namespace Saro.Console.Test
{
    public class TestExecute
    {
        TestCommand testCommand;

        [SetUp]
        public void Setup()
        {
            // setup
            testCommand = new TestCommand();
        }

        [Test]
        public void Execute_Normal_Command_No_Parameter()
        {
            // no parameter
            ConsoleCommand.ExecuteCommand(TestCommand.MethodPrefix_Normal);
        }

        [Test]
        public void Execute_Normal_Command_One_Parameter()
        {
            
            // single parameter
            ConsoleCommand.ExecuteCommand(TestCommand.MethodPrefix_Normal + TestCommand.StringParam + " " + TestCommand.refStr);
            ConsoleCommand.ExecuteCommand(TestCommand.MethodPrefix_Normal + TestCommand.IntParam + " " + TestCommand.refInt);
            ConsoleCommand.ExecuteCommand(TestCommand.MethodPrefix_Normal + TestCommand.FloatParam + " " + TestCommand.refFlogt);
            ConsoleCommand.ExecuteCommand(TestCommand.MethodPrefix_Normal + TestCommand.V2Param + " " + "(1,1)"/*TestCommand.refV2.ToString()*/);
            ConsoleCommand.ExecuteCommand(TestCommand.MethodPrefix_Normal + TestCommand.V3Param + " " + "1,1,1"/*TestCommand.refV3.ToString()*/);

        }

        [Test]
        public void Execute_Normal_Command_Two_Parameter()
        {
            // two parameters
            ConsoleCommand.ExecuteCommand(TestCommand.MethodPrefix_Normal + TestCommand.StringParam + TestCommand.IntParam + " 1 1");

        }

        [Test]
        public void Execute_Static_Command()
        {
            ConsoleCommand.ExecuteCommand(TestCommand.MethodPrefix_Static);
            ConsoleCommand.ExecuteCommand(TestCommand.MethodPrefix_Static + TestCommand.StringParam + " " + TestCommand.refStr);
            ConsoleCommand.ExecuteCommand(TestCommand.MethodPrefix_Static + TestCommand.IntParam + " " + TestCommand.refInt);
            ConsoleCommand.ExecuteCommand(TestCommand.MethodPrefix_Static + TestCommand.FloatParam + " " + TestCommand.refFlogt);
            ConsoleCommand.ExecuteCommand(TestCommand.MethodPrefix_Static + TestCommand.V2Param + " (1,1)" /*+ TestCommand.refV2.ToString()*/);
            ConsoleCommand.ExecuteCommand(TestCommand.MethodPrefix_Static + TestCommand.V3Param + " (1,1,1)" /*+ TestCommand.refV3.ToString()*/);
        }
    }
}

#endif