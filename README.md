# firebaseRealtimeGH
A python script that can be called by Rhino Hops to access a Firebase Realtime Database

# Issue
Integrating Firebase into Rhinoceros Grasshopper without REST has several issues:
- Rhino does not natively support CPython libraries such as the one used for the Firebase Realtime Database API
- C# does not have access to the Realtime Database via the `Firebase Admin SDK` (image shown here)
![Google's Firebase Admin SDK compatible languages chart](media\firebaseCompatabilityChart.png)
- Rhino's new `Hops` program relies on REST calls between the program and a locally hosted server