using CNoom.MoYuNovel.Runtime.Output;
using NUnit.Framework;

namespace CNoom.MoYuNovel.Tests
{
    /// <summary>IntervalPolicy 契约测试（FR-003：默认 1s、下限 0.2s、非法值钳制）。</summary>
    public class IntervalPolicyTests
    {
        [Test]
        public void Constants_MatchClarifiedValues()
        {
            Assert.AreEqual(1f, IntervalPolicy.DefaultSeconds);
            Assert.AreEqual(0.2f, IntervalPolicy.MinSeconds);
            Assert.AreEqual(3600f, IntervalPolicy.MaxSeconds);
        }

        [Test]
        public void Clamp_LegalValue_Unchanged()
        {
            Assert.AreEqual(0.5f, IntervalPolicy.Clamp(0.5f));
            Assert.AreEqual(2f, IntervalPolicy.Clamp(2f));
            Assert.AreEqual(3600f, IntervalPolicy.Clamp(3600f));
        }

        [Test]
        public void Clamp_TooSmall_ClampsToMin()
        {
            Assert.AreEqual(0.2f, IntervalPolicy.Clamp(0.01f));
            Assert.AreEqual(0.2f, IntervalPolicy.Clamp(0.2f));
        }

        [Test]
        public void Clamp_NegativeOrNaN_ClampsToMin()
        {
            Assert.AreEqual(0.2f, IntervalPolicy.Clamp(-1f));
            Assert.AreEqual(0.2f, IntervalPolicy.Clamp(float.NaN));
            Assert.AreEqual(0.2f, IntervalPolicy.Clamp(float.NegativeInfinity));
        }

        [Test]
        public void Clamp_TooLarge_ClampsToMax()
        {
            Assert.AreEqual(3600f, IntervalPolicy.Clamp(99999f));
            Assert.AreEqual(3600f, IntervalPolicy.Clamp(float.PositiveInfinity));
        }
    }
}
