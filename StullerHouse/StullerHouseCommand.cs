using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System.ComponentModel;


namespace StullerHouse
{
    public class StullerHouseCommand : Command
    {
        public StullerHouseCommand()
        {
            Instance = this;
        }

        public static StullerHouseCommand Instance { get; private set; }

        public override string EnglishName => "BuildHouse";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var conduit = StullerHousePlugin.Instance.Conduit;
            conduit.Enabled = true;
            doc.Views.Redraw();
            try
            {

                Point3d insertionPt = Point3d.Origin;
                var rc = RhinoGet.GetPoint("House insertion point", true, out insertionPt);
                if (rc != Result.Success) return rc;

                double width = 10.0, depth = 8.0, height = 6.0, roofHeight = 5.0;

                // Show a full preview using all defaults as soon as insertion point is picked
                conduit.PreviewBody = BuildBodyPreview(insertionPt, width, depth, height);
                conduit.PreviewRoof = BuildRoofPreview(insertionPt, width, depth, height, roofHeight, doc);
                doc.Views.Redraw();

                OptionDouble OptionWidth = new OptionDouble(10, 1, 100);
                OptionDouble OptionDepth = new OptionDouble(8, 1, 80);
                OptionDouble OptionHeight = new OptionDouble(6, 1, 60);
                OptionDouble OptionRoofHeight = new OptionDouble(5, 1, 50);


                GetObject go = new GetObject();
                go.SetCommandPrompt("Select surfaces, polysurfaces, or meshes");

                go.AddOptionDouble("Width", ref OptionWidth);
                go.AddOptionDouble("Depth", ref OptionDepth);
                go.AddOptionDouble("Height", ref OptionHeight);
                go.AddOptionDouble("RoofHeight", ref OptionRoofHeight);

                while(true)
                { 
                    var previewResult = go.Get();

                    if (previewResult == GetResult.Option)
                        {
                            go.EnablePreSelect(false,true);

                            width = OptionWidth.CurrentValue;
                            depth = OptionDepth.CurrentValue;
                            height = OptionHeight.CurrentValue;
                            roofHeight = OptionRoofHeight.CurrentValue;

                            conduit.PreviewBody = BuildBodyPreview(insertionPt, width, depth, height);
                            conduit.PreviewRoof = BuildRoofPreview(insertionPt, width, depth, height, roofHeight, doc);

                            doc.Views.Redraw();
                            continue;
                        }

                    else if (previewResult == GetResult.Object)
                        {
                            width = OptionWidth.CurrentValue;
                            depth = OptionDepth.CurrentValue;
                            height = OptionHeight.CurrentValue;
                            roofHeight = OptionRoofHeight.CurrentValue;

                            break;
                        }

                    else
                        {
                        return go.CommandResult();
                        }
                }


                if (width <= 0 || depth <= 0 || height <= 0 || roofHeight <= 0)
                {
                    RhinoApp.WriteLine("All dimensions must be greater than zero.");
                    return Result.Failure;
                }

                // Build final geometry for the document
                Plane basePlane = Plane.WorldXY;
                basePlane.Origin = insertionPt;
                Box bodyBox = new Box(basePlane,
                    new Interval(0, width),
                    new Interval(0, depth),
                    new Interval(0, height));
                Brep bodyBrep = bodyBox.ToBrep();

                double overhang = width * .17;
                Point3d p1 = new Point3d(insertionPt.X - overhang, insertionPt.Y, insertionPt.Z + height);
                Point3d p2 = new Point3d(insertionPt.X + width + overhang, insertionPt.Y, insertionPt.Z + height);
                Point3d p3 = new Point3d(insertionPt.X + width / 2.0, insertionPt.Y, insertionPt.Z + height + roofHeight);
                Polyline triangle = new Polyline { p1, p2, p3, p1 };
                Curve roofProfile = triangle.ToNurbsCurve();
                Brep[] roofBreps = Brep.CreatePlanarBreps(new Curve[] { roofProfile }, doc.ModelAbsoluteTolerance);
                Brep frontCap = roofBreps[0];
                Point3d p4 = new Point3d(p1.X, p1.Y + depth, p1.Z);
                Point3d p5 = new Point3d(p2.X, p2.Y + depth, p2.Z);
                Point3d p6 = new Point3d(p3.X, p3.Y + depth, p3.Z);
                Polyline backTriangle = new Polyline { p4, p5, p6, p4 };
                Curve backProfile = backTriangle.ToNurbsCurve();
                Brep[] backCapBreps = Brep.CreatePlanarBreps(new Curve[] { backProfile }, doc.ModelAbsoluteTolerance);
                Brep backCap = backCapBreps[0];
                Vector3d extrusionDir = new Vector3d(0, depth, 0);
                Surface roofSurface = Surface.CreateExtrusion(roofProfile, extrusionDir);
                if (roofSurface == null)
                {
                    RhinoApp.WriteLine("Failed to create roof.");
                    return Result.Failure;
                }
                Brep houseRoof = roofSurface.ToBrep();
                Brep[] joinedRoof = Brep.JoinBreps(new Brep[] { houseRoof, frontCap, backCap }, doc.ModelAbsoluteTolerance);

                //Create door for house
                double doorWidth = width * 0.2;
                double doorHeight = height * 0.55;
                var doorX = insertionPt.X + (width - doorWidth) / 2;
                var doorZ = insertionPt.Z;
                Plane doorPlane = new Plane(new Point3d(doorX, insertionPt.Y - 0.01, doorZ), Vector3d.XAxis, Vector3d.ZAxis);
                Rectangle3d doorRect = new Rectangle3d(doorPlane, doorWidth, doorHeight);
                Brep[] doorBreps = Brep.CreatePlanarBreps(new Curve[] { doorRect.ToNurbsCurve() }, doc.ModelAbsoluteTolerance);
                if (doorBreps == null || doorBreps.Length == 0)
                {
                    RhinoApp.WriteLine("Failed to create door.");
                    return Result.Failure;
                }
                Brep doorBrep = doorBreps[0];
                conduit.PreviewDoor = doorBrep;
                doc.Views.Redraw();

                Box doorCutter = new Box(Plane.WorldXY,
                    new Interval(doorX, doorX + doorWidth),
                    new Interval(insertionPt.Y - 0.1, insertionPt.Y + 0.1),
                    new Interval(doorZ, doorZ + doorHeight));
                Brep doorCutterBrep = doorCutter.ToBrep();

                //Create left window
                double windowLWidth = width * 0.12;
                double windowLHeight = height * 0.3;
                var windowLX = insertionPt.X + (width - windowLWidth) / 6;
                var windowLZ = insertionPt.Z + height * 0.45;
                Plane windowLPlane = new Plane(new Point3d(windowLX, insertionPt.Y - 0.01, windowLZ), Vector3d.XAxis, Vector3d.ZAxis);
                Rectangle3d windowLRect = new Rectangle3d(windowLPlane, windowLWidth, windowLHeight);
                Brep[] windowLBreps = Brep.CreatePlanarBreps(new Curve[] { windowLRect.ToNurbsCurve() }, doc.ModelAbsoluteTolerance);
                if (windowLBreps == null || windowLBreps.Length == 0)
                {
                    RhinoApp.WriteLine("Failed to create left window.");
                    return Result.Failure;
                }
                Brep windowLBrep = windowLBreps[0];
                conduit.PreviewWindowL = windowLBrep;
                doc.Views.Redraw();

                Box windowCutterL = new Box(Plane.WorldXY,
                    new Interval(windowLX, windowLX + windowLWidth),
                    new Interval(insertionPt.Y - 0.1, insertionPt.Y + 0.1),
                    new Interval(windowLZ, windowLZ + windowLHeight));
                Brep windowCutterBrep = windowCutterL.ToBrep();

                //Create right window
                double windowRWidth = width * 0.12;
                double windowRHeight = height * 0.3;
                var windowRX = insertionPt.X + (width - windowRWidth) * .84;
                var windowRZ = insertionPt.Z + height * 0.45;
                Plane windowRPlane = new Plane(new Point3d(windowRX, insertionPt.Y - 0.01, windowRZ), Vector3d.XAxis, Vector3d.ZAxis);
                Rectangle3d windowRRect = new Rectangle3d(windowRPlane, windowRWidth, windowRHeight);
                Brep[] windowRBreps = Brep.CreatePlanarBreps(new Curve[] { windowRRect.ToNurbsCurve() }, doc.ModelAbsoluteTolerance);
                if (windowRBreps == null || windowRBreps.Length == 0)
                {
                    RhinoApp.WriteLine("Failed to create right window.");
                    return Result.Failure;
                }
                Brep windowRBrep = windowRBreps[0];
                conduit.PreviewWindowR = windowRBrep;
                doc.Views.Redraw();

                Box windowCutterR = new Box(Plane.WorldXY,
                    new Interval(windowRX, windowRX + windowRWidth),
                    new Interval(insertionPt.Y - 0.1, insertionPt.Y + 0.1),
                    new Interval(windowRZ, windowRZ + windowRHeight));
                Brep windowCutterRBrep = windowCutterR.ToBrep();

                //Cutting out the door and windows
                Brep[] cutters = new Brep[] { doorCutterBrep, windowCutterBrep, windowCutterRBrep };
                Brep[] result = Brep.CreateBooleanDifference(new Brep[] { bodyBrep }, cutters, doc.ModelAbsoluteTolerance);
                if (result != null && result.Length > 0)
                    bodyBrep = result[0];
                else
                    RhinoApp.WriteLine("Boolean difference failed — check that all Breps are closed solids");

                //Create chimney
                double chimneyWidth = width * 0.12;
                double chimneyDepth = depth * 0.12;
                double chimneyHeight = roofHeight * 1.2;
                var chimneyX = insertionPt.X + (width * 0.65);
                var chimneyY = insertionPt.Y + (depth * 0.35);
                var chimneyZ = insertionPt.Z + height;
                Plane chimneyPlane = Plane.WorldXY;
                chimneyPlane.Origin = new Point3d(chimneyX, chimneyY, chimneyZ);
                Box chimneyBox = new Box(chimneyPlane,
                    new Interval(0, chimneyWidth),
                    new Interval(0, chimneyDepth),
                    new Interval(0, chimneyHeight));
                Brep chimneyBrep = chimneyBox.ToBrep();
                conduit.PreviewChimney = chimneyBrep;
                doc.Views.Redraw();

                //Object colors
                var bodyAttrs = new Rhino.DocObjects.ObjectAttributes();
                bodyAttrs.ColorSource = Rhino.DocObjects.ObjectColorSource.ColorFromObject;
                bodyAttrs.ObjectColor = System.Drawing.Color.BlanchedAlmond;

                var roofAttrs = new Rhino.DocObjects.ObjectAttributes();
                roofAttrs.ColorSource = Rhino.DocObjects.ObjectColorSource.ColorFromObject;
                roofAttrs.ObjectColor = System.Drawing.Color.Firebrick;

                var chimneyAttrs = new Rhino.DocObjects.ObjectAttributes();
                chimneyAttrs.ColorSource = Rhino.DocObjects.ObjectColorSource.ColorFromObject;
                chimneyAttrs.ObjectColor = System.Drawing.Color.PapayaWhip;

                var doorAttrs = new Rhino.DocObjects.ObjectAttributes();
                doorAttrs.ColorSource = Rhino.DocObjects.ObjectColorSource.ColorFromObject;
                doorAttrs.ObjectColor = System.Drawing.Color.Aquamarine;

                var windowAttrs = new Rhino.DocObjects.ObjectAttributes();
                windowAttrs.ColorSource = Rhino.DocObjects.ObjectColorSource.ColorFromObject;
                windowAttrs.ObjectColor = System.Drawing.Color.SkyBlue;

                string confirm = "";
                var confirmResult = RhinoGet.GetString("Press Enter to confirm or Escape to cancel", true, ref confirm);
                if (confirmResult == Result.Cancel) return confirmResult;

                doc.Objects.AddBrep(bodyBrep, bodyAttrs);
                if (joinedRoof == null || joinedRoof.Length == 0)
                {
                    RhinoApp.WriteLine("Failed to join roof.");
                    return Result.Failure;
                }
                doc.Objects.AddBrep(joinedRoof[0], roofAttrs);
                doc.Objects.AddBrep(doorBrep, doorAttrs);
                doc.Objects.AddBrep(chimneyBrep, chimneyAttrs);
                doc.Objects.AddBrep(windowLBrep, windowAttrs);
                doc.Objects.AddBrep(windowRBrep, windowAttrs);
                doc.Views.Redraw();
                return Result.Success;
            }
            finally
            {
                conduit.Enabled = false;
                conduit.PreviewBody = null;
                conduit.PreviewRoof = null;
                conduit.PreviewDoor = null;
                conduit.PreviewWindowL = null;
                conduit.PreviewWindowR = null;
                conduit.PreviewChimney = null;
                doc.Views.Redraw();
            }
        }

        private Brep BuildBodyPreview(Point3d insertionPt, double width, double depth, double height)
        {
            Plane basePlane = Plane.WorldXY;
            basePlane.Origin = insertionPt;
            Box bodyBox = new Box(basePlane,
                new Interval(0, width),
                new Interval(0, depth),
                new Interval(0, height));
            return bodyBox.ToBrep();
        }

        private Brep BuildRoofPreview(Point3d insertionPt, double width, double depth, double height, double roofHeight, RhinoDoc doc)
        {
            double overhang = width * 0.17;
            Point3d p1 = new Point3d(insertionPt.X - overhang, insertionPt.Y, insertionPt.Z + height);
            Point3d p2 = new Point3d(insertionPt.X + width + overhang, insertionPt.Y, insertionPt.Z + height);
            Point3d p3 = new Point3d(insertionPt.X + width / 2.0, insertionPt.Y, insertionPt.Z + height + roofHeight);
            Polyline triangle = new Polyline { p1, p2, p3, p1 };
            Curve roofProfile = triangle.ToNurbsCurve();

            Brep[] frontCapBreps = Brep.CreatePlanarBreps(new Curve[] { roofProfile }, doc.ModelAbsoluteTolerance);
            if (frontCapBreps == null || frontCapBreps.Length == 0) return null;

            Point3d p4 = new Point3d(p1.X, p1.Y + depth, p1.Z);
            Point3d p5 = new Point3d(p2.X, p2.Y + depth, p2.Z);
            Point3d p6 = new Point3d(p3.X, p3.Y + depth, p3.Z);
            Polyline backTriangle = new Polyline { p4, p5, p6, p4 };
            Curve backProfile = backTriangle.ToNurbsCurve();

            Brep[] backCapBreps = Brep.CreatePlanarBreps(new Curve[] { backProfile }, doc.ModelAbsoluteTolerance);
            if (backCapBreps == null || backCapBreps.Length == 0) return null;

            Surface roofSurface = Surface.CreateExtrusion(roofProfile, new Vector3d(0, depth, 0));
            if (roofSurface == null) return null;

            Brep[] joinedRoof = Brep.JoinBreps(
                new Brep[] { roofSurface.ToBrep(), frontCapBreps[0], backCapBreps[0] },
                doc.ModelAbsoluteTolerance);
            return (joinedRoof != null && joinedRoof.Length > 0) ? joinedRoof[0] : null;
        }
    }
}
