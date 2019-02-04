using Gdip = System.Drawing;

namespace OsbPlayerTest.Animation
{
    internal abstract class AnimatedInstance
    {
        public abstract void Fade(EasingType easingEnum, int startTime, int endTime, float startOpacity,
            float endOpacity);
        public abstract void Move(EasingType easingEnum, int startTime, int endTime, Gdip.PointF startPoint,
            Gdip.PointF endPoint);
        public abstract void Rotate(EasingType easingEnum, int startTime, int endTime, float startDeg, float endDeg);
        public abstract void ScaleVec(EasingType easingEnum, int startTime, int endTime, float startWidth,
            float startHeight, float endWidth, float endHeight);

        public abstract void MoveX(EasingType easingEnum, int startTime, int endTime, float startX, float endX);
        public abstract void MoveY(EasingType easingEnum, int startTime, int endTime, float startY, float endY);
    }
}
