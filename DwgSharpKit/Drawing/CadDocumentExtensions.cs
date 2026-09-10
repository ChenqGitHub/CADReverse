using ACadSharp;
using ACadSharp.Entities;
using ACadSharp.Tables;
using DwgSharpKit.Standards;

namespace DwgSharpKit
{
    public static class CadDocumentExtensions
    {
        public static void AddEntities<T>(this CadDocument doc, IEnumerable<T> entities)
            where T : Entity
        {
            foreach (var entity in entities)
            {
                doc.Entities.Add(entity);
            }
        }

        public static void AddTranslated(
            this CadDocument doc,
            CSMath.XYZ move,
            IEnumerable<Entity> templates,
            Action<Entity>? onCopy = null
        ) => AddTransformed(doc, move, templates, 1, 0, null, onCopy);

        /// <summary>
        /// 整组变换后写入文档：对每个模板克隆体依次做 缩放(绕 pivot)→旋转(绕 Z 轴，过 pivot)→平移(move)，
        /// 再调用 onCopy 并加入文档。与 <see cref="AddTranslated"/> 的区别是多了缩放与旋转（默认恒等时行为一致）。
        /// </summary>
        /// <param name="doc">CAD 文档</param>
        /// <param name="move">最终平移向量</param>
        /// <param name="templates">1:1 局部坐标模板图元（会被克隆，不修改模板）</param>
        /// <param name="scale">缩放比例，默认 1（不缩放）</param>
        /// <param name="rotation">旋转角（弧度，绕 Z 轴），默认 0（不旋转）</param>
        /// <param name="pivot">缩放/旋转基准点，默认局部原点 (0,0,0)</param>
        /// <param name="onCopy">每个克隆体加入文档前的回调（如写入 XData）</param>
        public static void AddTransformed(
            this CadDocument doc,
            CSMath.XYZ move,
            IEnumerable<Entity> templates,
            double scale = 1,
            double rotation = 0,
            CSMath.XYZ? pivot = null,
            Action<Entity>? onCopy = null
        )
        {
            foreach (var template in templates)
            {
                var copy = (Entity)template.Clone();
                ApplyPlaceTransform(copy, scale, rotation, pivot);
                copy.ApplyTranslation(move);
                if (copy is TextEntity text)
                {
                    // ACadSharp 的 ApplyRotation/ApplyTranslation 不移动 AlignmentPoint；
                    // 非基线对齐的 TEXT 在 CAD 中按组码 11（AlignmentPoint）锚定，
                    // CadDraw.Text 恒使两者相等，这里手工同步，保证文字跟随整组变换
                    text.AlignmentPoint = text.InsertPoint;
                }
                onCopy?.Invoke(copy);
                doc.Entities.Add(copy);
            }
        }

        private static void ApplyPlaceTransform(
            Entity e,
            double scale,
            double rotation,
            CSMath.XYZ? pivot
        )
        {
            var p = pivot ?? new CSMath.XYZ(0, 0, 0);
            if (scale != 1)
            {
                e.ApplyScaling(new CSMath.XYZ(scale, scale, 1), p);
            }
            if (rotation != 0)
            {
                // ApplyRotation 只支持绕经过原点的 Z 轴；任意 pivot 用 平移-旋转-平移 实现
                e.ApplyTranslation(new CSMath.XYZ(-p.X, -p.Y, -p.Z));
                e.ApplyRotation(CSMath.XYZ.AxisZ, rotation);
                e.ApplyTranslation(p);
            }
        }

        public static Layer Layer(this CadDocument doc, string name) => doc.Layers[name];

        public static Layer Layer(this CadDocument doc, CadLayerDef def) => doc.Layers[def.Name];

        public static TextStyle TextStyle(this CadDocument doc, string name) =>
            doc.TextStyles[name];

        public static TextStyle TextStyle(this CadDocument doc, CadTextStyleDef def) =>
            doc.TextStyles[def.Name];

        public static DimensionStyle DimStyle(this CadDocument doc, string name) =>
            doc.DimensionStyles[name];

        public static DimensionStyle DimStyle(this CadDocument doc, CadDimStyleDef def) =>
            doc.DimensionStyles[def.Name];
    }
}
