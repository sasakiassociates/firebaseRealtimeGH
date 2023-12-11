using System;
using System.Threading.Tasks;
using Firebase.Database;
using Firebase.Database.Query;
using Google.Apis.Auth.OAuth2;
using NUnit.Framework;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace realtimeLogic
{
    public class Repository
    {
        public static readonly Repository Instance = new Repository();
        private const string FirebaseUrl = "https://magpietable-default-rtdb.firebaseio.com/";
        private readonly FirebaseClient _firebaseClient;
        private const string pathToJsonFile = @"C:\Users\nshikada\Documents\GitHub\firebaseRealtimeGH\keys\firebase_table-key.json";
        public List<Marker> Markers { get; set; }

        private Repository()
        {
            _firebaseClient = new FirebaseClient(FirebaseUrl, new FirebaseOptions { AuthTokenAsyncFactory = () => GetAccessToken(), AsAccessToken = true });
            Markers = new List<Marker>();
        }

        public void TestRetrieve()
        {
            var result = _firebaseClient.Child("markers").
                OnceAsync<Marker>();
            
            foreach (var item in result.Result)
            {
                Console.WriteLine(item.Key);
                Console.WriteLine(item.Object.id);
                Console.WriteLine(item.Object.x);
                Console.WriteLine(item.Object.y);
                Console.WriteLine(item.Object.rotation);

            }
        }

        public void TestSubscribe()
        {
            // Opens a new thread observing the database
            var observable = _firebaseClient.Child("markers").AsObservable<Marker>().Subscribe(dbEventHandler => onNewData(dbEventHandler));

            for (int i = 0; i < 3; i++)
            {
                System.Threading.Thread.Sleep(5000);

                Console.WriteLine(Markers.Count);
            }
            Console.WriteLine(Markers.Count);
            observable.Dispose();
        }

        private void onNewData(Firebase.Database.Streaming.FirebaseEvent<Marker> eventSource)
        {
            if (eventSource.EventType == Firebase.Database.Streaming.FirebaseEventType.InsertOrUpdate)
            {
                if (Markers.Exists(x => x.uuid == eventSource.Key))
                {
                    var index = Markers.FindIndex(x => x.uuid == eventSource.Key);
                    Markers[index] = eventSource.Object;
                }
                else
                {
                    Marker marker = eventSource.Object;
                    marker.uuid = eventSource.Key;
                    Markers.Add(marker);

                    Console.WriteLine("New marker added");
                    Console.WriteLine(eventSource.Object.uuid);
                    Console.WriteLine(eventSource.Object.id);
                    Console.WriteLine(eventSource.Object.x);
                    Console.WriteLine(eventSource.Object.y);
                }
            }
            else if (eventSource.EventType == Firebase.Database.Streaming.FirebaseEventType.Delete)
            {
                if (Markers.Exists(x => x.uuid == eventSource.Key))
                {
                    var index = Markers.FindIndex(x => x.uuid == eventSource.Key);
                    Markers.RemoveAt(index);
                }
            }
        }

        private async Task<string> GetAccessToken()
        {
            var credential = GoogleCredential.FromFile(pathToJsonFile).CreateScoped(new string[] {
                "https://www.googleapis.com/auth/userinfo.email",
                "https://www.googleapis.com/auth/firebase.database"
            });

            ITokenAccess c = credential as ITokenAccess;
            return await c.GetAccessTokenForRequestAsync();
        }

    }
}
