#if UNIT_TEST
using UnityEngine;
using UnityEngine.Assertions;

namespace Saro.Console.Test
{
    public class TestCommand
    {
        public const string MethodPrefix_Normal = "TestNormal";
        public const string MethodPrefix_Static = "TestStatic";

        public const string IntParam = "_int";
        public const string FloatParam = "_float";
        public const string StringParam = "_string";
        public const string V2Param = "_vector2";
        public const string V3Param = "_vector3";

        public static int refInt = 1;
        public static float refFlogt = 1.1f;
        public static string refStr = "1";
        public static Vector2 refV2 = new Vector2(1, 1);
        public static Vector3 refV3 = new Vector3(1, 1, 1);

        public TestCommand()
        {
            Assert.IsNotNull(this, "instance shouldn't be null");
            ConsoleCommand.AddCommandInstance(typeof(TestCommand), this);
        }

        [Command(MethodPrefix_Normal, "this is a test command")]
        public void PulicNormalMethod()
        {
            // check this method has been called
            Assert.IsTrue(true);
        }

        [Command(MethodPrefix_Normal + IntParam, "this is a test command")]
        public void PulicNormalMethod(int value)
        {
            Assert.IsTrue(value == refInt);
        }

        [Command(MethodPrefix_Normal + FloatParam, "this is a test command")]
        public void PulicNormalMethod(float value)
        {
            Assert.IsTrue(value == refFlogt);
        }

        [Command(MethodPrefix_Normal + V2Param, "this is a test command")]
        public void PulicNormalMethod(Vector2 value)
        {
            Assert.IsTrue(value == refV2);
        }

        [Command(MethodPrefix_Normal + V3Param, "this is a test command")]
        public void PulicNormalMethod(Vector3 value)
        {
            Assert.IsTrue(value == refV3);
        }

        [Command(MethodPrefix_Normal + StringParam, "this is a test command")]
        public void PulicNormalMethod(string value)
        {
            Assert.IsTrue(value == refStr);
        }

        // -------------------------------------------------------------------

        [Command(MethodPrefix_Static, "this is a test command")]
        public static void PulicStaticMethod()
        {
            Assert.IsTrue(true);
        }

        [Command(MethodPrefix_Static + IntParam, "this is a test command")]
        public static void PulicStaticMethod(int value)
        {
            Assert.IsTrue(value == refInt);
        }

        [Command(MethodPrefix_Static + FloatParam, "this is a test command")]
        public static void PulicStaticMethod(float value)
        {
            Assert.IsTrue(value == refFlogt);
        }

        [Command(MethodPrefix_Static + V2Param, "this is a test command")]
        public static void PulicStaticMethod(Vector2 value)
        {
            Assert.IsTrue(value == refV2);
        }

        [Command(MethodPrefix_Static + V3Param, "this is a test command")]
        public static void PulicStaticMethod(Vector3 value)
        {
            Assert.IsTrue(value == refV3);
        }

        [Command(MethodPrefix_Static + StringParam, "this is a test command")]
        public static void PulicStaticMethod(string value)
        {
            Assert.IsTrue(value == refStr);
        }

        // -----------------------------------------------------------------------
        [Command(MethodPrefix_Normal + StringParam + IntParam, "this is a test command")]
        public static void PulicNormalMethod(string str, int number)
        {
            Assert.IsTrue(str == refStr);
            Assert.IsTrue(number == refInt);
        }
    }
}
#endif