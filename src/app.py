from flask import Flask
import ghhops_server as hs

# register hops app as middleware
app = Flask(__name__)
hops = hs.Hops(app)

# register hops endpoints
@hops.component(
    "/hello",
    name="Hello",
    description="A simple component that says hello.",
    icon="mdi-comment-text-outline",
)
def hello(name):
    return "Hello {}!".format(name)