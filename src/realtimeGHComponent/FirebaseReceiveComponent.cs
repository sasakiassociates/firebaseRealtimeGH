using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using realtimeLogic;
using Firebase.Database;
using Firebase.Database.Query;
using Google.Apis.Auth.OAuth2;
using Newtonsoft.Json;
using GrasshopperAsyncComponent;

namespace firebaseRealtime
{
    public class FirebaseReceiveComponent : GH_Component
    {
        private CancellationTokenSource cancellationTokenSource;
        private CancellationToken cancellationToken;
        private Repository repository;
        public string incomingData;
        private bool listening = false;
        public List<string> targetFolders = new List<string>();

        public string keyDirectory = "";
        public string url = "";
        

        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public FirebaseReceiveComponent()
          : base("Firebase Receive", "Receive",
            "Description",
            "Strategist", "Firebase")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Key directory", "K", "Key. Optional if the Repository is already connected in this sketch", GH_ParamAccess.item);
            pManager.AddTextParameter("Database URL", "U", "URL", GH_ParamAccess.item);
            // A list of folders to watch, each in the format "parent/child/folder1"
            pManager.AddTextParameter("Target Folders", "F", "Target Folders", GH_ParamAccess.list);
            // TODO make the last input optional and add a default behavior to watch the entire database
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Incoming Data", "Data", "Incoming Data", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.GetData("Key directory", ref keyDirectory);
            DA.GetData("Database URL", ref url);
            DA.GetDataList("Target Folders", targetFolders);

            if (listening == false)
            {
                cancellationTokenSource = new CancellationTokenSource();
                cancellationToken = cancellationTokenSource.Token;
                
                repository = Repository.GetInstance();

                _ = Task.Run(() => ListenThread(cancellationToken));
                listening = true;
            }

            DA.SetData("Incoming Data", incomingData);
        }

        private async Task ListenThread(CancellationToken cancellationToken)
        {
            if (repository.connected == false)
            {
                repository.Connect(keyDirectory, url);
            }

            await repository.Setup(targetFolders);

            while (!cancellationToken.IsCancellationRequested)
            {
                incomingData = repository.WaitForUpdate(cancellationToken);

                // Rerun the component
                Rhino.RhinoApp.InvokeOnUiThread((Action)delegate
                {
                    this.ExpireSolution(true);
                });
            }

            await repository.Teardown();
        }

        /// <summary>
        /// Append additional menu items to the main component menu.
        /// </summary>
        /// <param name="document"></param>
        public override void AppendAdditionalMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);
            // Add a cancel option to the menu to trigger the cancellation token
            Menu_AppendItem(menu, "Cancel", CancelClicked);
            Menu_AppendItem(menu, "Restart Listener", RestartClicked);
        }

        private void RestartClicked(object sender, EventArgs e)
        {
            cancellationTokenSource.Cancel();
            listening = false;
        }

        private void CancelClicked(object sender, EventArgs e)
        {
            cancellationTokenSource.Cancel();
        }

        public override void RemovedFromDocument(GH_Document document)
        {
            cancellationTokenSource.Cancel();
            base.RemovedFromDocument(document);
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// You can add image files to your project resources and access them like this:
        /// return Resources.IconForThisComponent;
        /// </summary>
        protected override System.Drawing.Bitmap Icon => null;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("0b6f7940-e0ce-4190-989d-c3497b3fb7ed");
    }
}