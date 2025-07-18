using Xbim.Ifc4.Interfaces;

namespace IfcDataExtractor
{
    public record SpaceInformation(
        string? Name,
        string? LongName,
        IIfcBuildingStorey? Floor,
        IIfcValue? Area,
        IIfcLengthMeasure? GrossHeight,
        IIfcLengthMeasure? NetHeight,
        IEnumerable<IIfcWindow> Windows,
        IEnumerable<IIfcDoor> Doors,
        string? FloorMaterial,
        string? CeilingMaterial,
        string? WallFinishEast,
        string? WallFinishNorth,
        string? WallFinishWest,
        string? WallFinishSouth,
        string? Skirting,
        IIfcLengthMeasure? Perimeter
    );
}