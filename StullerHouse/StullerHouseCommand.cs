using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
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

            Point3d insertionPt = Point3d.Origin;

            var rc = RhinoGet.GetPoint("House insertion point", true, out insertionPt);
            if (rc != Result.Success) return rc;

            double width = 10.0, depth = 8.0, height = 6.0, roofHeight = 5.0;

            var wd = RhinoGet.GetNumber("House width", true, ref width);
            if (wd != Result.Success) return wd;

            var dp = RhinoGet.GetNumber("House depth", true, ref depth);
            if (dp != Result.Success) return dp;

            var ht = RhinoGet.GetNumber("House wall height", true, ref height);
            if (ht != Result.Success) return ht;

            var rh = RhinoGet.GetNumber("Roof height", true, ref roofHeight);
            if (rh != Result.Success) return rh;

            if (width <= 0 || depth <= 0 || height <= 0 || roofHeight <= 0)
                {
                    RhinoApp.WriteLine("All dimensions must be greater than zero.");
                    return Result.Failure;
                }

            //Create the rectangular base for the walls
            Plane basePlane = Plane.WorldXY;
            basePlane.Origin = insertionPt;
            Box bodyBox = new Box(basePlane,
                new Interval(0, width),
                new Interval(0, depth),
                new Interval(0, height));
            Brep bodyBrep = bodyBox.ToBrep();

            //Adding in overhang so the roof will hang over the sides of the house
            double overhang = width * .17;
            
            // Create the points for the gable roof (also is coordinates for front of roof)
            Point3d p1 = new Point3d(insertionPt.X - overhang, insertionPt.Y, insertionPt.Z + height);
            Point3d p2 = new Point3d(insertionPt.X + width + overhang, insertionPt.Y, insertionPt.Z + height);
            Point3d p3 = new Point3d(insertionPt.X + width / 2.0, insertionPt.Y, insertionPt.Z + height + roofHeight);
            
            // Create the triangular roof profile curve
            Polyline triangle = new Polyline { p1, p2, p3, p1 };
            Curve roofProfile = triangle.ToNurbsCurve();
            Brep[] roofBreps = Brep.CreatePlanarBreps(new Curve[] {roofProfile}, doc.ModelAbsoluteTolerance);
            Brep frontCap = roofBreps[0];

            //Create points for creation of back of roof
            Point3d p4 = new Point3d(p1.X, p1.Y + depth, p1.Z);
            Point3d p5 = new Point3d(p2.X, p2.Y + depth, p2.Z);
            Point3d p6 = new Point3d(p3.X, p3.Y + depth, p3.Z);
            Polyline backTriangle = new Polyline { p4, p5, p6, p4 };
            Curve backProfile = backTriangle.ToNurbsCurve();

            Brep[] backCapBreps = Brep.CreatePlanarBreps(new Curve[] { backProfile },
            doc.ModelAbsoluteTolerance);
            Brep backCap = backCapBreps[0];
            

            // Extrude the triangle along the length of the house
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
            
            Plane doorPlane = new Plane(new Point3d(doorX, insertionPt.Y, doorZ), Vector3d.XAxis, Vector3d.ZAxis);
            Rectangle3d doorRect = new Rectangle3d(doorPlane, doorWidth, doorHeight);
            Brep[] doorBreps = Brep.CreatePlanarBreps(new Curve[] { doorRect.ToNurbsCurve() }, doc.ModelAbsoluteTolerance);
            if (doorBreps == null || doorBreps.Length == 0)
                {
                    RhinoApp.WriteLine("Failed to create door.");
                    return Result.Failure;
                }
            Brep doorBrep = doorBreps[0];


            //Create  Left windows for house
            double windowAWidth = width * 0.12;
            double windowAHeight = height * 0.3;
            //Making this be between the door and side of house
            var windowAX = insertionPt.X + (width - windowAWidth) / 6;

            var windowAZ = insertionPt.Z + height * 0.45;

            Plane windowAPlane = new Plane(new Point3d(windowAX, insertionPt.Y, windowAZ), Vector3d.XAxis, Vector3d.ZAxis);
            Rectangle3d windowARect = new Rectangle3d(windowAPlane, windowAWidth, windowAHeight);
            Brep[] windowABreps = Brep.CreatePlanarBreps(new Curve[] { windowARect.ToNurbsCurve() }, doc.ModelAbsoluteTolerance);
            if (windowABreps == null || windowABreps.Length == 0)
            {
                RhinoApp.WriteLine("Failed to create door.");
                return Result.Failure;
            }
            Brep windowABrep = windowABreps[0];


            //Create Rightsode windows for house
            double windowBWidth = width * 0.12;
            double windowBHeight = height * 0.3;
            //Making this be between the door and side of house
            var windowBX = insertionPt.X + (width - windowBWidth) * .84;

            var windowBZ = insertionPt.Z + height * 0.45;

            Plane windowBPlane = new Plane(new Point3d(windowBX, insertionPt.Y, windowBZ), Vector3d.XAxis, Vector3d.ZAxis);
            Rectangle3d windowBRect = new Rectangle3d(windowBPlane, windowBWidth, windowBHeight);
            Brep[] windowBBreps = Brep.CreatePlanarBreps(new Curve[] { windowBRect.ToNurbsCurve() }, doc.ModelAbsoluteTolerance);
            if (windowBBreps == null || windowBBreps.Length == 0)
            {
                RhinoApp.WriteLine("Failed to create door.");
                return Result.Failure;
            }
            Brep windowBBrep = windowBBreps[0];



            //Create chimney for house
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


            //Adding in colors for the fun of it:
            //House body color
            var bodyAttrs = new Rhino.DocObjects.ObjectAttributes();
            bodyAttrs.ColorSource = Rhino.DocObjects.ObjectColorSource.ColorFromObject;
            bodyAttrs.ObjectColor = System.Drawing.Color.BlanchedAlmond;

            //Roof color
            var roofAttrs = new Rhino.DocObjects.ObjectAttributes();
            roofAttrs.ColorSource = Rhino.DocObjects.ObjectColorSource.ColorFromObject;
            roofAttrs.ObjectColor = System.Drawing.Color.Firebrick;

            //Chimney color
            var chimneyAttrs = new Rhino.DocObjects.ObjectAttributes();
            chimneyAttrs.ColorSource = Rhino.DocObjects.ObjectColorSource.ColorFromObject;
            chimneyAttrs.ObjectColor = System.Drawing.Color.PapayaWhip;

            //Door color
            var doorAttrs = new Rhino.DocObjects.ObjectAttributes();
            doorAttrs.ColorSource = Rhino.DocObjects.ObjectColorSource.ColorFromObject;
            doorAttrs.ObjectColor = System.Drawing.Color.Aquamarine;

            //Windows color
            var windowAttrs = new Rhino.DocObjects.ObjectAttributes();
            windowAttrs.ColorSource = Rhino.DocObjects.ObjectColorSource.ColorFromObject;
            windowAttrs.ObjectColor = System.Drawing.Color.SkyBlue;


            doc.Objects.AddBrep(bodyBrep, bodyAttrs);
            if (joinedRoof == null || joinedRoof.Length == 0)
            {
                RhinoApp.WriteLine("Failed to join roof.");
                return Result.Failure;
            }
            doc.Objects.AddBrep(joinedRoof[0], roofAttrs);
            doc.Objects.AddBrep(doorBrep, doorAttrs);
            doc.Objects.AddBrep(chimneyBrep, chimneyAttrs);
            doc.Objects.AddBrep(windowABrep, windowAttrs);
            doc.Objects.AddBrep(windowBBrep, windowAttrs);
            doc.Views.Redraw();
            return Result.Success;
        }

    }
}
