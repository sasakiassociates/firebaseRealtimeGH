﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
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
        string destination = "";
        List<object> incomingData = new List<object>();
        List<object> previousData = new List<object>();
        List<object> dataToSend = new List<object>();
        bool waiting = false;

        CancellationTokenSource CancellationTokenSource;
        CancellationToken cancellationToken;

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
            CancellationTokenSource = new CancellationTokenSource();
            cancellationToken = CancellationTokenSource.Token;
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Data", "Data", "The data to send to the Firebase database", GH_ParamAccess.list);
            pManager.AddTextParameter("Destination", "Destination", "The specific location(s) in the Firebase database to send the data in the format 'cad_points' or 'examplefolder/subfolder/cad_points' WARNING: this overrides any data in the designated location", GH_ParamAccess.item);
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
            previousData.Clear();
            incomingData.Clear();
            dataToSend.Clear();

            DA.GetDataList("Data", incomingData);
            DA.GetData("Destination", ref destination);
            DA.GetData("Key directory", ref pathToKeyFile);
            DA.GetData("Database URL", ref firebaseUrl);

            if (pathToKeyFile != null && firebaseUrl != null)
            {
                _repository.OverrideLocalConnection(pathToKeyFile, firebaseUrl);
            }

            // Get the value from each of the objects in the incoming data
            for (int i = 0; i < incomingData.Count; i++)
            {
                dataToSend.Add(incomingData[i].GetType().GetProperty("Value").GetValue(incomingData[i]));
            }

            if (!previousData.Equals(dataToSend))
            {
                try
                {
                    _ = _repository.PutAsync(destination, dataToSend);
                }
                catch (Exception e)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Are you using the credentials component?" + e.ToString());
                }
                previousData = incomingData;
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

        public async Task SendData()
        {
            if (_repository.authorized == false)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Not authorized to interact with Firebase. Are you using the credentials component?");
                //_repository.WaitForConnection(cancellationToken);
                return;
            }

            /*if (dataToSend.Count == 1)
            {
                object data = dataToSend[0];
                foreach (string destination in destinations)
                {
                    await _repository.PutAsync(data, destination);
                }
            }
            else
            {
                foreach (string destination in destinations)
                {
                    // TODO this seems to send excess information along with the data, need to figure out how to send only the data
                    await _repository.PutAsync(dataToSend, destination);
                }
            }*/
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
        /// Append additional menu items to the main component menu.
        /// </summary>
        /// <param name="document"></param>
        public override void AppendAdditionalMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);
            // Add a cancel option to the menu to trigger the cancellation token
            Menu_AppendItem(menu, "Cancel", Cancel);
        }

        public void Cancel(object sender, EventArgs e)
        {
            CancellationTokenSource.Cancel();

            // Rerun the component
            Rhino.RhinoApp.InvokeOnUiThread((Action)delegate
            {
                this.ExpireSolution(true);
            });
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