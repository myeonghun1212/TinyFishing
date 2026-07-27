using NanFishing.Data;
using NanFishing.Fishing;
using NanFishing.Modes;
using NUnit.Framework;
using UnityEngine;

namespace NanFishing.Tests
{
    public sealed class NanFishingTests
    {
        private GameBalanceConfig config;

        [SetUp]
        public void SetUp()
        {
            config = GameBalanceConfig.CreateRuntime();
            config.sessionDuration = 60f;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(config);
        }

        [Test]
        public void TimeAttack_EndsAtZeroAndNeverGoesNegative()
        {
            var rule = new TimeAttackRule(config);
            rule.Begin();
            rule.Tick(61f);
            Assert.That(rule.IsFinished, Is.True);
            Assert.That(rule.TimeRemaining, Is.EqualTo(0f));
        }

        [Test]
        public void TimeAttack_CatchBuildsComboAndFailureResetsIt()
        {
            var rule = new TimeAttackRule(config);
            rule.Begin();
            var first = rule.RegisterCatch(100, 0, 3f);
            var second = rule.RegisterCatch(100, 1, 5f);
            rule.RegisterFailure();

            Assert.That(first, Is.EqualTo(175));
            Assert.That(second, Is.EqualTo(175));
            Assert.That(rule.Score, Is.EqualTo(350));
            Assert.That(rule.CatchCount, Is.EqualTo(2));
            Assert.That(rule.MaxCombo, Is.EqualTo(2));
            Assert.That(rule.Combo, Is.EqualTo(0));
        }

        [Test]
        public void LineTension_AlignedReelingEventuallyCatchesFish()
        {
            var model = new LineTensionModel(config);
            for (var i = 0; i < 60 && !model.IsCaught; i++)
            {
                model.Tick(0.1f, true, 0.25f, 0.25f, 1f);
            }

            Assert.That(model.IsCaught, Is.True);
            Assert.That(model.IsBroken, Is.False);
        }

        [Test]
        public void LineTension_ReelingAlwaysProducesVisibleLoad()
        {
            var model = new LineTensionModel(config);
            var initial = model.Tension;

            model.Tick(1f, true, 0.25f, 0.25f, 1f);

            Assert.That(model.Tension, Is.GreaterThan(initial));
        }

        [Test]
        public void LineTension_ReleasingRecoversTensionAndLosesProgress()
        {
            var model = new LineTensionModel(config);
            for (var i = 0; i < 10; i++)
            {
                model.Tick(0.1f, true, -1f, 1f, 1.5f);
            }
            var tensionBefore = model.Tension;
            var progressBefore = model.Progress;

            model.Tick(0.5f, false, 0f, 0f, 1f);

            Assert.That(model.Tension, Is.LessThan(tensionBefore));
            Assert.That(model.Progress, Is.LessThan(progressBefore));
        }
    }
}
