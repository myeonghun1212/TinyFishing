using System.Collections.Generic;
using Nan.Core;
using NUnit.Framework;

namespace Nan.Tests
{
    public sealed class FishingSessionTests
    {
        [Test]
        public void Cast_TransitionsToWaitingAndRejectsDuplicateRequests()
        {
            var session = new FishingSession();
            var transitions = new List<FishingState>();
            session.StateChanged += transitions.Add;

            Assert.That(session.State, Is.EqualTo(FishingState.Ready));
            Assert.That(session.TryCompleteCast(), Is.False);
            Assert.That(session.TryBeginCast(), Is.True);
            Assert.That(session.State, Is.EqualTo(FishingState.Casting));
            Assert.That(session.TryBeginCast(), Is.False);
            Assert.That(session.TryCompleteCast(), Is.True);
            Assert.That(session.State, Is.EqualTo(FishingState.Waiting));
            Assert.That(session.TryBeginCast(), Is.False);
            Assert.That(session.TryCompleteCast(), Is.False);
            CollectionAssert.AreEqual(new[] { FishingState.Casting, FishingState.Waiting }, transitions);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Reset_FromCastingOrWaitingEmitsReadyOnceAndAllowsAnotherCast(bool complete)
        {
            var session = new FishingSession();
            session.TryBeginCast();
            if (complete)
                session.TryCompleteCast();
            var transitions = new List<FishingState>();
            session.StateChanged += transitions.Add;

            session.Reset();
            session.Reset();

            Assert.That(session.State, Is.EqualTo(FishingState.Ready));
            CollectionAssert.AreEqual(new[] { FishingState.Ready }, transitions);
            Assert.That(session.TryBeginCast(), Is.True);
            Assert.That(session.TryCompleteCast(), Is.True);
        }

        [Test]
        public void Sessions_DoNotShareRuntimeState()
        {
            var first = new FishingSession();
            var second = new FishingSession();

            first.TryBeginCast();

            Assert.That(second.State, Is.EqualTo(FishingState.Ready));
            Assert.That(second.TryCompleteCast(), Is.False);
        }
    }
}
