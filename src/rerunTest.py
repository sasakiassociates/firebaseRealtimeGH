import threading
import time
from flask import Flask
import ghhops_server as hs

import rhino3dm

# register hops app as middleware
app = Flask(__name__)
hops = hs.Hops(app)

# register hops endpoints
@hops.component(
    "/main",
    name="Rerun component test",
    description="A component that reruns the main Rhino component",
    icon="C:\\Users\\nshikada\Desktop\\realtimeLogo.png",
    inputs=[
    ],
    outputs=[
        hs.HopsString("value", "Value", "The number of times the component has been rerun"),
    ],
)

def main():
    # Create a new instance of the Runner class
    runner = Runner()
    # Start the runner
    runner.run_component_on_ui_thread()

class Runner():
    def __init__(self) -> None:
        self.i = 0

    def rerun_component_loop(self):
        print(self.i)
        self.i += 1
        time.sleep(1000)

    def run_component_on_ui_thread(self):
        # Check if we are already on the main UI thread
        if Rhino.RhinoApp.MainApplicationWindow.InvokeRequired:
            # If not, use Invoke to run the function on the main UI thread
            Rhino.RhinoApp.MainApplicationWindow.Invoke(self.rerun_component_loop)
        else:
            # If already on the main UI thread, execute the function directly
            self.rerun_component_loop()

thread = threading.Thread(target=main)
thread.start()