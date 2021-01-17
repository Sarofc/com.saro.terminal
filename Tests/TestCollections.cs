#if true

using NUnit.Framework;
using System;

namespace Saro.Terminal.Test
{
    public class TestCollections
    {
        LitRingBuffer<int> buffer;
        int capacity = 5;

        [SetUp]
        public void Setup()
        {
            buffer = new LitRingBuffer<int>(capacity);
        }

        [Test]
        public void Add_Not_Full()
        {
            buffer.AddTail(0);
            buffer.AddTail(1);
            buffer.AddTail(2);
            buffer.AddTail(3);

            Assert.AreEqual(buffer.Length, 4);

            for (int i = 0; i < buffer.Length; i++)
            {
                //UnityEngine.Debug.Log(buffer[i]);
                Assert.AreEqual(i, buffer[i]);
            }
        }

        [Test]
        public void Add_Full()
        {
            buffer.AddTail(0);
            buffer.AddTail(1);
            buffer.AddTail(2);
            buffer.AddTail(3);
            buffer.AddTail(4);

            Assert.AreEqual(buffer.Length, capacity);

            for (int i = 0; i < buffer.Length; i++)
            {
                Assert.AreEqual(i, buffer[i]);
            }
        }

        [Test]
        public void Add_Not_Contains_When_Full()
        {
            buffer.AddTail(0);
            buffer.AddTail(1);
            buffer.AddTail(2);
            buffer.AddTail(3);
            buffer.AddTail(4);

            buffer.AddTail(5);

            Assert.AreEqual(buffer.Length, capacity);

            for (int i = 0; i < buffer.Length; i++)
            {
                UnityEngine.Debug.Log(buffer[i]);
            }

            Assert.AreEqual(buffer[0], 1);
            Assert.AreEqual(buffer[1], 2);
            Assert.AreEqual(buffer[2], 3);
            Assert.AreEqual(buffer[3], 4);
            Assert.AreEqual(buffer[4], 5);
        }

        [Test]
        public void Add_Contains_Front_When_Not_Full()
        {
            buffer.AddTail(0);
            buffer.AddTail(1);
            buffer.AddTail(2);
            buffer.AddTail(3);

            buffer.AddTail(0);

            for (int i = 0; i < buffer.Length; i++)
            {
                UnityEngine.Debug.Log(buffer[i]);
            }

            Assert.AreEqual(buffer.Length, 4);


            Assert.AreEqual(buffer[0], 1);
            Assert.AreEqual(buffer[1], 2);
            Assert.AreEqual(buffer[2], 3);
            Assert.AreEqual(buffer[3], 0);
        }

        [Test]
        public void Add_Contains_Tail_When_Not_Full()
        {
            buffer.AddTail(0);
            buffer.AddTail(1);
            buffer.AddTail(2);
            buffer.AddTail(3);

            buffer.AddTail(3);

            Assert.AreEqual(buffer.Length, 4);

            for (int i = 0; i < buffer.Length; i++)
            {
                UnityEngine.Debug.Log(buffer[i]);
            }

            Assert.AreEqual(buffer[0], 0);
            Assert.AreEqual(buffer[1], 1);
            Assert.AreEqual(buffer[2], 2);
            Assert.AreEqual(buffer[3], 3);
        }

        
       

        [Test]
        public void Add_Contains_Tail_When_Full()
        {
            buffer.AddTail(0);
            buffer.AddTail(1);
            buffer.AddTail(2);
            buffer.AddTail(3);
            buffer.AddTail(4);

            buffer.AddTail(4);

            Assert.AreEqual(buffer.Length, capacity);

            for (int i = 0; i < buffer.Length; i++)
            {
                UnityEngine.Debug.Log(buffer[i]);
            }

            Assert.AreEqual(buffer[0], 0);
            Assert.AreEqual(buffer[1], 1);
            Assert.AreEqual(buffer[2], 2);
            Assert.AreEqual(buffer[3], 3);
            Assert.AreEqual(buffer[4], 4);
        }

        [Test]
        public void Add_Contains_Front_When_Full()
        {
            buffer.AddTail(0);
            buffer.AddTail(1);
            buffer.AddTail(2);
            buffer.AddTail(3);
            buffer.AddTail(4);

            buffer.AddTail(0);

            Assert.AreEqual(buffer.Length, capacity);

            for (int i = 0; i < buffer.Length; i++)
            {
                UnityEngine.Debug.Log(buffer[i]);
            }

            Assert.AreEqual(buffer[0], 1);
            Assert.AreEqual(buffer[1], 2);
            Assert.AreEqual(buffer[2], 3);
            Assert.AreEqual(buffer[3], 4);
            Assert.AreEqual(buffer[4], 0);
        }

        [Test]
        public void Add_Contains_Mid_When_Full()
        {
            buffer.AddTail(0);
            buffer.AddTail(1);
            buffer.AddTail(2);
            buffer.AddTail(3);
            buffer.AddTail(4);

            buffer.AddTail(2);

            Assert.AreEqual(buffer.Length, capacity);

            for (int i = 0; i < buffer.Length; i++)
            {
                UnityEngine.Debug.Log(buffer[i]);
            }

            Assert.AreEqual(buffer[0], 0);
            Assert.AreEqual(buffer[1], 1);
            Assert.AreEqual(buffer[2], 3);
            Assert.AreEqual(buffer[3], 4);
            Assert.AreEqual(buffer[4], 2);
        }




        [Test]
        public void Add_Contains_Four_Times_When_Full()
        {
            buffer.AddTail(0);
            buffer.AddTail(1);
            buffer.AddTail(2);
            buffer.AddTail(3);
            buffer.AddTail(4);

            buffer.AddTail(2);
            buffer.AddTail(1);
            buffer.AddTail(3);

            Assert.AreEqual(buffer.Length, capacity);

            for (int i = 0; i < buffer.Length; i++)
            {
                UnityEngine.Debug.Log(buffer[i]);
            }

            Assert.AreEqual(buffer[0], 0);
            Assert.AreEqual(buffer[1], 4);
            Assert.AreEqual(buffer[2], 2);
            Assert.AreEqual(buffer[3], 1);
            Assert.AreEqual(buffer[4], 3);
        }

        [Test]
        public void Add_Not_Contains_Four_Times_When_Full()
        {
            buffer.AddTail(0);
            buffer.AddTail(1);
            buffer.AddTail(2);
            buffer.AddTail(3);
            buffer.AddTail(4);

            buffer.AddTail(5);
            buffer.AddTail(6);
            buffer.AddTail(7);

            Assert.AreEqual(buffer.Length, capacity);

            for (int i = 0; i < buffer.Length; i++)
            {
                UnityEngine.Debug.Log(buffer[i]);
            }

            Assert.AreEqual(buffer[0], 3);
            Assert.AreEqual(buffer[1], 4);
            Assert.AreEqual(buffer[2], 5);
            Assert.AreEqual(buffer[3], 6);
            Assert.AreEqual(buffer[4], 7);
        }

        [Test]
        public void Add_Four_Times_When_Full()
        {
            buffer.AddTail(0);
            buffer.AddTail(1);
            buffer.AddTail(2);
            buffer.AddTail(3);
            buffer.AddTail(4);

            buffer.AddTail(3);
            buffer.AddTail(6);//ovrride 0
            buffer.AddTail(1);

            Assert.AreEqual(buffer.Length, capacity);

            for (int i = 0; i < buffer.Length; i++)
            {
                UnityEngine.Debug.Log(buffer[i]);
            }

            Assert.AreEqual(buffer[0], 2);
            Assert.AreEqual(buffer[1], 4);
            Assert.AreEqual(buffer[2], 3);
            Assert.AreEqual(buffer[3], 6);
            Assert.AreEqual(buffer[4], 1);
        }
    }
}

#endif