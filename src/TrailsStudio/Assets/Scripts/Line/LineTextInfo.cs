using System.Collections.Generic;
using System.Threading.Tasks;
using QuestPDF.Fluent;

using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Linq;

public static class LineReportGenerator
{
    public static Task GeneratePdfAsync(LineTextInfo lineInfo, string lineName, string filePath)
    {
        return Task.Run(() =>
        {
            QuestPDF.Settings.License = LicenseType.Community;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header()
                        .Text($"LINE REPORT: {Line.Instance.Name}")
                        .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            foreach (var item in lineInfo.Items)
                            {
                                column.Item().PaddingBottom(10).Row(row =>
                                {
                                    row.RelativeItem().Column(col =>
                                    {
                                        col.Item().Text($"[{item.Title}]").Bold().FontSize(14).FontColor(item.TextColor);
                                        col.Item().Text(item.GetDescription()).FontColor(item.TextColor);
                                    });
                                });

                                column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                        });
                });
            })
            .GeneratePdf(filePath);
        });
    }
}

public class LineTextInfo
{
    public interface ILineInfoItem
    {
        string Title { get; }
        string GetDescription();

        Color TextColor => Colors.Black;
    }

    public List<ILineInfoItem> Items { get; } = new();

    public override string ToString()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($" LINE REPORT: {Line.Instance.Name} ");
        sb.AppendLine("========================================");
        sb.AppendLine();

        foreach (var item in Items)
        {
            sb.AppendLine($"[{item.Title}]");
            sb.AppendLine(item.GetDescription());
            sb.AppendLine("----------------------------------------");
        }
        return sb.ToString();
    }

    public record RollInItem : ILineInfoItem
    {
        float angleDeg;
        float height;

        public RollInItem(float angleDeg, float height)
        {
            this.angleDeg = angleDeg;
            this.height = height;
        }

        public string Title => "Roll-In";
        public string GetDescription() => $"Height: {height:F2}m\nAngle: {angleDeg:F1}°";

        public Color TextColor => Colors.Teal.Darken1;
    }

    public record SlopeStartItem : ILineInfoItem
    {
        ILineElement previousElement;
        float distanceFromPrevious;

        float angleDeg;
        float heightDiff;
        float length;

        public SlopeStartItem(ILineElement previousElement, float distanceFromPrevious, float angleDeg, float heightDiff, float length)
        {
            this.previousElement = previousElement;
            this.distanceFromPrevious = distanceFromPrevious;
            this.angleDeg = angleDeg;
            this.heightDiff = heightDiff;
            this.length = length;
        }

        public string Title => "Slope Start";
        public string GetDescription() => $"Starts {distanceFromPrevious:F1}m after previous element\nSlope Length: {length:F1}m\nAngle: {angleDeg:F1}°\nHeight Diff: {heightDiff:F2}m";

        public Color TextColor => Colors.Green.Darken1;
    }

    public record SlopeEndItem : ILineInfoItem
    {
        float? distanceFromPrevious;

        public SlopeEndItem(float? distanceFromPrevious = null)
        {
            this.distanceFromPrevious = distanceFromPrevious;
        }

        public string Title => "Slope End";
        public string GetDescription() =>
             distanceFromPrevious.HasValue
            ? $"End of slope section.\nDistance from previous element: {distanceFromPrevious.Value:F1}m"
            : "End of slope section.";

        public Color TextColor => Colors.Red.Darken1;
    }

    public record TakeoffItem : ILineInfoItem
    {
        ILineElement previousElement;
        float distanceFromPrevious;

        float height;
        float length;
        float width;
        float radius;
        float endAngle;
        float jumpLength;

        float? slopeAngleDeg;

        public TakeoffItem(ILineElement previousElement, float distanceFromPrevious, float height, float length, float width, float radius, float endAngle, float jumpLength, float? slopeAngleDeg)
        {
            this.previousElement = previousElement;
            this.distanceFromPrevious = distanceFromPrevious;
            this.height = height;
            this.length = length;
            this.width = width;
            this.radius = radius;
            this.endAngle = endAngle;
            this.jumpLength = jumpLength;
            this.slopeAngleDeg = slopeAngleDeg;
        }

        public string Title => "Take-off";
        public string GetDescription() =>
            $"Dimensions: {length:F1}m(L) x {width:F1}m(W) x {height:F2}m(H)\n" +
            $"Radius: {radius:F1}m | Lip Angle: {endAngle:F1}°\n" +
            $"Position: {distanceFromPrevious:F1}m after previous\n" +
            $"Gap to Landing: {jumpLength:F1}m" +
            (slopeAngleDeg.HasValue ? $"\nOn Slope: {slopeAngleDeg:F1}°" : "");

        public Color TextColor => Colors.Orange.Darken1;
    }

    public record LandingItem : ILineInfoItem
    {
        float landingAreaSlopeDeg;
        float height;
        float length;
        float width;
        float jumpLength;
        float rotationDeg;
        float shiftToSide;
        float? slopeAngleDeg;

        public LandingItem(float landingAreaSlopeDeg, float height, float length, float width, float jumpLength, float rotationDeg, float shiftToSide, float? slopeAngleDeg)
        {
            this.landingAreaSlopeDeg = landingAreaSlopeDeg;
            this.height = height;
            this.length = length;
            this.width = width;
            this.jumpLength = jumpLength;
            this.rotationDeg = rotationDeg;
            this.shiftToSide = shiftToSide;
            this.slopeAngleDeg = slopeAngleDeg;
        }

        public string Title => "Landing";
        public string GetDescription() =>
            $"Dimensions: {length:F1}m(L) x {width:F1}m(W) x {height:F2}m(H)\n" +
            $"Steepness: {landingAreaSlopeDeg:F1}°\n" +
            $"Rotation against take-off: {rotationDeg:F1}° | Shift to Side: {shiftToSide:F1}m\n" +
            (slopeAngleDeg.HasValue ? $"On Slope: {slopeAngleDeg:F1}°" : "");

        public Color TextColor => Colors.Purple.Darken1;
    }
}
