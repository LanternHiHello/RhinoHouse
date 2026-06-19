using Rhino.Display;
using Rhino.Geometry;
using System.Drawing;

namespace StullerHouse
{

    public class HouseDisplayConduit : DisplayConduit
    {

        public Brep PreviewBody { get; set; }
        public Brep PreviewRoof { get; set; }
        public Brep PreviewWindowR { get; set; }
        public Brep PreviewWindowL { get; set; }
        public Brep PreviewDoor { get; set; }
        public Brep PreviewChimney { get; set; }


        protected override void CalculateBoundingBox(CalculateBoundingBoxEventArgs e)
        {
            if (PreviewBody != null)
                e.IncludeBoundingBox(PreviewBody.GetBoundingBox(false));
            if (PreviewRoof != null)
                e.IncludeBoundingBox(PreviewRoof.GetBoundingBox(false));
            if (PreviewWindowR != null)
                e.IncludeBoundingBox(PreviewWindowR.GetBoundingBox(false));
            if (PreviewWindowL != null)
                e.IncludeBoundingBox(PreviewWindowL.GetBoundingBox(false));
            if (PreviewDoor != null)
                e.IncludeBoundingBox(PreviewDoor.GetBoundingBox(false));
            if (PreviewChimney != null)
                e.IncludeBoundingBox(PreviewChimney.GetBoundingBox(false));
            base.CalculateBoundingBox(e);
        }


        protected override void PostDrawObjects(DrawEventArgs e)
        {

            if (PreviewBody != null)
                e.Display.DrawBrepShaded(PreviewBody, new
                DisplayMaterial(Color.DarkSeaGreen, 0.5));
            if (PreviewRoof != null)
                e.Display.DrawBrepShaded(PreviewRoof, new
                DisplayMaterial(Color.PeachPuff, 0.5));
            if (PreviewWindowR != null)
                e.Display.DrawBrepShaded(PreviewWindowR, new
                DisplayMaterial(Color.AliceBlue, 0.5));
            if (PreviewWindowL != null)
                e.Display.DrawBrepShaded(PreviewWindowL, new
                DisplayMaterial(Color.AliceBlue, 0.5));
            if (PreviewDoor != null)
                e.Display.DrawBrepShaded(PreviewDoor, new
                DisplayMaterial(Color.PapayaWhip, 0.5));
            if (PreviewChimney != null)
                e.Display.DrawBrepShaded(PreviewChimney, new
                DisplayMaterial(Color.NavajoWhite, 0.5));
            base.PostDrawObjects(e);
        }

    }
}
