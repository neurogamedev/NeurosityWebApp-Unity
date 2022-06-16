# NeurosityWebApp-Unity

![GitHub](https://img.shields.io/github/release/neuromodgames/NeurosityWebApp-Unity?style=for-the-badge)
![GitHub](https://img.shields.io/github/license/neuromodgames/NeurosityWebApp-Unity?style=for-the-badge)

Unity example project. Log into your Neurosity account, choose your device, and check device status, calm and focus scores.

![Unity-WebApp](https://user-images.githubusercontent.com/88777150/173224711-d086d6a3-ef74-4565-afd4-50b398a722c8.gif)

## What can you do with it?

You can use this example to connect to your Neurosity account and devices. It is up to you to build upon it and make your own apps and games!

## The shoulders of giants

This project was built upon the hard work of others. As such, it is also dependent on the scrutiny and update of the tools they so generously provide. Make sure to check their sites and repositories if you want to upgrade your own project or if you want to try your hand at older versions of Unreal Engine.

- Thanks to [AJ Keller](https://www.linkedin.com/in/andrewjaykeller/) and [Alex Castillo](https://www.linkedin.com/in/alexcas/) for coming up with the [Crown](https://neurosity.co/) hardware and the [Neurosity SDK](https://docs.neurosity.co/docs/overview). 

- Thanks to [Ryan Turney](https://github.com/ryanturney) for developing the [Notion SDK for Unity](https://github.com/ryanturney/notion-unity). 

- Thanks to Unity Technologies for making [Unity](https://unity.com//) accessible to everyone.

## Where is the fun stuff?

If you want to modify how the handlers communicate with Unity, like I did, go to the *Assets/Scripts/Notion-Unity/Handlers* folder. The *Types* folder right beside it will help you with what kind of information is received from Firebase.

If you're more into design and less into coding, you can find my contributions in the *Assets/Scripts/WebApp* folder. It's inundated with comments about how things work. The NotionInterfacer is the prime candidate for a singleton, since you may not want to relog every time you change scenes. Up to you, really. 

## Development Environment
* [Unity 2020.3.15 LTS](https://unity3d.com/get-unity/download/archive)
* [Firebase for Unity Authentication](https://developers.google.com/unity/packages#firebase_authentication)
* [Firebase for Unity Realtime Database](https://developers.google.com/unity/packages#firebase_realtime_database)
* [Json.NET by jilleJr](https://github.com/jilleJr/Newtonsoft.Json-for-Unity)
* [External Dependency Manager](https://developers.google.com/unity/packages#external_dependency_manager_for_unity)

Nota Bene: When updating to newer versions of Unity, be sure to update the external packages in *{Project Name}/Packages/* and in the `manifest.json` and `packages-lock.jason`.

## Using in Other Projects
Other apps will require your own Firebase project, you can follow [Firebase Documentation](https://firebase.google.com/docs/unity/setup) for help on that. There is a stub setup for this repo but any app developed using the Notion Unity SDK will eventually require you to setup your own Firebase account. This is currently a requirement as the Neurosity tech is built on top of Firebase and the Unity Firebase SDKs require `google-services.json` and `GoogleService-Into.plist` to be unique for each store app.

## To Do

- Fix Logout() warnings stemming from processes being interrupted mid-thread.
- Check if Builds work in iOS and Android environments.

## Frequently Asked Questions

**Q: Why are there no questions here?**

A: People haven't yet asked me questions to populate this section.
