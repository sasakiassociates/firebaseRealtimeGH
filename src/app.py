from flask import Flask
import ghhops_server as hs

import rhino3dm

# register hops app as middleware
app = Flask(__name__)
hops = hs.Hops(app)

# register hops endpoints
@hops.component(
    "/listen_to_firebase",
    name="Firebase Listener",
    description="A component that listens to a firebase database and returns the value of a given path",
    icon="C:\\Users\\nshikada\Desktop\\realtimeLogo.png",
    inputs=[
        hs.HopsString("url", "URL", "The url of the Firebase database"),
        hs.HopsString("credentials_path", "Credentials Path", "The path to the credentials file"),
    ],
    outputs=[
        hs.HopsString("value", "Value", "The json value of the given path"),
    ],
)
def listen_to_firebase(url, credentials_path):
    import firebase_admin
    from firebase_admin import credentials
    from firebase_admin import db

    cred = credentials.Certificate(credentials_path)
    firebase_admin.initialize_app(cred, {
        'databaseURL': url
    })

    ref = db.reference("/")
    return ref.get()

# run the app
if __name__ == "__main__":
    app.run(debug=True)