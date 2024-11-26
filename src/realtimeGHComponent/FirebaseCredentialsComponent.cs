using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using realtimeLogic;
using Rhino.Geometry;

using StrategistLibrary;

namespace realtimeGHComponent
{
    public class FirebaseCredentialsComponent : GH_Component
    {
        public string database_url;
        public string key_directory;

        private FirebaseConnectionManager credentials;

        /// <summary>
        /// Initializes a new instance of the FirebaseCredentials class.
        /// </summary>
        public FirebaseCredentialsComponent()
          : base("Firebase Credentials", "Credentials",
              "A component to specify and authenticate the Firebase components",
              "Strategist", "Firebase")
        {
            StrategistLogger.LogComponentAddedToCanvas(this);
            credentials = FirebaseConnectionManager.GetInstance();
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Key directory", "K", "Key. Optional if the Repository is already connected in this sketch", GH_ParamAccess.item);
            pManager.AddTextParameter("Database URL", "U", "URL", GH_ParamAccess.item);
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
            DA.GetData("Key directory", ref key_directory);
            DA.GetData("Database URL", ref database_url);

            credentials.SetSharedCredentials(key_directory, database_url);
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
            get { return new Guid("6E8EA39A-88B5-4DAC-8BCB-55C4BF9C7B57"); }
        }
    }
}