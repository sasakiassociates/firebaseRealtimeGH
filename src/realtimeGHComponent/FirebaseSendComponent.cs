using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using realtimeLogic;
using Rhino.Geometry;

namespace realtimeGHComponent
{
    public class FirebaseSendComponent : GH_Component
    {
        Repository _repository;
        string firebaseUrl;
        string pathToKeyFile;
        List<string> destinations = new List<string>();
        List<object> incomingData = new List<object>();
        List<object> previousData = new List<object>();

        /// <summary>
        /// Initializes a new instance of the SendFirebase class.
        /// </summary>
        public FirebaseSendComponent()
          : base("Firebase Send", "Send",
              "Send things to a Firebase repository",
              "Strategist", "Firebase")
        {
            Attributes = new FirebaseSendAttributes(this);
            _repository = new Repository();
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Data", "Data", "The data to send to the Firebase database", GH_ParamAccess.list);
            pManager.AddTextParameter("Destinations", "Destinations", "The specific location(s) in the Firebase database to send the data in the format 'cad_points' or 'examplefolder/subfolder/cad_points' WARNING: this overrides any data in the designated location", GH_ParamAccess.list);
            pManager.AddTextParameter("Key directory", "Key directory", "The directory of the key file for the Firebase database", GH_ParamAccess.item);
            pManager.AddTextParameter("Database URL", "Database URL", "The URL of the Firebase database", GH_ParamAccess.item);

            pManager[2].Optional = true;
            pManager[3].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            incomingData.Clear();

            DA.GetDataList("Data", incomingData);
            DA.GetDataList(1, destinations);
            DA.GetData("Key directory", ref pathToKeyFile);
            DA.GetData("Database URL", ref firebaseUrl);

            if (pathToKeyFile != null && firebaseUrl != null)
            {
                _repository.OverrideLocalConnection(pathToKeyFile, firebaseUrl);
            }

            if (incomingData != previousData)
            {
                SendData();
            }
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        public void SendData()
        {
            previousData = incomingData;
            foreach (string destination in destinations)
            {
                _ = _repository.PutAsync(incomingData, destination);
            }
        }

        public class FirebaseSendAttributes : GH_ComponentAttributes
        {
            public FirebaseSendAttributes(IGH_Component component) : base(component) { }

            private Rectangle SendButtonBounds { get; set; }

            protected override void Layout()
            {
                base.Layout();
                Rectangle rec0 = GH_Convert.ToRectangle(Bounds);
                rec0.Height += 34;
                Bounds = rec0;

                SendButtonBounds = new Rectangle((int)(Bounds.X + 10), (int)(Bounds.Bottom - 30), (int)(Bounds.Width - 20), 20);
            }

            protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
            {
                base.Render(canvas, graphics, channel);

                if (channel == GH_CanvasChannel.Objects)
                {
                    GH_Capsule button = GH_Capsule.CreateTextCapsule(SendButtonBounds, SendButtonBounds, GH_Palette.Black, "Send", 2, 0);
                    button.Render(graphics, Selected, Owner.Locked, false);
                    button.Dispose();
                }
            }

            public override GH_ObjectResponse RespondToMouseUp(GH_Canvas sender, GH_CanvasMouseEvent e)
            {
                if (e.Button == MouseButtons.Left && SendButtonBounds.Contains(System.Drawing.Point.Round(e.CanvasLocation)))
                {
                    // Send the current data to the Firebase database
                    ((FirebaseSendComponent)Owner).SendData();

                    return GH_ObjectResponse.Handled;
                }
                return base.RespondToMouseUp(sender, e);
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("6C1B41FE-1876-448F-B878-E0CC881D7600"); }
        }
    }
}