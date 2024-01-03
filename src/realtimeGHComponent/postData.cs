using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using realtimeLogic;
using Rhino.Geometry;

namespace realtimeGHComponent
{
    public class postData : GH_Component
    {
        private Repository repository;
        public string sendingData;
        private string message;
        private string keyDirectory;
        private string url;

        /// <summary>
        /// Initializes a new instance of the postData class.
        /// </summary>
        public postData()
          : base("Firebase Post", "Post",
              "Description",
              "Strategist", "Firebase")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Data", "D", "Data to send", GH_ParamAccess.item);
            pManager.AddTextParameter("Key directory", "K", "Key", GH_ParamAccess.item);
            pManager.AddTextParameter("Database URL", "U", "URL", GH_ParamAccess.item);

            pManager[1].Optional = true;
            pManager[2].Optional = true;
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
            DA.GetData(0, ref message);
            DA.GetData(1, ref keyDirectory);
            DA.GetData(2, ref url);

            if (message == null)
            {
                return;
            }

            _ = Task.Run(() => repository.PostAsync(message));
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

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("D0C754CD-494B-4979-BEB3-CB42EF326889"); }
        }
    }
}