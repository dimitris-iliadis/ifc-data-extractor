using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using System.Text;
using System.IO;

namespace IfcDataExtractor
{
    public static class SpaceAnalyzer
    {
        public static void ProcessIfcFile(string fileName, bool debug = false)
        {
            try
            {
                Console.WriteLine("Opening IFC file...");
                using var model = IfcStore.Open(fileName);

                if (debug)
                {
                    var firstSpace = model.Instances.OfType<IIfcSpace>().FirstOrDefault();
                    if (firstSpace != null)
                    {
                        PrintDebugProperties(firstSpace);
                    }
                    else
                    {
                        Console.WriteLine("No spaces found in the IFC file.");
                    }
                    return;
                }

                Console.WriteLine("Processing spaces...");
                var windowsByZone = model.Instances.OfType<IIfcWindow>()
                    .Select(w => new { Window = w, Zone = IfcExtractor.GetRelatedZoneNumberFromElement(w) })
                    .Where(x => x.Zone != null)
                    .GroupBy(x => x.Zone)
                    .ToDictionary(g => g.Key!, g => g.Select(x => x.Window).ToList());

                var doorsByZone = model.Instances.OfType<IIfcDoor>()
                    .Select(d => new { Door = d, Zone = IfcExtractor.GetRelatedZoneNumberFromElement(d) })
                    .Where(x => x.Zone != null)
                    .GroupBy(x => x.Zone)
                    .ToDictionary(g => g.Key!, g => g.Select(x => x.Door).ToList());

                var spaces = model.Instances.OfType<IIfcSpace>().ToList();

                Console.WriteLine("Generating reports...");
                Directory.CreateDirectory("reports");

                for (int i = 0; i < spaces.Count; i++)
                {
                    var space = spaces[i];
                    if (space.Name == null) continue;

                    windowsByZone.TryGetValue(space.Name, out var spaceWindows);
                    doorsByZone.TryGetValue(space.Name, out var spaceDoors);

                    var spaceInfo = new SpaceInformation
                    (
                        space.Name?.ToString(),
                        space.LongName?.ToString(),
                        IfcExtractor.GetFloor(space),
                        IfcExtractor.GetArea(space),
                        IfcExtractor.GetLengthQuantityByName(space, "Height"),
                        IfcExtractor.GetLengthQuantityByName(space, "ZoneCeilingHeight"),
                        spaceWindows ?? Enumerable.Empty<IIfcWindow>(),
                        spaceDoors ?? Enumerable.Empty<IIfcDoor>(),
                        IfcExtractor.GetFloorMaterial(space),
                        IfcExtractor.GetCeilingMaterial(space),
                        IfcExtractor.GetWallFinishEast(space),
                        IfcExtractor.GetWallFinishNorth(space),
                        IfcExtractor.GetWallFinishWest(space),
                        IfcExtractor.GetWallFinishSouth(space),
                        IfcExtractor.GetSkirting(space),
                        IfcExtractor.GetPerimeter(space)
                    );

                    var report = GenerateHtmlReport(spaceInfo);
                    File.WriteAllText(Path.Combine("reports", $"{spaceInfo.Name}.html"), report);
                    Console.Write($"\rGenerated report {i + 1}/{spaces.Count}");
                }
                Console.WriteLine("\nFinished.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Σφάλμα κατά το άνοιγμα του αρχείου IFC: {ex.Message}");
                Environment.Exit(-2);
            }
        }

        private static void PrintDebugProperties(IIfcSpace space)
        {
            Console.WriteLine($"--- DEBUG: Properties for Space {space.Name} ---");
            var psets = space.IsDefinedBy
                .Select(r => r.RelatingPropertyDefinition)
                .OfType<IIfcPropertySet>();

            foreach (var pset in psets)
            {
                Console.WriteLine($"  PropertySet: {pset.Name}");
                foreach (var prop in pset.HasProperties)
                {
                    if (prop is IIfcPropertySingleValue pval)
                    {
                        Console.WriteLine($"    Property: {prop.Name}, Value: {pval.NominalValue}");
                    }
                    else
                    {
                        Console.WriteLine($"    Property: {prop.Name}, Value: (Complex Property)");
                    }
                }
            }
        }

        private static string GenerateHtmlReport(SpaceInformation spaceInfo)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"en\">");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset=\"UTF-8\">");
            sb.AppendLine("<title>Space Report</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f7f6; color: #333; line-height: 1.4; }");
            sb.AppendLine("@page { size: A4; margin: 1cm; }");
            sb.AppendLine(".report-container { background-color: #ffffff; padding: 1.5em; margin: 0 auto; box-shadow: 0 0 10px rgba(0,0,0,0.1); border-radius: 5px; }");
            sb.AppendLine("h1, h2 { color: #2c3e50; border-bottom: 1px solid #e0e0e0; padding-bottom: 8px; margin-bottom: 15px; }");
            sb.AppendLine("h1 { text-align: center; font-size: 1.8em; }");
            sb.AppendLine("h2 { font-size: 1.3em; color: #c0392b; }");
            sb.AppendLine(".container { display: flex; align-items: center; }");
            sb.AppendLine(".left-panel { flex: 1; padding: 15px; display: flex; justify-content: center; align-items: center; }");
            sb.AppendLine(".right-panel { flex: 2; padding-left: 15px; }");
            sb.AppendLine(".wall-diagram-container { text-align: center; }");
            sb.AppendLine(".wall-diagram { border: 2px solid #333; width: 90px; height: 90px; position: relative; margin: 20px auto; }");
            sb.AppendLine(".wall-label { position: absolute; font-weight: bold; color: #c0392b; }");
            sb.AppendLine(".wall-label.north { top: -20px; left: 0; right: 0; text-align: center; }");
            sb.AppendLine(".wall-label.south { bottom: -20px; left: 0; right: 0; text-align: center; }");
            sb.AppendLine(".wall-label.east { top: 50%; right: -20px; transform: translateY(-50%); }");
            sb.AppendLine(".wall-label.west { top: 50%; left: -20px; transform: translateY(-50%); }");
            sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 15px; }");
            sb.AppendLine("th, td { padding: 8px 12px; text-align: left; border-bottom: 1px solid #dddddd; }");
            sb.AppendLine("th { background-color: #444; color: #ffffff; text-transform: uppercase; font-size: 0.8em; letter-spacing: 0.05em; border-bottom-width: 2px; border-bottom-color: #222; }");
            sb.AppendLine("tr:last-child td { border-bottom: 0; }");
            sb.AppendLine("tr:nth-child(even) { background-color: #f8f9fa; }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("<div class='report-container'>");
            sb.AppendLine($"<h1>{spaceInfo.Name}    {spaceInfo.LongName}</h1>");
            sb.AppendLine("<table>");
            sb.AppendLine($"<tr><th>ΟΡΟΦΟΣ</th><td>{spaceInfo.Floor?.Name ?? "N/A"}</td></tr>");
            sb.AppendLine($"<tr><th>ΕΜΒΑΔΟΝ</th><td>{spaceInfo.Area?.Value:F2} m²</td></tr>");
            sb.AppendLine($"<tr><th>ΜΕΙΚΤΟ ΥΨΟΣ</th><td>{spaceInfo.GrossHeight?.Value:F2} m</td></tr>");
            sb.AppendLine($"<tr><th>ΚΑΘΑΡΟ ΥΨΟΣ</th><td>{spaceInfo.NetHeight?.Value:F2} m</td></tr>");
            sb.AppendLine("</table>");

            sb.AppendLine("<h2>ΥΛΙΚΑ & ΦΙΝΙΡΙΣΜΑΤΑ</h2>");
            sb.AppendLine("<div class='container'>");
            sb.AppendLine("<div class='left-panel'>");
            sb.AppendLine("<div class='wall-diagram-container'>");
            sb.AppendLine("<h5>ΤΟΙΧΟΣ</h5>");
            sb.AppendLine("<div class='wall-diagram'>");
            sb.AppendLine("<div class='wall-label north'>B</div>");
            sb.AppendLine("<div class='wall-label south'>N</div>");
            sb.AppendLine("<div class='wall-label east'>A</div>");
            sb.AppendLine("<div class='wall-label west'>Δ</div>");
            sb.AppendLine("</div>");
            sb.AppendLine("</div>");
            sb.AppendLine("</div>");
            sb.AppendLine("<div class='right-panel'>");
            sb.AppendLine("<table>");
            sb.AppendLine($"<tr><th>ΥΛΙΚΟ ΔΑΠΕΔΟΥ</th><td>{spaceInfo.FloorMaterial ?? "N/A"}</td></tr>");
            sb.AppendLine($"<tr><th>ΥΛΙΚΟ ΟΡΟΦΗΣ</th><td>{spaceInfo.CeilingMaterial ?? "N/A"}</td></tr>");
            sb.AppendLine($"<tr><th><span style='color: #c0392b;'>A</span> ΥΛΙΚΟ ΤΟΙΧΟΥ (ΑΝΑΤΟΛΗ)</th><td>{spaceInfo.WallFinishEast ?? "N/A"}</td></tr>");
            sb.AppendLine($"<tr><th><span style='color: #c0392b;'>B</span> ΥΛΙΚΟ ΤΟΙΧΟΥ (ΒΟΡΑΣ)</th><td>{spaceInfo.WallFinishNorth ?? "N/A"}</td></tr>");
            sb.AppendLine($"<tr><th><span style='color: #c0392b;'>Δ</span> ΥΛΙΚΟ ΤΟΙΧΟΥ (ΔΥΣΗ)</th><td>{spaceInfo.WallFinishWest ?? "N/A"}</td></tr>");
            sb.AppendLine($"<tr><th><span style='color: #c0392b;'>N</span> ΥΛΙΚΟ ΤΟΙΧΟΥ (ΝΟΤΟΣ)</th><td>{spaceInfo.WallFinishSouth ?? "N/A"}</td></tr>");
            sb.AppendLine($"<tr><th>ΠΕΡΙΜΕΤΡΟΣ</th><td>{spaceInfo.Perimeter?.Value:F2} m</td></tr>");
            sb.AppendLine($"<tr><th>ΠΕΡΙΘΩΡΙΟ</th><td>{spaceInfo.Skirting ?? "N/A"}</td></tr>");
            sb.AppendLine("</table>");
            sb.AppendLine("</div>");
            sb.AppendLine("</div>");

            if (spaceInfo.Windows.Any() || spaceInfo.Doors.Any())
            {
                sb.AppendLine("<h2>ΚΟΥΦΩΜΑΤΑ</h2>");
                sb.AppendLine("<table>");
                sb.AppendLine("<tr><th>ΤΥΠΟΣ</th><th>ΟΝΟΜΑ</th><th>ΠΛΑΤΟΣ x ΥΨΟΣ</th></tr>");
                foreach (var window in spaceInfo.Windows)
                {
                    sb.AppendLine($"<tr><td>Παράθυρο</td><td>{window.Name}</td><td>{window.OverallWidth?.Value:F2} x {window.OverallHeight?.Value:F2} m</td></tr>");
                }
                foreach (var door in spaceInfo.Doors)
                {
                    sb.AppendLine($"<tr><td>Πόρτα</td><td>{door.Name}</td><td>{door.OverallWidth?.Value:F2} x {door.OverallHeight?.Value:F2} m</td></tr>");
                }
                sb.AppendLine("</table>");
            }
            sb.AppendLine("</div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }
    }
}
