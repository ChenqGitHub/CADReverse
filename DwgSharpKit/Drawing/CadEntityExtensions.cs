using ACadSharp.Entities;
using CSMath;

namespace DwgSharpKit
{
    public static class CadEntityExtensions
    {
        /// <summary>
        /// 返回实体关于竖直镜像轴 x=axisX 的镜像副本。
        /// 原实体不修改。
        /// </summary>
        public static Entity MirrorAcrossVertical(this Entity entity, double axisX = 0)
        {
            var copy = (Entity)entity.Clone();
            copy.ApplyScaling(new XYZ(-1, 1, 1), new XYZ(axisX, 0, 0));
            return copy;
        }

        /// <summary>
        /// 返回实体关于水平镜像轴 y=axisY 的镜像副本。
        /// 原实体不修改。
        /// </summary>
        public static Entity MirrorAcrossHorizontal(this Entity entity, double axisY = 0)
        {
            var copy = (Entity)entity.Clone();
            copy.ApplyScaling(new XYZ(1, -1, 1), new XYZ(0, axisY, 0));
            return copy;
        }
    }
}
