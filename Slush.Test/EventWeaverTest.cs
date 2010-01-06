using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Slush;

namespace Slush.Test
{
    [TestFixture]
    public class EventWeaverTest
    {
        #region Private Delegates

        delegate void TestEventHandler1(int i);
        delegate void TestEventHandler2(string s);

        #endregion


        #region Private Types

        class Service1
        {
            public event EventHandler Event;

            public void Trigger()
            {
                if (null != Event) Event(this, null);
            }
        }

        class Service2
        {
            public int EventCount = 0;

            public void EventHandler(object o, EventArgs e)
            {
                ++EventCount;
            }
        }

        class Service3
        {
            public int EventCount = 0;

            public event TestEventHandler1 Event1;
            public event TestEventHandler2 Event2;

            public void EventHandler(object o, EventArgs e)
            {
                ++EventCount;

                if (null != Event1) Event1(0);
                if (null != Event2) Event2(null);
            }
        }

        class Service4
        {
            public int Event1Count = 0;
            public int Event2Count = 0;

            public void Event1Handler(int i)
            {
                ++Event1Count;
            }

            public void Event2Handler(string s)
            {
                ++Event2Count;
            }
        }

        #endregion


        #region Members

        private EventWeaver ew;

        #endregion


        #region Housekeeping

        [SetUp]
        public void SetUp()
        {
            ew = new EventWeaver();
        }

        #endregion


        #region Tests

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Add_NullTest()
        {
            ew.Add(null);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void Add_AlreadyExistsTest()
        {
            Int16 i = new Int16();
            try
            {
                ew.Add(i);
            }
            catch
            {
                Assert.Fail();
            }
            ew.Add(i);
        }
        
        [Test]
        public void Add_WeaveTest1()
        {
            Service1 s1 = new Service1();
            Service2 s2 = new Service2();

            ew.Add(s1);
            ew.Add(s2);

            s1.Trigger();
            Assert.AreEqual(1, s2.EventCount);
        }

        [Test]
        public void Add_WeaveTest2()
        {
            Service1 s1 = new Service1();
            Service2 s2 = new Service2();

            // Add in wrong order won't weave
            ew.Add(s2);
            ew.Add(s1);

            s1.Trigger();
            Assert.AreEqual(0, s2.EventCount);
        }

        [Test]
        public void Add_WeaveTest3()
        {
            Service1 s1 = new Service1();
            Service3 s3 = new Service3();
            Service4 s4 = new Service4();

            ew.Add(s1);
            ew.Add(s3);
            ew.Add(s4);

            s1.Trigger();

            Assert.AreEqual(1, s3.EventCount);
            Assert.AreEqual(1, s4.Event1Count);
            Assert.AreEqual(1, s4.Event2Count);
        }

        [Test]
        public void Add_WeaveDelegateTest()
        {
            int called = 0;

            Service1 s1 = new Service1();

            ew.Add(s1);
            ew.Add(
            new EventHandler(delegate(object o, EventArgs e)
            {
                ++called;
            }
            ));

            s1.Trigger();

            Assert.AreEqual(1, called);
        }

        #endregion
    }
}
