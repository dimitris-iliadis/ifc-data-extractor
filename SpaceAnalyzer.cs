using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using System.Text;

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
                        Name: space.Name?.ToString(),
                        LongName: space.LongName?.ToString(),
                        Floor: IfcExtractor.GetFloor(space),
                        Area: IfcExtractor.GetArea(space),
                        GrossHeight: IfcExtractor.GetLengthQuantityByName(space, "Height"),
                        NetHeight: IfcExtractor.GetLengthQuantityByName(space, "ZoneCeilingHeight"),
                        Windows: spaceWindows ?? Enumerable.Empty<IIfcWindow>(),
                        Doors: spaceDoors ?? Enumerable.Empty<IIfcDoor>(),
                        FloorMaterial: IfcExtractor.GetFloorMaterial(space),
                        CeilingMaterial: IfcExtractor.GetCeilingMaterial(space),
                        WallFinishEast: IfcExtractor.GetWallFinish(space, Orientation.East),
                        WallFinishNorth: IfcExtractor.GetWallFinish(space, Orientation.North),
                        WallFinishWest: IfcExtractor.GetWallFinish(space, Orientation.West),
                        WallFinishSouth: IfcExtractor.GetWallFinish(space, Orientation.South),
                        Skirting: IfcExtractor.GetSkirting(space),
                        Perimeter: IfcExtractor.GetPerimeter(space)
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
    sb.AppendLine("<html lang=\"el\">");
    sb.AppendLine("<head>");
    sb.AppendLine("<meta charset=\"UTF-8\">");
    sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
    sb.AppendLine($"<title>Αναφορά Χώρου - {spaceInfo.Name}</title>");
    sb.AppendLine("<style>");
    
    // Print-specific CSS - optimized for single page
    sb.AppendLine("@page {");
    sb.AppendLine("    size: A4;");
    sb.AppendLine("    margin: 10mm 15mm 10mm 15mm;"); // Reduced margins
    sb.AppendLine("    @top-center { content: 'Αναφορά Χώρου'; font-size: 8pt; color: #666; }");
    sb.AppendLine("    @bottom-right { content: 'Σελίδα ' counter(page); font-size: 8pt; color: #666; }");
    sb.AppendLine("}");
    
    // Base styles - more compact
    sb.AppendLine("* { box-sizing: border-box; margin: 0; padding: 0; }");
    sb.AppendLine("body {");
    sb.AppendLine("    font-family: 'Segoe UI', 'Calibri', Arial, sans-serif;");
    sb.AppendLine("    font-size: 9pt;"); // Reduced font size
    sb.AppendLine("    line-height: 1.2;"); // Tighter line height
    sb.AppendLine("    color: #333;");
    sb.AppendLine("    background: white;");
    sb.AppendLine("    -webkit-print-color-adjust: exact;");
    sb.AppendLine("    print-color-adjust: exact;");
    sb.AppendLine("}");
    
    // Print media queries
    sb.AppendLine("@media print {");
    sb.AppendLine("    body { margin: 0; padding: 0; }");
    sb.AppendLine("    .no-print { display: none !important; }");
    sb.AppendLine("    .page-break { page-break-before: always; }");
    sb.AppendLine("    .avoid-break { page-break-inside: avoid; }");
    sb.AppendLine("    h1, h2, h3 { page-break-after: avoid; }");
    sb.AppendLine("    table { page-break-inside: auto; }");
    sb.AppendLine("    tr { page-break-inside: avoid; page-break-after: auto; }");
    sb.AppendLine("    thead { display: table-header-group; }");
    sb.AppendLine("}");
    
    // Container and layout - more compact
    sb.AppendLine(".report-container {");
    sb.AppendLine("    max-width: 190mm;"); // Slightly reduced width
    sb.AppendLine("    margin: 0 auto;");
    sb.AppendLine("    background: white;");
    sb.AppendLine("    padding: 0;");
    sb.AppendLine("    height: 100vh;"); // Full viewport height
    sb.AppendLine("    display: flex;");
    sb.AppendLine("    flex-direction: column;");
    sb.AppendLine("}");
    
    // Header styles - more compact
    sb.AppendLine(".report-header {");
    sb.AppendLine("    text-align: center;");
    sb.AppendLine("    border-bottom: 2pt solid #2c3e50;"); // Thinner border
    sb.AppendLine("    margin-bottom: 12pt;"); // Reduced margin
    sb.AppendLine("    padding-bottom: 8pt;"); // Reduced padding
    sb.AppendLine("    background: linear-gradient(135deg, #f8f9fa 0%, #e9ecef 100%);");
    sb.AppendLine("    padding: 12pt;"); // Reduced padding
    sb.AppendLine("    margin: -10pt -10pt 12pt -10pt;"); // Adjusted margins
    sb.AppendLine("    flex-shrink: 0;"); // Don't shrink header
    sb.AppendLine("}");
    
    sb.AppendLine("h1 {");
    sb.AppendLine("    font-size: 14pt;"); // Reduced size
    sb.AppendLine("    font-weight: bold;");
    sb.AppendLine("    color: #2c3e50;");
    sb.AppendLine("    margin: 0 0 4pt 0;"); // Reduced margin
    sb.AppendLine("    text-transform: uppercase;");
    sb.AppendLine("    letter-spacing: 0.5pt;"); // Reduced spacing
    sb.AppendLine("}");
    
    sb.AppendLine(".space-subtitle {");
    sb.AppendLine("    font-size: 11pt;"); // Reduced size
    sb.AppendLine("    color: #666;");
    sb.AppendLine("    margin: 0;");
    sb.AppendLine("    font-weight: normal;");
    sb.AppendLine("}");
    
    sb.AppendLine("h2 {");
    sb.AppendLine("    font-size: 10pt;"); // Reduced size
    sb.AppendLine("    font-weight: bold;");
    sb.AppendLine("    color: #c0392b;");
    sb.AppendLine("    margin: 8pt 0 4pt 0;"); // Reduced margins
    sb.AppendLine("    padding: 4pt 8pt;"); // Reduced padding
    sb.AppendLine("    background: #f8f9fa;");
    sb.AppendLine("    border-left: 3pt solid #c0392b;"); // Thinner border
    sb.AppendLine("    text-transform: uppercase;");
    sb.AppendLine("    letter-spacing: 0.3pt;"); // Reduced spacing
    sb.AppendLine("}");
    
    // Table styles - more compact
    sb.AppendLine("table {");
    sb.AppendLine("    width: 100%;");
    sb.AppendLine("    border-collapse: collapse;");
    sb.AppendLine("    margin: 6pt 0;"); // Reduced margin
    sb.AppendLine("    font-size: 8pt;"); // Smaller font
    sb.AppendLine("    background: white;");
    sb.AppendLine("    box-shadow: 0 0.5pt 1pt rgba(0,0,0,0.1);"); // Subtle shadow
    sb.AppendLine("}");
    
    sb.AppendLine("th {");
    sb.AppendLine("    background: #34495e;");
    sb.AppendLine("    color: white;");
    sb.AppendLine("    padding: 4pt 6pt;"); // Reduced padding
    sb.AppendLine("    text-align: left;");
    sb.AppendLine("    font-weight: bold;");
    sb.AppendLine("    font-size: 7pt;"); // Smaller font
    sb.AppendLine("    text-transform: uppercase;");
    sb.AppendLine("    letter-spacing: 0.3pt;");
    sb.AppendLine("    border-bottom: 1pt solid #2c3e50;"); // Thinner border
    sb.AppendLine("}");
    
    sb.AppendLine("td {");
    sb.AppendLine("    padding: 3pt 6pt;"); // Reduced padding
    sb.AppendLine("    border-bottom: 0.5pt solid #e0e0e0;"); // Thinner border
    sb.AppendLine("    vertical-align: top;");
    sb.AppendLine("}");
    
    sb.AppendLine("tr:nth-child(even) td {");
    sb.AppendLine("    background: #f8f9fa;");
    sb.AppendLine("}");
    
    sb.AppendLine("tr:last-child td {");
    sb.AppendLine("    border-bottom: 1pt solid #34495e;"); // Thinner border
    sb.AppendLine("}");
    
    // Wall diagram styles - more compact
    sb.AppendLine(".materials-section {");
    sb.AppendLine("    display: flex;");
    sb.AppendLine("    align-items: flex-start;");
    sb.AppendLine("    gap: 12pt;"); // Reduced gap
    sb.AppendLine("    margin: 8pt 0;"); // Reduced margin
    sb.AppendLine("}");
    
    sb.AppendLine(".wall-diagram-container {");
    sb.AppendLine("    flex: 0 0 80pt;"); // Smaller diagram
    sb.AppendLine("    text-align: center;");
    sb.AppendLine("    padding: 8pt;"); // Reduced padding
    sb.AppendLine("    background: #f8f9fa;");
    sb.AppendLine("    border-radius: 3pt;"); // Smaller radius
    sb.AppendLine("    border: 0.5pt solid #e0e0e0;"); // Thinner border
    sb.AppendLine("}");
    
    sb.AppendLine(".wall-diagram-title {");
    sb.AppendLine("    font-size: 7pt;"); // Smaller font
    sb.AppendLine("    font-weight: bold;");
    sb.AppendLine("    color: #2c3e50;");
    sb.AppendLine("    margin-bottom: 6pt;"); // Reduced margin
    sb.AppendLine("    text-transform: uppercase;");
    sb.AppendLine("}");
    
    sb.AppendLine(".wall-diagram {");
    sb.AppendLine("    border: 1.5pt solid #2c3e50;"); // Thinner border
    sb.AppendLine("    width: 50pt;"); // Smaller diagram
    sb.AppendLine("    height: 50pt;"); // Smaller diagram
    sb.AppendLine("    position: relative;");
    sb.AppendLine("    margin: 0 auto;");
    sb.AppendLine("    background: white;");
    sb.AppendLine("    box-shadow: 0 1pt 2pt rgba(0,0,0,0.1);"); // Subtle shadow
    sb.AppendLine("}");
    
    sb.AppendLine(".wall-label {");
    sb.AppendLine("    position: absolute;");
    sb.AppendLine("    font-weight: bold;");
    sb.AppendLine("    font-size: 8pt;"); // Smaller font
    sb.AppendLine("    color: #c0392b;");
    sb.AppendLine("    background: white;");
    sb.AppendLine("    padding: 1pt 3pt;"); // Reduced padding
    sb.AppendLine("    border: 0.5pt solid #c0392b;"); // Thinner border
    sb.AppendLine("    border-radius: 1pt;"); // Smaller radius
    sb.AppendLine("}");
    
    sb.AppendLine(".wall-label.north { top: -8pt; left: 50%; transform: translateX(-50%); }");
    sb.AppendLine(".wall-label.south { bottom: -8pt; left: 50%; transform: translateX(-50%); }");
    sb.AppendLine(".wall-label.east { right: -8pt; top: 50%; transform: translateY(-50%); }");
    sb.AppendLine(".wall-label.west { left: -8pt; top: 50%; transform: translateY(-50%); }");
    
    sb.AppendLine(".materials-table {");
    sb.AppendLine("    flex: 1;");
    sb.AppendLine("}");
    
    // Value highlighting
    sb.AppendLine(".highlight-value {");
    sb.AppendLine("    font-weight: bold;");
    sb.AppendLine("    color: #2c3e50;");
    sb.AppendLine("}");
    
    sb.AppendLine(".orientation-label {");
    sb.AppendLine("    color: #c0392b;");
    sb.AppendLine("    font-weight: bold;");
    sb.AppendLine("}");
    
    // Summary box - more compact
    sb.AppendLine(".summary-box {");
    sb.AppendLine("    background: linear-gradient(135deg, #e3f2fd 0%, #f8f9fa 100%);");
    sb.AppendLine("    border: 0.5pt solid #2196f3;"); // Thinner border
    sb.AppendLine("    border-radius: 3pt;"); // Smaller radius
    sb.AppendLine("    padding: 8pt;"); // Reduced padding
    sb.AppendLine("    margin: 6pt 0;"); // Reduced margin
    sb.AppendLine("}");
    
    // Content area - flexible
    sb.AppendLine(".content-area {");
    sb.AppendLine("    flex: 1;");
    sb.AppendLine("    overflow: hidden;"); // Prevent overflow
    sb.AppendLine("}");
    
    // Footer - compact
    sb.AppendLine(".report-footer {");
    sb.AppendLine("    margin-top: auto;"); // Push to bottom
    sb.AppendLine("    padding-top: 6pt;"); // Reduced padding
    sb.AppendLine("    border-top: 0.5pt solid #e0e0e0;"); // Thinner border
    sb.AppendLine("    font-size: 7pt;"); // Smaller font
    sb.AppendLine("    color: #666;");
    sb.AppendLine("    text-align: center;");
    sb.AppendLine("    flex-shrink: 0;"); // Don't shrink footer
    sb.AppendLine("}");
    
    // Responsive adjustments for very long content
    sb.AppendLine("@media print {");
    sb.AppendLine("    .materials-section { flex-direction: column; gap: 6pt; }");
    sb.AppendLine("    .wall-diagram-container { flex: none; align-self: center; }");
    sb.AppendLine("    table { font-size: 7pt; }");
    sb.AppendLine("    td, th { padding: 2pt 4pt; }");
    sb.AppendLine("}");
    
    sb.AppendLine("</style>");
    sb.AppendLine("</head>");
    sb.AppendLine("<body>");
    sb.AppendLine("<div class='report-container'>");
    
    // Header
    sb.AppendLine("<div class='report-header avoid-break'>");
    sb.AppendLine($"<h1>Αναφορά Χώρου</h1>");
    sb.AppendLine($"<p class='space-subtitle'>{spaceInfo.Name}{(string.IsNullOrEmpty(spaceInfo.LongName) ? "" : $" - {spaceInfo.LongName}")}</p>");
    sb.AppendLine("</div>");
    
    // Content area
    sb.AppendLine("<div class='content-area'>");
    
    // Basic Information
    sb.AppendLine("<div class='summary-box avoid-break'>");
    sb.AppendLine("<h2>Βασικά Στοιχεία</h2>");
    sb.AppendLine("<table>");
    sb.AppendLine("<thead>");
    sb.AppendLine("<tr><th>Χαρακτηριστικό</th><th>Τιμή</th></tr>");
    sb.AppendLine("</thead>");
    sb.AppendLine("<tbody>");
    sb.AppendLine($"<tr><td>Όροφος</td><td class='highlight-value'>{spaceInfo.Floor?.Name ?? "Μη διαθέσιμο"}</td></tr>");
    sb.AppendLine($"<tr><td>Εμβαδόν</td><td class='highlight-value'>{($"{spaceInfo.Area?.Value:F2} m²")}</td></tr>");
    sb.AppendLine($"<tr><td>Μεικτό Ύψος</td><td class='highlight-value'>{($"{spaceInfo.GrossHeight?.Value:F2} m")}</td></tr>");
    sb.AppendLine($"<tr><td>Καθαρό Ύψος</td><td class='highlight-value'>{($"{spaceInfo.NetHeight?.Value:F2} m")}</td></tr>");
    sb.AppendLine($"<tr><td>Περίμετρος</td><td class='highlight-value'>{($"{spaceInfo.Perimeter?.Value:F2} m")}</td></tr>");
    sb.AppendLine("</tbody>");
    sb.AppendLine("</table>");
    sb.AppendLine("</div>");
    
    // Materials and Finishes
    sb.AppendLine("<h2>Υλικά & Φινιρίσματα</h2>");
    sb.AppendLine("<div class='materials-section avoid-break'>");
    
    // Wall diagram
    sb.AppendLine("<div class='wall-diagram-container'>");
    sb.AppendLine("<div class='wall-diagram-title'>Προσανατολισμός</div>");
    sb.AppendLine("<div class='wall-diagram'>");
    sb.AppendLine("<div class='wall-label north'>Β</div>");
    sb.AppendLine("<div class='wall-label south'>Ν</div>");
    sb.AppendLine("<div class='wall-label east'>Α</div>");
    sb.AppendLine("<div class='wall-label west'>Δ</div>");
    sb.AppendLine("</div>");
    sb.AppendLine("</div>");
    
    // Materials table
    sb.AppendLine("<div class='materials-table'>");
    sb.AppendLine("<table>");
    sb.AppendLine("<thead>");
    sb.AppendLine("<tr><th>Επιφάνεια</th><th>Υλικό/Φινίρισμα</th></tr>");
    sb.AppendLine("</thead>");
    sb.AppendLine("<tbody>");
    sb.AppendLine($"<tr><td>Δάπεδο</td><td>{spaceInfo.FloorMaterial ?? "Μη διαθέσιμο"}</td></tr>");
    sb.AppendLine($"<tr><td>Οροφή</td><td>{spaceInfo.CeilingMaterial ?? "Μη διαθέσιμο"}</td></tr>");
    sb.AppendLine($"<tr><td><span class='orientation-label'>Α</span> Τοίχος</td><td>{spaceInfo.WallFinishEast ?? "Μη διαθέσιμο"}</td></tr>");
    sb.AppendLine($"<tr><td><span class='orientation-label'>Β</span> Τοίχος</td><td>{spaceInfo.WallFinishNorth ?? "Μη διαθέσιμο"}</td></tr>");
    sb.AppendLine($"<tr><td><span class='orientation-label'>Δ</span> Τοίχος</td><td>{spaceInfo.WallFinishWest ?? "Μη διαθέσιμο"}</td></tr>");
    sb.AppendLine($"<tr><td><span class='orientation-label'>Ν</span> Τοίχος</td><td>{spaceInfo.WallFinishSouth ?? "Μη διαθέσιμο"}</td></tr>");
    sb.AppendLine($"<tr><td>Περιθώριο</td><td>{spaceInfo.Skirting ?? "Μη διαθέσιμο"}</td></tr>");
    sb.AppendLine("</tbody>");
    sb.AppendLine("</table>");
    sb.AppendLine("</div>");
    sb.AppendLine("</div>");
    
    // Windows and Doors - only if they exist and space permits
    if (spaceInfo.Windows.Any() || spaceInfo.Doors.Any())
    {
        sb.AppendLine("<h2>Κουφώματα</h2>");
        sb.AppendLine("<table>");
        sb.AppendLine("<thead>");
        sb.AppendLine("<tr><th>Τύπος</th><th>Όνομα</th><th>Διαστάσεις</th></tr>");
        sb.AppendLine("</thead>");
        sb.AppendLine("<tbody>");
        
        foreach (var window in spaceInfo.Windows)
        {
            var dimensions = $"{window.OverallWidth?.Value:F2}×{window.OverallHeight?.Value:F2}m";
            sb.AppendLine($"<tr><td>Παράθυρο</td><td>{window.Name ?? "—"}</td><td class='highlight-value'>{dimensions}</td></tr>");
        }
        
        foreach (var door in spaceInfo.Doors)
        {
            var dimensions = $"{door.OverallWidth?.Value:F2}×{door.OverallHeight?.Value:F2}m";
            sb.AppendLine($"<tr><td>Πόρτα</td><td>{door.Name ?? "—"}</td><td class='highlight-value'>{dimensions}</td></tr>");
        }
        
        sb.AppendLine("</tbody>");
        sb.AppendLine("</table>");
    }
    
    sb.AppendLine("</div>"); // End content area
    
    // Footer
    sb.AppendLine("<div class='report-footer'>");
    sb.AppendLine($"<p>Δημιουργήθηκε: {DateTime.Now:dd/MM/yyyy HH:mm} | Αυτόματη επεξεργασία IFC</p>");
    sb.AppendLine("</div>");
    
    sb.AppendLine("</div>");
    sb.AppendLine("</body>");
    sb.AppendLine("</html>");
    
    return sb.ToString();
}
    }
}
