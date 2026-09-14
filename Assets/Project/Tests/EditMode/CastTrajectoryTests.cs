using Nan.Fishing;
using NUnit.Framework;
using UnityEngine;

namespace Nan.Tests
{
    public sealed class CastTrajectoryTests
    {
        [TestCase(-1f, false)]
        [TestCase(0f, false)]
        [TestCase(1f, true)]
        [TestCase(2f, true)]
        public void Evaluate_ClampsToLaunchAndLandingPositions(float progress, bool atLanding)
        {
            var start = new Vector3(2f, 3f, 4f);
            var end = new Vector3(8f, 1f, 12f);

            Vector3 result = CastTrajectory.Evaluate(start, end, 5f, progress);

            Assert.That(Vector3.Distance(result, atLanding ? end : start), Is.LessThan(0.0001f));
        }

        [Test]
        public void Evaluate_MidpointHasConfiguredHeightAboveDirectPath()
        {
            var start = new Vector3(2f, 3f, 4f);
            var end = new Vector3(8f, 1f, 12f);

            Vector3 result = CastTrajectory.Evaluate(start, end, 5f, 0.5f);

            Assert.That(Vector3.Distance(result, new Vector3(5f, 7f, 8f)), Is.LessThan(0.0001f));
        }

        [Test]
        public void Evaluate_NegativeHeightDoesNotCreateAnInvertedArc()
        {
            Vector3 result = CastTrajectory.Evaluate(Vector3.zero, Vector3.right * 8f, -2f, 0.25f);

            Assert.That(Vector3.Distance(result, Vector3.right * 2f), Is.LessThan(0.0001f));
        }
    }
}
