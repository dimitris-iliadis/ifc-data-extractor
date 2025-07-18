using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.Common;

namespace IfcDataExtractor
{
    public static class IfcExtractor
    {
        public static IIfcBuildingStorey? GetFloor(IIfcSpace space)
        {
            return space.Decomposes
                .Select(r => r.RelatingObject)
                .OfType<IIfcBuildingStorey>()
                .FirstOrDefault();
        }

        public static IIfcValue? GetArea(IIfcProduct product)
        {
            return product.IsDefinedBy
                .SelectMany(r => r.RelatingPropertyDefinition.PropertySetDefinitions)
                .OfType<IIfcElementQuantity>()
                .SelectMany(qset => qset.Quantities)
                .OfType<IIfcQuantityArea>()
                .FirstOrDefault()?
                .AreaValue;
        }

        public static IIfcLengthMeasure? GetLengthQuantityByName(IIfcProduct product, string quantityName)
        {
            return product.IsDefinedBy
                .SelectMany(r => r.RelatingPropertyDefinition.PropertySetDefinitions)
                .OfType<IIfcElementQuantity>()
                .SelectMany(qset => qset.Quantities)
                .OfType<IIfcQuantityLength>()
                .FirstOrDefault(q => string.Equals(q.Name.ToString(), quantityName, StringComparison.OrdinalIgnoreCase))?
                .LengthValue;
        }

        public static string? GetRelatedZoneNumberFromElement(IIfcObject element) => GetPropertyValue(element, "ArchiCADProperties", "Related Zone Number");

        public static string? GetFloorMaterial(IIfcSpace space) => GetPropertyValue(space, "PavCusPropZones", "02FloorType");
        public static string? GetCeilingMaterial(IIfcSpace space) => GetPropertyValue(space, "PavCusPropZones", "01CeilingType");
        public static string? GetWallFinishEast(IIfcSpace space) => GetPropertyValue(space, "PavCusPropZones", "0301FinishWallEastType");
        public static string? GetWallFinishNorth(IIfcSpace space) => GetPropertyValue(space, "PavCusPropZones", "0302FinishWallNorthType");
        public static string? GetWallFinishWest(IIfcSpace space) => GetPropertyValue(space, "PavCusPropZones", "0303FinishWallWestType");
        public static string? GetWallFinishSouth(IIfcSpace space) => GetPropertyValue(space, "PavCusPropZones", "0304FinishWallSouthType");
        public static string? GetSkirting(IIfcSpace space) => GetPropertyValue(space, "PavCusPropZones", "04WallPerimeterSet");
        public static IIfcLengthMeasure? GetPerimeter(IIfcSpace space) => GetLengthQuantityByName(space, "Zone Net Perimeter");

        private static string? GetPropertyValue(IIfcObject obj, string psetName, string propName)
        {
            return obj.IsDefinedBy
                .Select(r => r.RelatingPropertyDefinition)
                .OfType<IIfcPropertySet>()
                .FirstOrDefault(p => p.Name == psetName)?
                .HasProperties
                .OfType<IIfcPropertySingleValue>()
                .FirstOrDefault(p => p.Name == propName)?
                .NominalValue?.Value.ToString();
        }
    }
}
