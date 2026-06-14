using NUnit.Framework;
using UnityAnimationGraph.Editor;

namespace UnityAnimationGraph.Tests {
    /// <summary>
    /// AnimationGraphPreviewTimelineView tests.
    /// </summary>
    public sealed class AnimationGraphPreviewTimelineViewTests {
        /// <summary>
        /// SetPreviewState returns even when layout width has not been resolved yet.
        /// </summary>
        [Test]
        [Timeout(1000)]
        public void SetPreviewState_BeforeLayout_DoesNotHang() {
            var timelineView = new AnimationGraphPreviewTimelineView();

            timelineView.SetPreviewState(0.5f, 1.0f, 30);

            Assert.Pass();
        }

        /// <summary>
        /// Non-finite preview values are ignored instead of entering tick rebuild loops.
        /// </summary>
        [Test]
        [Timeout(1000)]
        public void SetPreviewState_NonFiniteValues_DoesNotHang() {
            var timelineView = new AnimationGraphPreviewTimelineView();

            timelineView.SetPreviewState(float.NaN, float.NaN, 30);
            timelineView.SetPreviewState(float.PositiveInfinity, float.PositiveInfinity, 30);

            Assert.Pass();
        }
    }
}
