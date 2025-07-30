using System.ComponentModel;
using Xbim.Ifc4.Interfaces;

namespace IfcDataExtractor
{
    public enum Orientation : byte
    {
        East = 0,
        North = 1,
        West = 2,
        South = 3
    }

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
        public static string? GetWallFinish(IIfcSpace space, Orientation orientation)
        {
            return orientation switch
            {
                Orientation.East => GetPropertyValue(space, "PavCusPropZones", "0301FinishWallEastType"),
                Orientation.North => GetPropertyValue(space, "PavCusPropZones", "0302FinishWallNorthType"),
                Orientation.West => GetPropertyValue(space, "PavCusPropZones", "0303FinishWallWestType"),
                Orientation.South => GetPropertyValue(space, "PavCusPropZones", "0304FinishWallSouthType"),
                _ => throw new InvalidEnumArgumentException("Unknown wall orientation")
            };
        }
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
