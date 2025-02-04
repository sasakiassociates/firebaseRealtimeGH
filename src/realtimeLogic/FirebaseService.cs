using Firebase.Database.Streaming;
using Firebase.Database;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace realtimeLogic
{
    public interface IFirebaseService
    {
        Task PutAsync(string path, object data);
        Task DeleteAsync(string path);
        Task<string> GetAsync(string path);
        IObservable<FirebaseEvent<JToken>> Observe(string path);
    }

    public class FirebaseService : IFirebaseService
    {
        private readonly FirebaseClient _client;

        public FirebaseService(FirebaseClient client)
        {
            _client = client;
        }

        public async Task PutAsync(string path, object data)
        {
            // Serialize the object to JSON
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(data);
            await _client.Child(path).PutAsync(json);
        }

        public async Task DeleteAsync(string path)
        {
            await _client.Child(path).DeleteAsync();
        }

        public async Task<string> GetAsync(string path)
        {
            return await _client.Child(path).OnceAsJsonAsync();
        }

        public IObservable<FirebaseEvent<JToken>> Observe(string path)
        {
            return _client.Child(path).AsObservable<JToken>();
        }
    }
}
