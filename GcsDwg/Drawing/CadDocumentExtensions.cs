using ACadSharp;
using ACadSharp.Entities;
using ACadSharp.Tables;
using GcsDwg.Standards;

namespace GcsDwg
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
            IEnumerable<Entity> templates
        )
        {
            foreach (var template in templates)
            {
                var copy = (Entity)template.Clone();
                copy.ApplyTranslation(move);
                doc.Entities.Add(copy);
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
