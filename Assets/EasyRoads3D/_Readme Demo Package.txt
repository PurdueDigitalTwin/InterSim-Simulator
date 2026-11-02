
This package includes the EasyRoads3D demo scene and EasyRoads3D Pro tool.

It is recommended to import the package in a new project first. Assets that you like tho use can be exported / imported.  Or the package can be imported in your project if you want to use all assets.

The older v3.1 scene includes featured zones with links to tutorials on our website, http://easyroads3d.com/tutorials.php. The v3.1 scene is not optimized for URP and HDRP, suport for these render pipelines were added in v3.2.

This demo package has the v3.3 beta imported which includes the Flex Connector. This new v3.3 crossing type supports flexible angles and different road types. It is used in the new v3.2+ demo scene which is work in progress and will eventually replace the v3.1 demo scene.

The road network state in both scenes is in Build Mode. To switch to Edit Mode and explore the road objects, select the road network object in the hierarchy and press the "Back to Edit Mode" button in the Inspector.

The demo project also includes a variety of different road types and side objects which can be used in your own personal and commercial projects except for asset store packages. 

Please contact us for permission to use these assets in Unity asset store packages or for example open source Unity projects.   

The terrain object in both scenes includes data for tree prototypes. Some of the tree prefabs are copyrighted and used to be available on the Asset Store for free. Unfortunately this is no longer the case. The tree data is still available, trees will be placed accordingly after assigning a tree prefab to the Tree Prototypes that appear as "Missing". 

Forum: http://forum.unity3d.com/threads/easyroads3d-v3-the-upcoming-new-road-system.229327/
Website: http://www.easyroads3d.com
Support: info@easyroads3d.com


Troubleshooting:

- For Unity 2021.2 and Unity 2022 upgrade packages are included here in the EasyRoads3D root directory. These packages should auto import when these Unity versions are detected. Please import the respective package manually in case the EasyRoads3D toolbar does not appear and when there are errors in the console after selecting the main Road Network object.

- URP and HDRP packages should auto import when the repective render pipeline is detected. Please try importing the respective package manually in the case v3.2 demo scene road materials are still pink. The packages are located in /Assets/EasyRoads3D/SRP Support packages/

- Slow Scene View window responsiveness has been reported in the past for the v3.1 scene, this was mainly caused by the Broadleaf and Conifer tree prefabs assigned to the terrain object. Removing these tree prototypes from the terrain object or replacing them with other prefabs should solve this. 