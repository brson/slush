using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using NUnit.Framework.SyntaxHelpers;
using Slush.DomainObjects.Mp3;
using Slush.Services.Mp3;
using Slush.DomainObjects.Mp3.Test;

namespace Slush.Services.Mp3.Test
{
    [TestFixture]
    public class Mp3CrawlerServiceTest
    {
        #region Private Members

        private Mp3CrawlerService crawler = null;
        private Mp3StreamBuilder builder = null;

        #endregion


        #region Housekeeping

        [SetUp]
        public void SetUp()
        {
            crawler = new Mp3CrawlerService();
            builder = new Mp3StreamBuilder();
        }

        #endregion


        #region Tests

        [Test]
        public void BeginStreamProcessEventHandler_ShouldNotFindAnyRegions_IfArgumentIsNull()
        {
            crawler.OnFoundJunkRegion +=
                delegate(FoundJunkRegionEventArgs e)
                {
                    Assert.Fail();
                };
            crawler.OnFoundMp3Frame +=
                delegate(FoundMp3FrameEventArgs e)
                {
                    Assert.Fail();
                };
            crawler.BeginStreamProcessEventHandler(null);
        }

        [Test]
        public void BeginStreamProcessEventHandler_ShouldNotThrow_IfArgumentIsNull()
        {
            crawler.BeginStreamProcessEventHandler(null);
        }

        [Test]
        public void BeginStreamProcessEventHandler_ShouldNotThrow_IfOnFoundJunkRegionIsNull()
        {
            builder.AddJunk(10);
            crawler.BeginStreamProcessEventHandler(
                new BeginStreamProcessEventArgs(
                    builder.BuildStream()));
        }

        [Test]
        public void BeginStreamProcessEventHandler_ShouldNotThrow_IfOnFoundMp3FrameIsNull()
        {
            builder.AddFrame(MpegVersion.V25, MpegLayer.Layer1, true, 0, 0);
            builder.AddFrame(MpegVersion.V1, MpegLayer.Layer1, true, 0, 0);
            crawler.BeginStreamProcessEventHandler(
                new BeginStreamProcessEventArgs(
                    builder.BuildStream()));
        }

        // Stream correctness is tested by Mp3StreamReaderTest
        [Test]
        public void BeginStreamProcessEventHandler_ShouldEnumerateAllRegions()
        {
            Type[] regionOrder = 
            {
                typeof ( Mp3Frame ),
                typeof ( Mp3Frame ),
                typeof ( Mp3Frame ),
                typeof ( JunkRegion ),
                typeof ( Mp3Frame ),
                typeof ( Mp3Frame )
            };

            builder.AddFrame(MpegVersion.V25, MpegLayer.Layer1, true, 0, 0);
            builder.AddFrame(MpegVersion.V25, MpegLayer.Layer1, true, 0, 0);
            builder.AddFrame(MpegVersion.V25, MpegLayer.Layer1, true, 0, 0);
            builder.AddJunk(100);
            builder.AddFrame(MpegVersion.V25, MpegLayer.Layer1, true, 0, 0);
            builder.AddFrame(MpegVersion.V25, MpegLayer.Layer1, true, 0, 0);

            int regionTypeIndex = 0;
            crawler.OnFoundJunkRegion +=
                delegate(FoundJunkRegionEventArgs e)
                {
                    Assert.That(regionOrder[regionTypeIndex++], Is.EqualTo(typeof(JunkRegion)));
                };
            crawler.OnFoundMp3Frame +=
                delegate(FoundMp3FrameEventArgs e)
                {
                    Assert.That(regionOrder[regionTypeIndex++], Is.EqualTo(typeof(Mp3Frame)));
                };

            crawler.BeginStreamProcessEventHandler(
                new BeginStreamProcessEventArgs(builder.BuildStream()));
            Assert.That(regionTypeIndex, Is.EqualTo(regionOrder.Length));
        }

        [Test]
        public void BeginStreamProcessEventHandler_ShouldContinue_IfOnFoundJunkRegionThrows()
        {
            crawler.OnFoundJunkRegion +=
                delegate(FoundJunkRegionEventArgs e)
                {
                    throw new Exception();
                };
            bool called = false;
            crawler.OnFoundMp3Frame +=
                delegate(FoundMp3FrameEventArgs e)
                {
                    called = true;
                };

            builder.AddJunk(10);
            builder.AddFrame();
            builder.AddFrame();
            crawler.BeginStreamProcessEventHandler(
                new BeginStreamProcessEventArgs(builder.BuildStream()));
            Assert.That(called, Is.True);
        }

        [Test]
        public void BeginStreamProcessEventHandler_ShouldContinue_IfOnFoundMp3FrameThrows()
        {
            crawler.OnFoundMp3Frame +=
                delegate(FoundMp3FrameEventArgs e)
                {
                    throw new Exception();
                };
            bool called = false;
            crawler.OnFoundJunkRegion +=
                delegate(FoundJunkRegionEventArgs e)
                {
                    called = true;
                };

            builder.AddFrame(MpegVersion.V25, MpegLayer.Layer1, true, 0, 0);
            builder.AddFrame(MpegVersion.V25, MpegLayer.Layer1, true, 0, 0);
            builder.AddJunk(200);
            crawler.BeginStreamProcessEventHandler(
                new BeginStreamProcessEventArgs(builder.BuildStream()));
            Assert.That(called, Is.True);
        }

        [Test]
        public void OnFoundJunkRegion_ShouldBeCalledWithANonNullArgument()
        {
            crawler.OnFoundJunkRegion +=
                delegate(FoundJunkRegionEventArgs e)
                {
                    Assert.That(e, Is.Not.Null);
                };

            builder.AddJunk(10);
            crawler.BeginStreamProcessEventHandler(
                new BeginStreamProcessEventArgs(builder.BuildStream()));
        }

        [Test]
        public void OnFoundMp3Frame_ShouldBeCalledWithANonNullArgument()
        {
            crawler.OnFoundMp3Frame +=
                delegate(FoundMp3FrameEventArgs e)
                {
                    Assert.That(e, Is.Not.Null);
                };

            builder.AddFrame(MpegVersion.V25, MpegLayer.Layer1, true, 0, 0);
            builder.AddFrame(MpegVersion.V25, MpegLayer.Layer1, true, 0, 0);
            crawler.BeginStreamProcessEventHandler(
                new BeginStreamProcessEventArgs(builder.BuildStream()));
        }

        public void Handler(IFoundMp3StreamRegionEventArgs e)
        {
        }

        [Test]
        public void OnFoundMp3StreamRegion_Test()
        {
            // Just want to make sure that contravariance makes
            // it possible for a single handler to handle both
            // these events
            crawler.OnFoundMp3Frame += Handler;
            crawler.OnFoundJunkRegion += Handler;
        }

        #endregion
    }
}
