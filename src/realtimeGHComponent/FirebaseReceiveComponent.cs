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
using System.Linq;

namespace firebaseRealtime
{
    public class FirebaseReceiveComponent : GH_Component
    {
        private CancellationTokenSource cancellationTokenSource;
        private CancellationToken cancellationToken;
        private Repository repository;

        public string incomingData;
        private bool listening = false;
        
        // Inputs
        public List<string> targetNodes = new List<string>();
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
            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;
            repository = new Repository(cancellationToken);
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            // A list of folders to watch, each in the format "parent/child/folder1"
            pManager.AddTextParameter("Target Folders", "F", "Target Folders", GH_ParamAccess.list);
            // TODO make the last input optional and add a default behavior to watch the entire database
            pManager.AddTextParameter("Key directory", "K", "Key. Optional if the Repository is already connected in this sketch", GH_ParamAccess.item);
            pManager.AddTextParameter("Database URL", "U", "URL", GH_ParamAccess.item);

            // Credentials can be specified here or added to the credentials component
            pManager[0].Optional = true;
            pManager[1].Optional = true;
            pManager[2].Optional = true;
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
            List<string> incomingTargetFolders = new List<string>();
            string incomingDirectory = "";
            string incomingUrl = "";

            DA.GetDataList("Target Folders", incomingTargetFolders);
            DA.GetData("Key directory", ref incomingDirectory);
            DA.GetData("Database URL", ref incomingUrl);

            // If the incoming directory and url are different from the current directory and url, update the repository
            if (incomingDirectory != "" && incomingUrl != "" && incomingDirectory != keyDirectory && incomingUrl != url)
            {
                keyDirectory = incomingDirectory;
                url = incomingUrl;
                repository.OverrideLocalConnection(keyDirectory, url);
            }

            // If the listener thread is not running, start it
            if (listening == false)
            {
                cancellationTokenSource = new CancellationTokenSource();
                cancellationToken = cancellationTokenSource.Token;

                repository.SetTargetNodes(incomingTargetFolders);
                repository.ReloadConnection();
                
                _ = Task.Run(() => ListenThread(cancellationToken));
                listening = true;
            }
            // If the incoming target folders are different from the current target nodes, update the target nodes
            else if (!incomingTargetFolders.SequenceEqual(targetNodes))
            {
                // This keeps running whenever a new target folder is added
                targetNodes = incomingTargetFolders;
                repository.SetTargetNodes(targetNodes);
            }

            if (!repository.connected)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Not connected to Firebase. Check your credentials and URL. Add the Credentials component to the sketch or provide credentials here");
            }

            DA.SetData("Incoming Data", incomingData);
        }

        private void ListenThread(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {

                incomingData = repository.WaitForUpdate();

                // Rerun the component
                Rhino.RhinoApp.InvokeOnUiThread((Action)delegate
                {
                    this.ExpireSolution(true);
                });
            }

            repository.Teardown();
        }

        /// <summary>
        /// Append additional menu items to the main component menu.
        /// </summary>
        /// <param name="document"></param>
        public override void AppendAdditionalMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);
            // Add a cancel option to the menu to trigger the cancellation token
            Menu_AppendItem(menu, "Restart Listener", RestartClicked);
        }

        private void RestartClicked(object sender, EventArgs e)
        {
            cancellationTokenSource.Cancel();
            listening = false;

            // Rerun the component
            Rhino.RhinoApp.InvokeOnUiThread((Action)delegate
            {
                this.ExpireSolution(true);
            });
        }

        public override void RemovedFromDocument(GH_Document document)
        {
            cancellationTokenSource.Cancel();
            if (repository != null)
            {
                repository.Teardown();
            }
            base.RemovedFromDocument(document);
        }

        // Run this when the document is closed
        public override void DocumentContextChanged(GH_Document document, GH_DocumentContext context)
        {
            if (context == GH_DocumentContext.Close)
            {
                if (cancellationTokenSource != null)
                {
                    cancellationTokenSource.Cancel();
                    repository.Teardown();
                }
            }
            base.DocumentContextChanged(document, context);
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