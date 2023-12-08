import firebase_admin
from firebase_admin import credentials
from firebase_admin import db
import keyboard
import threading

cred = credentials.Certificate("C:\\Users\\nshikada\\Documents\\GitHub\\table\\src\\sender\\key\\firebase_table-key.json")
firebase_admin.initialize_app(cred, {
    'databaseURL': "https://magpietable-default-rtdb.firebaseio.com/"
})

ref = db.reference("/")
# ordered_list = ref.order_by_key().limit_to_last(2).get()
# for key, value in ordered_list.items():
#     print(key, value)

# print(ref.get())

# Define a flag to signal the listener thread to stop
stop_listener = threading.Event()

# Defines the callback function for the listener that happens when the data changes.
def listener_event(event):
    print(event.data)

    # This is where we'll tell the grasshopper component to rerun on the main thread

    # If "q" is pressed on the keyboard, stop the listener.
    if keyboard.is_pressed("q"):
        stop_listener.set()
        print("Listener stopped.")

listener = ref.listen(listener_event)


# past_data = None

# while True:
#     incoming_data = ref.get()

#     if incoming_data == None:
#         continue

#     if incoming_data != past_data:
#         print(incoming_data)

#     past_data = incoming_data