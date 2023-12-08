using Grasshopper;
using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace firebaseRealtime
{
    public class firebaseRealtimeInfo : GH_AssemblyInfo
    {
        public override string Name => "firebaseRealtime";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => null;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "";

        public override Guid Id => new Guid("7f2b7cfc-7089-4531-9232-12effc108504");

        //Return a string identifying you or your company.
        public override string AuthorName => "";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "";
    }
}