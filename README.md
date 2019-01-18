# Unity plugin V3

This plugin has been developed and tested using Unity 2018.3. Using ealier versions of Unity will break the prefabs, but all scripts are compatible down to Unity 5.6

## Documentation
Follow the documentation available on [Notion](https://www.notion.so/vrtracker/VR-Tracker-Unity-Plugin-V3-5aad172e672944c1a4f47a5ac2c8e72d)

A Youtube playlist is available [here](https://www.youtube.com/playlist?list=PLLrMTGcyUCRVX20KkwyIsl2GJgY3aI7fc)


## Demonstration
The following Git provides a full example to integrate the Plugin in a Game allowing with a set of explanatory video : [Example](https://github.com/VR-Tracker/Unity-Example)


## Import in your Unity project
### Download
You can download the plugin as a zip that you will have to extract in the "Asset" folder of your Unity project

### Git
To keep the plugin in synch with the latest development, you can use the git directly. They are two cases :
1. Your project is *NOT* in another Git
Then you can simply clone this Git in you Unity Project "Asset" folder using the following Git command
```
git clone https://github.com/VR-Tracker/Unity-Plugin.git VRTracker
```
2. Your project already has its own Git, you need to add this Git as a submodule. To do so go to the Unity Project "Asset" folder and enter the following git command :
```
git submodule add https://github.com/VR-Tracker/Unity-Plugin.git VRTracker
```
