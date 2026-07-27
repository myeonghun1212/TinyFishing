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
        public void CatchZone_OnlyBuildsGaugeWithActionInsideGreenZone()
        {
            Random.InitState(7);
            var model = new CatchZoneModel(config);

            model.Tick(1f, false, model.ZoneCenter);
            Assert.That(model.Progress, Is.EqualTo(0f));

            model.Tick(1f, true, model.ZoneCenter + 2f);
            Assert.That(model.Progress, Is.EqualTo(0f));

            model.Tick(0.25f, true, model.ZoneCenter);
            Assert.That(model.Progress, Is.GreaterThan(0f));
        }

        [Test]
        public void CatchZone_ChangesAfterEachTwentyPercentMilestone()
        {
            Random.InitState(11);
            var model = new CatchZoneModel(config);
            var initialCenter = model.ZoneCenter;
            var initialWidth = model.ZoneHalfWidth;

            model.Tick(0.7f, true, model.ZoneCenter);

            Assert.That(model.Progress, Is.GreaterThanOrEqualTo(0.2f));
            Assert.That(model.ZoneCenter != initialCenter || model.ZoneHalfWidth != initialWidth,
                Is.True);
        }

        [Test]
        public void CatchZone_ReachesCaughtWhenAllHitsAreValid()
        {
            Random.InitState(17);
            var model = new CatchZoneModel(config);
            for (var index = 0; index < 20 && !model.IsCaught; index++)
            {
                model.Tick(0.2f, true, model.ZoneCenter);
            }

            Assert.That(model.IsCaught, Is.True);
        }
    }
}
