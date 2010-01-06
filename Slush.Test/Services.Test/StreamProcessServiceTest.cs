using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using NUnit.Framework.SyntaxHelpers;
using Slush.Services;

namespace Slush.Services.Test
{
    [TestFixture]
    public class StreamProcessServiceTest
    {
        #region Tests

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullTest()
        {
            StreamProcessService p = new StreamProcessService(null);
        }

        [Test]
        public void Begin_EventsTest()
        {
            StreamProcessService p = new StreamProcessService(Stream.Null);

            int count = 0;

            p.OnBeginStreamProcess +=
                delegate(BeginStreamProcessEventArgs e)
                {
                    count++;
                };

            p.OnEndStreamProcess +=
                delegate(EndStreamProcessEventArgs e)
                {
                    Assert.AreEqual(1, count++);
                };

            p.Begin();
            Assert.AreEqual(2, count);
        }

        [Test]
        public void Begin_BeginEventArgsTest()
        {
            Stream s = new MemoryStream();
            StreamProcessService p = new StreamProcessService(s);

            BeginStreamProcessEventArgs args = null;

            p.OnBeginStreamProcess +=
                delegate(BeginStreamProcessEventArgs e)
                {
                    args = e;
                };

            p.Begin();
            Assert.IsNotNull(args);
            Assert.AreEqual(s, args.Stream);
        }

        [Test]
        public void Begin_EndEventArgsTest()
        {
            Stream s = new MemoryStream();
            StreamProcessService p = new StreamProcessService(s);

            EndStreamProcessEventArgs args = null;

            p.OnEndStreamProcess +=
                delegate(EndStreamProcessEventArgs e)
                {
                    args = e;
                };

            p.Begin();
            Assert.IsNotNull(args);
        }

        [Test]
        public void Begin_NullEventsTest()
        {
            StreamProcessService p = new StreamProcessService(Stream.Null);

            // Don't throw
            p.Begin();
        }

        /// <summary>
        /// Tests that Begin swallows exceptions thrown from the
        /// BeginStreamProcess event handlers, and still fires
        /// the EndStreamProcess event
        /// </summary>
        [Test]
        public void Begin_ThrowBeginEventTest()
        {
            StreamProcessService p = new StreamProcessService(Stream.Null);

            bool calledEndEvent = false;
            p.OnBeginStreamProcess +=
                delegate(BeginStreamProcessEventArgs e)
                {
                    throw new Exception();
                };
            p.OnEndStreamProcess +=
                delegate(EndStreamProcessEventArgs e)
                {
                    calledEndEvent = true;
                };

            p.Begin();
            Assert.IsTrue(calledEndEvent);
        }

        [Test]
        public void Begin_ThrowEndEventTest()
        {
            StreamProcessService p = new StreamProcessService(Stream.Null);

            p.OnEndStreamProcess +=
                delegate(EndStreamProcessEventArgs e)
                {
                    throw new Exception();
                };

            // Don't throw
            p.Begin();
        }

        #endregion
    }
}
